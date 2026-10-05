"""Stack Lab's bounded background desktop and local preview; no OS input injection.

Uses the same Win32 launch pattern as OpenCnid/strata-bench DesktopProcess.
Controls and images belong to Stack Lab's optional in-process preview bridge.
"""
import argparse
import ctypes
import json
import os
import secrets
import subprocess
import threading
import time
from ctypes import wintypes as w
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


class Startup(ctypes.Structure):
    _fields_ = [("cb", w.DWORD), ("reserved", w.LPWSTR), ("desktop", w.LPWSTR),
                ("title", w.LPWSTR)] + [(key, w.DWORD) for key in
                ("x", "y", "width", "height", "chars_x", "chars_y", "fill", "flags")] + [
                ("show", w.WORD), ("reserved_size", w.WORD), ("reserved_bytes", ctypes.c_void_p),
                ("stdin", w.HANDLE), ("stdout", w.HANDLE), ("stderr", w.HANDLE)]


class Process(ctypes.Structure):
    _fields_ = [("process", w.HANDLE), ("thread", w.HANDLE), ("pid", w.DWORD), ("tid", w.DWORD)]


class BasicLimits(ctypes.Structure):
    _fields_ = [("process_time", ctypes.c_int64), ("job_time", ctypes.c_int64), ("flags", w.DWORD),
                ("min_working", ctypes.c_size_t), ("max_working", ctypes.c_size_t),
                ("processes", w.DWORD), ("affinity", ctypes.c_size_t),
                ("priority", w.DWORD), ("scheduling", w.DWORD)]


class Limits(ctypes.Structure):
    _fields_ = [("basic", BasicLimits), ("io", ctypes.c_uint64 * 6),
                ("process_memory", ctypes.c_size_t), ("job_memory", ctypes.c_size_t),
                ("peak_process", ctypes.c_size_t), ("peak_job", ctypes.c_size_t)]


def atomic_json(path, value):
    temporary = path.with_suffix(".tmp")
    temporary.write_text(json.dumps(value, indent=2), encoding="utf-8")
    os.replace(temporary, path)


def read_shared(path):
    # Allow Unity to atomically replace state/images while an HTTP reader holds the old file.
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel.CreateFileW.argtypes = [w.LPCWSTR, w.DWORD, w.DWORD, ctypes.c_void_p, w.DWORD, w.DWORD, w.HANDLE]
    kernel.CreateFileW.restype = w.HANDLE
    kernel.GetFileSizeEx.argtypes = [w.HANDLE, ctypes.POINTER(ctypes.c_int64)]
    kernel.ReadFile.argtypes = [w.HANDLE, ctypes.c_void_p, w.DWORD, ctypes.POINTER(w.DWORD), ctypes.c_void_p]
    kernel.CloseHandle.argtypes = [w.HANDLE]
    handle = kernel.CreateFileW(str(path), 0x80000000, 7, None, 3, 0x80, None)
    if handle == ctypes.c_void_p(-1).value:
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        size = ctypes.c_int64()
        if not kernel.GetFileSizeEx(handle, ctypes.byref(size)) or not 0 <= size.value <= 4 * 1024 * 1024:
            raise OSError("Preview file unavailable or too large")
        content, actual = ctypes.create_string_buffer(size.value), w.DWORD()
        if not kernel.ReadFile(handle, content, size.value, ctypes.byref(actual), None) or actual.value != size.value:
            raise ctypes.WinError(ctypes.get_last_error())
        return content.raw
    finally:
        kernel.CloseHandle(handle)


class OwnedDesktop:
    def __init__(self):
        if os.name != "nt":
            raise RuntimeError("This launcher requires Windows.")
        self.user = ctypes.WinDLL("user32", use_last_error=True)
        self.kernel = ctypes.WinDLL("kernel32", use_last_error=True)
        signatures = {
            "CreateDesktopW": (self.user, [w.LPCWSTR, ctypes.c_void_p, ctypes.c_void_p, w.DWORD, w.DWORD, ctypes.c_void_p], w.HANDLE),
            "OpenInputDesktop": (self.user, [w.DWORD, w.BOOL, w.DWORD], w.HANDLE),
            "GetProcessWindowStation": (self.user, [], w.HANDLE),
            "GetUserObjectInformationW": (self.user, [w.HANDLE, ctypes.c_int, ctypes.c_void_p, w.DWORD, ctypes.POINTER(w.DWORD)], w.BOOL),
            "CloseDesktop": (self.user, [w.HANDLE], w.BOOL),
            "CreateJobObjectW": (self.kernel, [ctypes.c_void_p, w.LPCWSTR], w.HANDLE),
            "SetInformationJobObject": (self.kernel, [w.HANDLE, ctypes.c_int, ctypes.c_void_p, w.DWORD], w.BOOL),
            "AssignProcessToJobObject": (self.kernel, [w.HANDLE, w.HANDLE], w.BOOL),
            "TerminateJobObject": (self.kernel, [w.HANDLE, w.UINT], w.BOOL),
            "CreateProcessW": (self.kernel, [w.LPCWSTR, w.LPWSTR, ctypes.c_void_p, ctypes.c_void_p, w.BOOL, w.DWORD, ctypes.c_void_p, w.LPCWSTR, ctypes.POINTER(Startup), ctypes.POINTER(Process)], w.BOOL),
            "ResumeThread": (self.kernel, [w.HANDLE], w.DWORD),
            "TerminateProcess": (self.kernel, [w.HANDLE, w.UINT], w.BOOL),
            "WaitForSingleObject": (self.kernel, [w.HANDLE, w.DWORD], w.DWORD),
            "CloseHandle": (self.kernel, [w.HANDLE], w.BOOL),
        }
        for name, (library, arguments, result) in signatures.items():
            function = getattr(library, name)
            function.argtypes, function.restype = arguments, result
        self.desktop = self.job = None
        self.children = []
        self.name = "StackLab-" + secrets.token_hex(12)
        try:
            self.initial_input = self.input_name()
            self.station = self.object_name(self.user.GetProcessWindowStation())
            if self.station.lower() != "winsta0":
                raise RuntimeError("Expected the current interactive window station.")
            # READOBJECTS | CREATEWINDOW | WRITEOBJECTS; no SWITCHDESKTOP access.
            self.desktop = self.user.CreateDesktopW(self.name, None, None, 0, 0x83, None)
            self.check(self.desktop)
            self.job = self.kernel.CreateJobObjectW(None, None)
            self.check(self.job)
            limits = Limits()
            limits.basic.flags = 0x2000  # Kill every owned child when the last job handle closes.
            self.check(self.kernel.SetInformationJobObject(self.job, 9, ctypes.byref(limits), ctypes.sizeof(limits)))
        except BaseException:
            self.close()
            raise

    @staticmethod
    def check(result):
        if not result:
            raise ctypes.WinError(ctypes.get_last_error())
        return result

    def object_name(self, handle):
        value, needed = ctypes.create_unicode_buffer(256), w.DWORD()
        self.check(handle)
        self.check(self.user.GetUserObjectInformationW(handle, 2, value, ctypes.sizeof(value), ctypes.byref(needed)))
        return value.value

    def input_name(self):
        handle = self.check(self.user.OpenInputDesktop(0, False, 1))
        try:
            return self.object_name(handle)
        finally:
            self.user.CloseDesktop(handle)

    def verify_input(self):
        if self.input_name() != self.initial_input:
            raise RuntimeError("Input desktop changed; stopping the owned pair.")

    def launch(self, argv, cwd, *, on_input=False):
        self.verify_input()
        info, startup = Process(), Startup()
        startup.cb = ctypes.sizeof(startup)
        startup.desktop = self.station + "\\" + (self.initial_input if on_input else self.name)
        startup.flags, startup.show = 0x81, 1  # Show on the selected desktop; never switch desktops.
        command = ctypes.create_unicode_buffer(subprocess.list2cmdline([str(a) for a in argv]))
        environment = ctypes.create_unicode_buffer("\0".join(f"{k}={v}" for k, v in sorted(os.environ.items(), key=lambda p: p[0].lower())) + "\0\0")
        self.check(self.kernel.CreateProcessW(str(argv[0]), command, None, None, False,
                   0x4 | 0x400 | 0x08000000, environment, str(cwd), ctypes.byref(startup), ctypes.byref(info)))
        attached = False
        try:
            self.check(self.kernel.AssignProcessToJobObject(self.job, info.process))
            attached = True
            self.children.append(info)
            self.verify_input()
            if self.kernel.ResumeThread(info.thread) != 1:
                raise ctypes.WinError(ctypes.get_last_error())
            return info.pid
        except BaseException:
            if not attached:
                self.kernel.TerminateProcess(info.process, 125)
                self.kernel.WaitForSingleObject(info.process, 2000)
                self.kernel.CloseHandle(info.process)
            raise
        finally:
            self.kernel.CloseHandle(info.thread)
            info.thread = None

    def close(self):
        if self.job:
            self.kernel.TerminateJobObject(self.job, 125)
            for info in self.children:
                self.kernel.WaitForSingleObject(info.process, 2000)
                self.kernel.CloseHandle(info.process)
            self.kernel.CloseHandle(self.job)
            self.job = None
        if self.desktop:
            self.user.CloseDesktop(self.desktop)
            self.desktop = None


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--player-folder", type=Path, required=True)
    parser.add_argument("--results", type=Path, required=True)
    parser.add_argument("--port", type=int, default=7777)
    parser.add_argument("--width", type=int, default=1920)
    parser.add_argument("--height", type=int, default=1080)
    parser.add_argument("--duration", type=int, default=900)
    parser.add_argument("--scenario", choices=["shared-rifle", "solo-precision", "developer-parity", "earned-growth", "weapon-stack"], default="shared-rifle")
    parser.add_argument("--uncapped", action="store_true")
    parser.add_argument("--human-host", action="store_true", help="Native-input A on the current desktop; bridge-controlled B in the background.")
    parser.add_argument("--wait-for-join", action="store_true", help="Hold B's launch until the viewer's Start test action.")
    args = parser.parse_args()
    if args.wait_for_join and not args.human_host:
        parser.error("--wait-for-join requires --human-host")
    executable = args.player_folder.resolve() / "StackLab.exe"
    if not executable.is_file() or not 1 <= args.duration <= 3600 or not 1 <= args.port <= 65535:
        parser.error("Check player path, duration (1..3600 seconds), and UDP port.")
    results = args.results.resolve()
    results.mkdir(parents=True, exist_ok=False)
    stop, lock = threading.Event(), threading.Lock()
    join_requested, joined = threading.Event(), threading.Event()
    sequences = {"host": 0, "client": 0}
    route = "/session/" + secrets.token_urlsafe(24)
    html = Path(__file__).with_name("shared-preview.html").read_bytes()

    class Handler(BaseHTTPRequestHandler):
        def log_message(self, *_):
            pass

        def reply(self, code, content, kind="application/json"):
            self.send_response(code)
            self.send_header("Content-Type", kind)
            self.send_header("Cache-Control", "no-store")
            self.send_header("Content-Length", str(len(content)))
            self.end_headers()
            self.wfile.write(content)

        def valid(self):
            return self.headers.get("Host") == f"127.0.0.1:{self.server.server_port}" and self.path.split("?", 1)[0].startswith(route + "/")

        def do_GET(self):
            if not self.valid():
                return self.reply(404, b"{}")
            path = self.path.split("?", 1)[0][len(route) + 1:]
            if path == "":
                return self.reply(200, html, "text/html; charset=utf-8")
            if path == "session.json":
                return self.reply(200, json.dumps({"humanRole": "host" if args.human_host else None,
                    "roles": ["client"] if args.human_host else list(sequences),
                    "waitingForJoin": args.wait_for_join and not joined.is_set()}).encode())
            parts = path.split("/")
            if len(parts) != 2 or parts[0] not in sequences or parts[1] not in ("state.json", "preview.jpg", "stream"):
                return self.reply(404, b"{}")
            if parts[1] == "stream":
                self.send_response(200)
                self.send_header("Content-Type", "multipart/x-mixed-replace; boundary=frame")
                self.send_header("Cache-Control", "no-store")
                self.end_headers()
                previous = None
                while not stop.wait(1 / 60):
                    try:
                        path = results / parts[0] / "preview.jpg"
                        modified = path.stat().st_mtime_ns
                        if modified == previous:
                            continue
                        content = read_shared(path)
                        previous = modified
                    except OSError:
                        continue
                    try:
                        self.wfile.write(b"--frame\r\nContent-Type: image/jpeg\r\nContent-Length: " + str(len(content)).encode() + b"\r\n\r\n" + content + b"\r\n")
                        self.wfile.flush()
                    except OSError:
                        return
                return
            try:
                content = read_shared(results / parts[0] / parts[1])
                self.reply(200, content, "image/jpeg" if parts[1].endswith("jpg") else "application/json")
            except (OSError, ValueError):
                self.reply(503, b"{}")

        def do_POST(self):
            if not self.valid() or self.headers.get("Origin", f"http://127.0.0.1:{self.server.server_port}") != f"http://127.0.0.1:{self.server.server_port}":
                return self.reply(403, b"{}")
            role = self.path[len(route) + 1:]
            if role == "stop":
                stop.set()
                return self.reply(202, b'{"stopping":true}')
            if role == "join" and args.wait_for_join:
                if joined.is_set():
                    return self.reply(409, b'{"error":"Client already joined; use Retry after the run"}')
                join_requested.set()
                return self.reply(202, b'{"joining":true}')
            if role not in sequences:
                return self.reply(404, b"{}")
            if args.human_host and role == "host":
                return self.reply(403, b'{"error":"Human host uses native input"}')
            try:
                length = int(self.headers.get("Content-Length", "0"))
                if not 0 < length <= 2048:
                    raise ValueError("Invalid size")
                command = json.loads(self.rfile.read(length))
                if command.get("action") not in ("input", "scripted", "idle", "choice", "continue", "retry", "quit"):
                    raise ValueError("Unknown action")
                with lock:
                    state = json.loads(read_shared(results / role / "state.json"))
                    if state["lastSequence"] < sequences[role]:
                        return self.reply(409, b'{"error":"Previous command not yet acknowledged"}')
                    command.update(sequence=sequences[role] + 1, run=state["run"], life=state["life"])
                    atomic_json(results / role / "command.json", command)
                    sequences[role] += 1
                    self.reply(202, json.dumps(command).encode())
            except (OSError, ValueError, KeyError, TypeError) as error:
                self.reply(400, json.dumps({"error": "Invalid command or player not ready", "detail": str(error)}).encode())

    with ThreadingHTTPServer(("127.0.0.1", 0), Handler) as server:
        desktop = OwnedDesktop()
        invocation = {"desktop": desktop.name, "inputDesktopBefore": desktop.initial_input,
                      "viewer": f"http://127.0.0.1:{server.server_port}{route}/", "results": str(results),
                      "startedUtc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
                      "durationLimitSeconds": args.duration, "processes": [],
                      "humanRole": "host" if args.human_host else None,
                      "waitingForJoin": args.wait_for_join,
                      "capture": "Unity camera JPEG stream, target 30 Hz. Preview overhead and other desktop load invalidate performance acceptance."}
        thread = None
        try:
            def launch_role(role):
                output = results / role
                output.mkdir()
                argv = [executable, "-screen-fullscreen", "0", "-screen-width", args.width,
                        "-screen-height", args.height, "-force-d3d11", "-port", args.port,
                        "-scenario", args.scenario, "-sharedResults", output, "-logFile", output / "player.log"]
                if not (args.human_host and role == "host"):
                    argv += ["-sharedPreview"]
                    if not args.human_host:
                        argv += ["-sharedScripted"]
                argv += ["-host", "-sharedLoopback"] if role == "host" else ["-join", "127.0.0.1"]
                if args.uncapped:
                    argv += ["-sharedUncapped"]
                pid = desktop.launch(argv, executable.parent, on_input=args.human_host and role == "host")
                invocation["processes"].append({"role": role, "pid": pid, "argv": [str(a) for a in argv]})
            launch_role("host")
            if not args.wait_for_join:
                launch_role("client")
                joined.set()
            invocation["inputDesktopAfterLaunch"] = desktop.input_name()
            atomic_json(results / "invocation.json", invocation)
            print(json.dumps(invocation), flush=True)
            thread = threading.Thread(target=server.serve_forever, daemon=True)
            thread.start()
            deadline = time.monotonic() + args.duration
            while not stop.wait(.05) and time.monotonic() < deadline:
                desktop.verify_input()
                if join_requested.is_set() and not joined.is_set():
                    launch_role("client")
                    joined.set()
                    invocation["waitingForJoin"] = False
                    invocation["joinedUtc"] = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
                    atomic_json(results / "invocation.json", invocation)
                if any(desktop.kernel.WaitForSingleObject(child.process, 0) == 0 for child in desktop.children):
                    invocation["stopReason"] = "owned player closed"
                    break
            invocation.setdefault("stopReason", "viewer stop" if stop.is_set() else "duration limit")
        except BaseException as error:
            invocation["stopReason"] = str(error)
            raise
        finally:
            stop.set()
            try:
                invocation["inputDesktopAtStop"] = desktop.input_name()
            finally:
                if thread:
                    server.shutdown()
                desktop.close()
            invocation["stoppedUtc"] = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
            atomic_json(results / "invocation.json", invocation)


if __name__ == "__main__":
    main()
