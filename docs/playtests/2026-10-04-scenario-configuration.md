# GDJ-22 — scenario configuration candidate

October 4, 2026 (America/Chicago). **Owner accepts the presentation for developer testing; candidate awaits coordinator review.** This report does not establish enjoyment, balance or new native performance. GDJ-20's accepted fixed loopback evidence remains separate.

## Exact source and build

- Base: merged main `e6434ec7575e2d7e3e81e3d0b799bc15299bb6e1`; foundation revision 18.
- Runtime/release source: **`ba71678da53dbe9e1d1354b4a8dcb43a61f0b3f2`**, clean. Unity 6000.3.25f1 / Transport 2.7.4 unchanged. Release built UTC `2026-10-05T02:52:00.2602718Z`.
- Whole player: `C:/Users/Darian/.codex/worktrees/gdj-22-scenario-config/new-game-dev/prototypes/001-stacked-weapons/build/StackLab-GDJ22-v1/`. All **167 files** rehash with zero mismatches against the [manifest](evidence/gdj-22-scenarios/v1-player-manifest.json), SHA-256 `191c8913837abeb077df372408cc899df7143ddc80f33560e81f81494ae9228a`. Distribute this entire folder. Accepted GDJ-20 and solo archives were untouched.
- Worktree: `C:/Users/Darian/.codex/worktrees/gdj-22-scenario-config/new-game-dev`; branch `codex/gdj-22-scenario-config`. [Task contract](../planning/tasks/GDJ-22.md) was committed before runtime edits.

## Implemented contract

An immutable version-1 configuration owns scenario identity, seed, duration, drone count/HP and starting rifle body damage/head multiplier/interval. Canonical binary content has a SHA-256 identity. Both solo and shared paths consume it, including respawn HP, hit requests, cadence, wounded geometry/HP projection and critical feedback. Starting definitions remain distinct from future authoritative personal earned builds.

| Named control | Count / starting HP | Body / head | Interval / rate | Seed / duration |
| --- | --- | --- | --- | --- |
| Solo precision | 35 / 40 | 20 / 40 (x2) | .12 s / about 8.33/s | 137 / 60 s |
| Shared rifle | 12 / 60 | 20 / 40 (x2) | .12 s / about 8.33/s | 137 / 60 s |
| Developer parity | 12 / 90 | 15 / 45 (x3) | .25 s / 4/s | 271 / 60 s |

Protocol **2** transfers a bounded configuration offer and new session GUID; client validates/acknowledges that content before Start. Snapshots and remote Retry carry the frozen session/configuration. Both-ready Retry increments run generation and retains the same settings. Stale run feedback/readiness cannot mutate the new run. Changed or invalid configuration requires rejection, not local fallback. Old protocol-1 players are intentionally incompatible. Host settings cannot be edited within a session; start a fresh pair to change them.

The menu distinguishes Next solo and Host. Selecting a scenario applies on Start/Host; Join adopts the host's settings. Solo rate edits retain fractional cooldown, pause behavior and session/restart retention, and feed the next Host selection. Shared submission now drains multiple due shots per rendered frame, preserving the supported high-rate control. Existing historical-pose/incarnation rejection remains in place.

Supported envelope: 1–35 drones, HP 1–10000, body damage 1–1000, integer head multiplier 1–4, finite duration 1–600 seconds, finite rifle rate .5–120/s, signed 32-bit seed. The largest snapshot is **907 bytes including message type**, within the unchanged 1100-byte cap. This bound is functional, not measured network scalability. Synthetic benchmarks retain their independent 20,000-enemy capacity and historical sphere/body-only workload.

## Checks and observed results

[Exact check metadata](evidence/gdj-22-scenarios/v1-checks.json), [108 Edit Mode results](evidence/gdj-22-scenarios/v1-EditMode.xml) and [10 Play Mode results](evidence/gdj-22-scenarios/v1-PlayMode.xml) pass through serialized `tools/unity.ps1` runs, with the affected Editor closed. The release build passes. Checks cover default/developer HP/body/head parity, exact per-tick due-shot parity at .5/2/4/baseline/30/120 shots/sec, malformed/version/nonfinite/range rejection, atomic parsing, changed session/configuration, old-run rejection, maximum packet size, configured projection and the retained solo controls. Actual Play Mode game pairs cover all three named scenarios and one/both-ready Retry. Existing death/return/immunity/defeat-before-return/completion and loss tests remain passing.

| Standalone check | Result |
| --- | --- |
| [Default shared pair](evidence/gdj-22-scenarios/default-pair-verification.json) | Both processes adopt shared rifle despite the client's explicit solo/2/s local flags. Complete at tick 3600; full terminal player/enemy parity; 500 accepted shots per player. |
| [Developer pair](evidence/gdj-22-scenarios/override-pair-verification.json) | Both adopt 90 HP, 15/45 damage, .25 s, seed 271. Complete at tick 3600; full terminal parity. |
| [120/s, 8-second override](evidence/gdj-22-scenarios/fast-pair-verification.json) | Matching frozen configuration; complete at tick 480 with terminal parity. Confirms high-rate submission functions; not a damage/throughput or performance guarantee. |
| Each of those pairs | One-ready remains on run 1; both-ready enters run 2 with the same session/content identity; old-run Retry rejects; hard client loss aborts the survivor. |
| [Five invalid native launches](evidence/gdj-22-scenarios/invalid-launches.json) | Unknown scenario, HP 0, NaN rate, malformed damage and 36 drones reject before creating a shared session. |
| Retained reporting controls | [2/s regular](evidence/gdj-22-scenarios/reference-slow.json), [120/s regular](evidence/gdj-22-scenarios/reference-fast.json), and [1000 synthetic bosses](evidence/gdj-22-scenarios/reference-bosses.json) complete short hidden reporting smokes; expected rate/interval metadata, zero dropped projectiles and zero catch-up-limit frames. Their frame timings are not performance evidence. |

The [pair event audit](evidence/gdj-22-scenarios/pair-audit.json) confirms identical ordered nonterminal outcomes in each pair. Default has 1000 identical outcomes. Developer has 481 damage/miss outcomes; fast has 1505 damage/miss outcomes plus 415 identical stale-enemy rejections. At high rate, several shots can refer to an incarnation already killed by an earlier shot; the preserved authority policy rejects those historical requests, so emitted requests and accepted shots differ. Developer/fast each have one extra host-only late Terminal rejection after ending/Retry; client discards old-run feedback. These rejected requests change no state. No claim of complete terminal-rejection log parity or new throughput follows.

Raw configuration, run-1 summaries and attributed event CSVs are retained beside the audit. Full frame logs and sampled preview observations remain in ignored `prototypes/001-stacked-weapons/artifacts/gdj22-{default,override,fast}-pair/`. The pair checks use hidden native windows with the existing preview bridge and scripted input, so they are functional evidence only. No other person or hardware was needed. All owned players and project Unity operations are stopped.

## Native visibility and owner feedback

Agent inspection of the actual release windows verifies [named default selections](evidence/gdj-22-scenarios/native-menu.png), [solo default](evidence/gdj-22-scenarios/native-solo-default.png), [solo developer](evidence/gdj-22-scenarios/native-solo-developer.png), and matching developer settings on [host](evidence/gdj-22-scenarios/native-shared-developer-host.png) / [client](evidence/gdj-22-scenarios/native-shared-developer-client.png). The shared HUD displays Host ownership, Retry retention and matching content identity. These are native window captures; no preview-only HUD reconstruction is used. An earlier hidden native ScreenCapture returned black and was excluded from the visibility evidence.

The owner was shown the native comparison and asked whether the scenario differences and host ownership are clear and match expectations. The owner replied: “I can't answer this question without what the rubric or my expectations should entail. It's unclear.” This identified an unusable review request; the rubric below was then supplied. The later acceptance below supersedes that pending visibility gate.

### Concrete owner review rubric

The owner is reviewing whether the native menu/HUD communicates the tested behavior. Automated checks already cover value agreement, validation, cadence and reset correctness. This review does not ask the owner to approve these fixture values as final game balance or recheck network correctness.

| Readability criterion | A pass means |
| --- | --- |
| Selected versus active scenario | The menu makes the next solo/Host selection understandable, and the combat HUD lets the player identify the scenario actually running. |
| Effective combat settings | HP, body/head damage and firing rate are legible and their labels understandable. Default solo is 40 HP, 20/40 damage, about 8.33/s; default shared is 60 HP with the same rifle. Developer parity is 90 HP, 15/45 damage and 4/s in both modes. |
| Ownership and when changes apply | The UI communicates that Join uses the host's settings; both players share them; shared Retry keeps them; changing shared tuning requires a new session. |

The default solo/shared HP difference preserves earlier test fixtures; Developer parity is a deliberate comparison configuration, not proposed final balance. A useful review identifies which criterion is clear or names missing/confusing wording, illegible text or unexpected behavior. “Cannot tell from these screenshots” is also valid feedback and calls for a guided native comparison. Agent inspection and automated parity are not owner acceptance, gameplay enjoyment or a balance decision.

### Owner acceptance for developer testing

After the rubric was supplied, the owner asked whether both versions are development prototypes rather than the final product. We clarified that the scenario selectors, technical labels and tuning values support testing, and that an optimized standalone release build does not mean a finished product. The owner then accepted the intended scope: **“Yes, for testing they are fine. Since they're just for developers AKA myself.”**

This completes GDJ-22's owner visibility/expectation gate for the developer-facing solo/co-op scenario controls and settings. It does not accept final player-facing UI, final game balance or gameplay enjoyment, and provides no new performance evidence or merge authorization. Runtime source, build, archived player and checks remain unchanged.

## Coordinator review — feedback reset finding

The owner requested the remaining code/integration review. Review of `dfbe2d73cd73245201d85fce07e1ec8439964625` found one P2 lifecycle issue during a deeper audit of repeated default/developer/120-per-second standalone runs: the client omitted the host's first hit from feedback/events, while authoritative player/enemy state still matched. `SharedGame.SyncGeneration` cleared the entire feedback queue after `Poll` could already have received the new snapshot and that run's first hits. The same ordering can occur at Retry. This reset behavior predated the scenario extraction but affects its integration checks.

The [three reproduced omissions](evidence/gdj-22-scenarios/review-feedback-finding.json) preserve the exact missing event and limits. The [new regression before the fix](evidence/gdj-22-scenarios/review-feedback-before-PlayMode.xml) fails with zero retained events instead of two. The bounded fix filters queued feedback by the active run, preserving current-run order and discarding stale events. A Play Mode regression covers both initial Start and Retry; the standalone helper now compares all ordered run-1 nonterminal outcomes, including the first hit. Late terminal rejections remain outside that comparison because clients discard old-run feedback after Retry.

A replacement clean release and standalone rechecks are being prepared. The original v1 archive/evidence above remain preserved. Review completion and the replacement source/build will be recorded here after those checks.

## Reproduction

From the worktree above, with its Editor closed:

```powershell
./tools/unity.ps1 -Action Test -TestPlatform EditMode
./tools/unity.ps1 -Action Test -TestPlatform PlayMode
./tools/unity.ps1 -Action Build
```

For the exact archived candidate, the existing helper can launch default manual/scripted pairs:

```powershell
./tools/shared-combat.ps1 -PlayerFolder prototypes/001-stacked-weapons/build/StackLab-GDJ22-v1 -ScriptedCompanion
```

For the reproducible functional agreement/Retry/loss check, use a fresh output directory and free port. The script owns/stops only its pair and writes the assertions and observation snapshots:

```powershell
$taskPython = "$env:USERPROFILE/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe"
& $taskPython docs/playtests/evidence/gdj-22-scenarios/check_scenario_pair.py `
  --player "$PWD/prototypes/001-stacked-weapons/build/StackLab-GDJ22-v1/StackLab.exe" `
  --results "$PWD/prototypes/001-stacked-weapons/artifacts/NEW-gdj22-pair" `
  --scenario developer-parity --port 17823
```

Use `--scenario shared-rifle` for the default; add `--rate 120 --duration 8` for the short high-rate case. Native launch flags and all override bounds are in the [prototype README](../../prototypes/001-stacked-weapons/README.md#scenario-configuration-gdj-22). Solo uses `-playtest -scenario developer-parity`; host uses `-host -sharedLoopback -scenario developer-parity`; client uses `-join 127.0.0.1`. Supply distinct `-sharedResults` paths for the pair. Default direct UDP port is 7777.

## Remaining scope and handoff

Draft [PR #13](https://github.com/OpenCnid/game-dev-journey/pull/13) is open and attached. The owner visibility/expectation gate is complete for developer testing; coordinator code/integration review remains. Do not merge automatically. Full lobby/readiness design, unified command processing, GDJ-6 progression, thinking space, recovery/migration, four players/dense replication and GDJ-21 runner/viewer redesign are excluded. GDJ-21 can reuse scenario construction/validation and evidence identity; future earned builds must not mutate frozen starting definitions. No editor/package upgrade, external asset or new performance claim is introduced.
