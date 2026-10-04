# 0012: Disconnect recovery and preserved return

- **Date:** 2026-10-04
- **Status:** accepted owner direction for freeze allowance and late return; detailed recovery policy proposed, implementation absent.

## Context

The owner considers reconnect after connection failure important for the 1–4-player cooperative game. A run contains shared progress and personal builds; losing either to a brief disconnection undermines perceptible growth. The fixed GDJ-20 feasibility fixture aborts on disconnect and has no recovery identity/state protocol.

The owner proposes using The Zone to freeze physical time while a player reconnects. They explicitly choose **“Five minutes total per run”** for automatic disconnect waiting and **“Allow later return with preserved progress”** after that window. Disconnected participants must not block resuming indefinitely. This adds future product behavior; it does not retroactively claim implementation or replace the current fixture's abort verification.

## Accepted direction

Use a shared **300-second wall-clock allowance per run** for automatic disconnect freezes. Physical encounter clocks stop while recovery/networking continues. Earlier successful reconnect uses only the wait actually spent; subsequent losses use the remainder. Retry creates a new run/allowance. Presentation may use The Zone without spatial movement or combat during the freeze.

Retain the original participant's progress and support later return to the same still-ongoing run after the automatic allowance has expired. Preserve normal upgrade-thinking space's unlimited earned-choice time; its pause/readiness rules and connectivity eligibility must remain explicit rather than letting an absent participant veto resume indefinitely.

## Recommended policy requiring review

The [research and recovery draft](../design/cooperative-session-recovery.md) recommends withdrawing the expired avatar from combat/readiness while keeping an authorized return record, rather than letting it stand still and die. It proposes current-state resynchronization, once-only choice/reward records and separate connectivity/life state. Late return does not automatically pause the party again. Exact re-entry safety, absent reward eligibility, participant/terminal ordering and resume presentation are candidates, not additional owner selections.

Treat a live host losing its connection separately from a host process crash. Checkpoint-backed peer host migration is the recommended long-term target; same-host reconnect is the first bounded implementation. Election/fencing, complete authority state, persistence/rollback budgets and the alternative of persistent dedicated authority need separate feasibility work. Reconnecting or selecting a lobby host cannot restore missing combat state by itself.

## Consequences and revisit conditions

Design stable membership, versioned configuration, explicit pause reasons and complete recovery state before attaching more multiplayer progression. Coordinate GDJ-13 death/participation, GDJ-7 The Zone and GDJ-14 shared grant eligibility. Keep current GDJ-20 visible performance/human acceptance pending and preserve its legacy fixture until a new implementation is explicitly verified.

The [multiplayer boundary proposal](../design/multiplayer-boundaries.md) records the subsequently requested ownership/interface and staged-verification design. It preserves this accepted direction while keeping the broader architecture, production eligibility and service/fault guarantees proposed.

Research finds documented reconnect/rejoin patterns in Gunfire Reborn and Ember Knights, separate pause/rejoin evidence in Risk of Rain 2, and an official host-loss limitation in Ravenswatch. Those sources do not establish this whole contract or validate a five-minute waiting experience. Revisit timing and proposed withdrawal/re-entry behavior through human tests, including repeated/overlapping failures and host-process loss. No service/package, migration guarantee or implementation date is selected by this decision.

**Second design review, October 4:** the [networking/scale contract](../design/cooperative-session-recovery.md#networking-and-scale-contract--second-review) tightens credential claims and message epochs, consistent live catch-up, bounded bulk work, coherent checkpoints and durable once-only commitments. Recovery countdowns consume the same allowance, budget metadata cannot refill through world rollback, and withdrawal/terminal/admission ordering runs even inside a frozen upgrade Zone. Interrupted/dormant recovery can restore the same run; only committed completion/defeat is terminal. These are proposed technical/policy corrections, not additional accepted owner choices or runtime validation. Keep the first implementation at the existing pair/12-drone envelope and gate dense replication separately.
