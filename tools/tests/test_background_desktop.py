"""Actual Windows desktop placement, exact arguments, and owned tree cleanup."""
import ctypes
import json
import os
import subprocess
import sys
import tempfile
import time
import unittest
from ctypes import wintypes as w
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from background_desktop import OwnedDesktop


@unittest.skipUnless(os.name == "nt", "Win32 integration")
class DesktopTests(unittest.TestCase):
    def test_child_desktop_arguments_and_descendants_stop_on_close(self):
        script = '''import ctypes,json,subprocess,sys,time
from ctypes import wintypes as w
from pathlib import Path
u=ctypes.WinDLL('user32');k=ctypes.WinDLL('kernel32')
u.GetThreadDesktop.argtypes=[w.DWORD];u.GetThreadDesktop.restype=w.HANDLE
k.GetCurrentThreadId.restype=w.DWORD
u.GetUserObjectInformationW.argtypes=[w.HANDLE,ctypes.c_int,ctypes.c_void_p,w.DWORD,ctypes.POINTER(w.DWORD)]
value=ctypes.create_unicode_buffer(256);size=w.DWORD()
assert u.GetUserObjectInformationW(u.GetThreadDesktop(k.GetCurrentThreadId()),2,value,ctypes.sizeof(value),ctypes.byref(size))
child=subprocess.Popen([sys.executable,'-c','import time;time.sleep(30)'])
Path(sys.argv[1]).write_text(json.dumps({'desktop':value.value,'args':sys.argv[2:],'child':child.pid}))
time.sleep(30)
'''
        with tempfile.TemporaryDirectory(prefix="Stack Lab desktop ") as directory:
            output = Path(directory) / "ready.json"
            desktop = OwnedDesktop()
            handles = []
            try:
                arguments = ['quoted "value"', 'Unicode café', 'trailing\\']
                desktop.launch([sys.executable, "-c", script, str(output), *arguments], Path(directory))
                deadline = time.monotonic() + 5
                while not output.is_file() and time.monotonic() < deadline:
                    time.sleep(.02)
                report = json.loads(output.read_text())
                self.assertEqual(report["desktop"], desktop.name)
                self.assertEqual(report["args"], arguments)
                self.assertEqual(desktop.input_name(), desktop.initial_input)
                open_process = desktop.kernel.OpenProcess
                open_process.argtypes, open_process.restype = [w.DWORD, w.BOOL, w.DWORD], w.HANDLE
                for pid in [desktop.children[0].pid, report["child"]]:
                    handles.append(desktop.check(open_process(0x100000, False, pid)))
                desktop.close()
                for handle in handles:
                    self.assertEqual(desktop.kernel.WaitForSingleObject(handle, 2000), 0)
            finally:
                desktop.close()
                for handle in handles:
                    desktop.kernel.CloseHandle(handle)

    def test_changed_input_context_prevents_launch(self):
        desktop = OwnedDesktop()
        try:
            desktop.initial_input = "injected-context-mismatch"
            with self.assertRaisesRegex(RuntimeError, "Input desktop changed"):
                desktop.launch([sys.executable, "-c", "raise RuntimeError('must not run')"], Path.cwd())
            self.assertEqual(len(desktop.children), 0)
        finally:
            desktop.close()


if __name__ == "__main__":
    unittest.main()
