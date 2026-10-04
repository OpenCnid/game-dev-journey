"""Supervised companion inputs for a human A / bridge B session; never grants outcomes."""
import argparse
import json
import math
import time
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.parse import urlparse
from urllib.request import Request, urlopen


def request(url, command=None):
    data = None if command is None else json.dumps(command).encode()
    with urlopen(Request(url, data=data, headers={"Content-Type": "application/json"}), timeout=2) as response:
        return json.load(response)


def distance(a, b):
    return math.hypot(a["x"] - b["x"], a["z"] - b["z"])


def cover_input(state):
    human, companion = state["players"]
    position = companion["Position"]
    enemies = [e for e in state["enemies"] if e["Health"] > 0]
    if not enemies:
        return {"action": "idle"}
    nearest = min(enemies, key=lambda e: distance(e["Position"], position))
    protect = human["Position"] if human["Health"] > 0 else position
    target = nearest if distance(nearest["Position"], position) < 6 else min(enemies, key=lambda e: distance(e["Position"], protect))
    goal_x, goal_z = protect["x"] + 5, protect["z"] + 3
    dx, dz = goal_x - position["x"], goal_z - position["z"]
    if distance(nearest["Position"], position) < 5:
        dx, dz = position["x"] - nearest["Position"]["x"], position["z"] - nearest["Position"]["z"]
    size = math.hypot(dx, dz)
    move_x, move_z = (dx / size, dz / size) if size > 1.5 else (0, 0)
    to_x, to_z = target["Position"]["x"] - position["x"], target["Position"]["z"] - position["z"]
    return {"action": "input", "duration": .5, "moveX": move_x, "moveZ": move_z,
            "yaw": math.degrees(math.atan2(to_x, to_z)),
            "pitch": min(80, math.degrees(math.atan2(.85, math.hypot(to_x, to_z)))), "fire": True}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--viewer", required=True)
    parser.add_argument("--results", type=Path, required=True)
    parser.add_argument("--seconds", type=int, default=900)
    args = parser.parse_args()
    address = urlparse(args.viewer)
    if address.scheme != "http" or address.hostname != "127.0.0.1" or not 1 <= args.seconds <= 3600:
        parser.error("Use this game's loopback viewer and a bounded duration.")
    base = args.viewer.rstrip("/") + "/"
    args.results.mkdir(parents=True, exist_ok=True)
    deadline, previous, ready_runs = time.monotonic() + args.seconds, None, set()
    sent, samples = 0, 0
    with (args.results / "companion-policy.jsonl").open("a", encoding="utf-8") as log:
        while time.monotonic() < deadline:
            try:
                state = request(base + "client/state.json")
                samples += 1
                identity = (state["run"], state["state"], tuple((p["Health"] == 0, p["Life"]) for p in state["players"]))
                if identity != previous:
                    previous = identity
                    entry = {"utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()), "state": state}
                    log.write(json.dumps(entry) + "\n"); log.flush()
                    print(json.dumps({"run": state["run"], "state": state["state"], "elapsed": state["elapsed"],
                                      "A_hp": state["players"][0]["Health"], "B_hp": state["players"][1]["Health"]}), flush=True)
                if state["state"] == "Running" and state["players"][1]["Health"] > 0:
                    request(base + "client", cover_input(state)); sent += 1
                elif state["state"] in ("Complete", "Defeat") and state["run"] not in ready_runs:
                    # Preserve results before the human chooses Retry; only B readies itself.
                    (args.results / f"run-{state['run']}-terminal.json").write_text(json.dumps(state, indent=2), encoding="utf-8")
                    request(base + "client", {"action": "retry"})
                    ready_runs.add(state["run"])
                elif state["state"] == "Aborted":
                    break
            except HTTPError as error:
                if error.code not in (400, 409, 503):
                    raise
            except (URLError, TimeoutError, ConnectionError):
                try:
                    if not request(base + "session.json")["waitingForJoin"]:
                        break
                except (OSError, URLError):
                    break
            time.sleep(.2)
    print(json.dumps({"samples": samples, "inputs": sent, "readyRuns": sorted(ready_runs)}), flush=True)


if __name__ == "__main__":
    main()
