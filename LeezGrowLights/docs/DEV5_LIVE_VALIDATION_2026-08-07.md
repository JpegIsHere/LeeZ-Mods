# v0.5.3-dev5 live validation evidence

Test date: 2026-08-07 (Australia/Melbourne)
Branch: `dev/midstage-reschedule-probe`
Package version: `0.5.3.0`
Runtime banner: `v0.5.3-dev5`
Target game build: `7 Days to Die V3.1.0 (b14)`

This file records only live/build evidence actually observed during the dev5 validation session. Static/source-only claims are kept separate in the main testing documents.

## Build and startup

- Release rebuild: **PASS** — `Build succeeded`, `0 Warning(s)`, `0 Error(s)`.
- Rebuilt DLL copied into the game's `Mods/LeezGrowLights` directory: **PASS**.
- Runtime startup banner: **PASS** — `[LeezGrowLights] Loading V3.1 grow-light runtime candidate v0.5.3-dev5`.
- Crop scheduling hook: **PASS**.
- Crop tick-rate hook: **PASS**.
- Crop update-context hook: **PASS**.
- Sunlight substitution: **PASS (startup hook installation)** — 11 crop/base methods patched.
- Electrical transition hooks: **PASS (startup hook installation)** — all four installed:
  - `PowerConsumerToggle.set_IsToggled`
  - `PowerConsumerToggle.HandlePowerReceived`
  - `PowerConsumerToggle.HandlePowerUpdate`
  - `PowerItem.HandleDisconnect`
- Physical-removal hook: **PASS (startup hook installation)** — `BlockPowered.OnBlockRemoved`.
- No LeezGrowLights Harmony/startup warning was observed in the dev5 startup evidence.

## Coverage boundaries already live-validated

These live results predate or span the dev5 hardening work and remain regression evidence for the current branch:

- X/Z offset `+2`: **PASS / valid coverage**.
- X/Z offset `+3`: **PASS / correctly rejected**.
- Y `+10`: **PASS / valid coverage**.
- Y `+11`: **PASS / correctly rejected**.

Still pending for full Stage 3 closure: corner X+2/Z+2, same-Y/below rejection, switched-OFF rejection, unpowered rejection, no-global-sunlight-bypass check, and explicit paired sunlight/acceleration geometry confirmation.

## Mid-stage multiplier and overlap evidence

Reschedule rule under test:

`new remaining ticks = old remaining ticks × old effective multiplier ÷ new effective multiplier`

Rounding is AwayFromZero.

### Direct tier changes

- T1 -> T6: **PASS**.
  - T1 activation: `1x -> 1.2x`, `108115 -> 90096` ticks.
  - Direct T6 winner change: `1.2x -> 4x`, `89828 -> 26948` ticks.
  - Expected: `89828 × 1.2 / 4 = 26948.4`, rounded to `26948`.

- T6 -> T1: **PASS**.
  - `4x -> 1.2x`, `26577 -> 88590` ticks.
  - Expected: `26577 × 4 / 1.2 = 88590`.

- T4 `1x -> 1.5x`: **PASS**.
  - `99296 -> 66197` ticks.
  - Expected: `99296 / 1.5 = 66197.333...`, rounded to `66197`.

- T4 `1.5x -> 1x`: **PASS**.
  - `66169 -> 99254` ticks.
  - Expected: `66169 × 1.5 = 99253.5`, AwayFromZero -> `99254`.

- T6 -> T4 winner change: **PASS**.
  - `4x -> 1.5x`, `21629 -> 57677` ticks.
  - Expected: `21629 × 4 / 1.5 = 57677.33...`, rounded to `57677`.

### Highest-active-tier behavior

- T1 switched OFF while T6 remained active/effective: **PASS** — no crop reschedule occurred because effective multiplier stayed 4x.
- T1 switched ON while T6 was already active/effective during the dev5 removal-overlap setup: **PASS** — T1 became active but no crop reschedule occurred because T6 remained the 4x winner.

## Power-source transition evidence

- Direct source power loss while T6 switch remained ON: **PASS**.
  - T6 became inactive.
  - `4x -> 1x`, `20659 -> 82636` ticks.
  - Expected: `20659 × 4 = 82636`.
- Direct source restoration `1x -> 4x`: **NOT TESTED** in the recorded session.
- Upstream relay-only power transition: **NOT TESTED** by explicit test choice; do not treat the installed propagation hook as live behavioral proof.

## Physical grow-light removal

### Dev4 failure that motivated dev5

On dev4, an active effective T6 was physically broken while covering a crop, but no LeeZ reschedule event followed the removal. The removal hook was installed at startup, yet the live removal behavior failed. Diagnosis: `OnBlockRemoved` can execute while the old lamp is still readable from the world, causing the post-removal scan to see the same 4x winner and skip rescheduling.

### Dev5 fix validation

Dev5 changes the removal-specific rescan to exclude the block position being removed.

- Active effective T6 physical removal: **PASS**.
  - Pre-removal setup at `23:03:56`: T6 ACTIVE at 4x; one observed crop transition was `1x -> 4x`, `2138 -> 535` ticks.
  - Physical T6 removal at `23:05:01` produced T6 INACTIVE and `4x -> 1x` reschedules.
  - Observed examples:
    - `207 -> 828` ticks (`207 × 4 = 828`).
    - `28089 -> 112356` ticks (`28089 × 4 = 112356`).

- Lower-tier T1 physical removal while T6 remained the active 4x winner: **PASS**.
  - T6 and T1 were both active before removal.
  - Removing T1 made T1 inactive but produced no crop reschedule, which is correct because the effective multiplier remained 4x.

## Save/reload continuity

- Save/reload with T6 powered and ON during a partial crop stage: **PASS for active-multiplier continuity**.
  - The world was exited to the main menu and the same world reloaded without restarting the game.
  - After reload, turning T6 OFF produced `4x -> 1x`, `1603 -> 6412` ticks.
  - Expected: `1603 × 4 = 6412`.
  - This proves the crop still behaved as being under the effective 4x T6 state after reload and rescheduled correctly when the light was then switched OFF.

This does **not** complete all Stage 6 persistence/lifecycle testing. Chunk unload/reload, dedicated-server/remote-client behavior, and other lifecycle cases remain pending.

## Current live-validation status summary

Live PASS:

- dev5 Release build: 0 warnings / 0 errors.
- dev5 startup and expected hook installation.
- T1 -> T6 and T6 -> T1 proportional transitions.
- T4 1x <-> 1.5x proportional transitions.
- T6 -> T4 winner transition.
- Highest-active-tier no-op behavior for lower-tier toggle changes under T6.
- Direct source power loss 4x -> 1x.
- Active effective T6 physical removal 4x -> 1x.
- Lower-tier physical removal while T6 remains winner causes no crop reschedule.
- Save/reload active-T6 continuity, proven by a correct post-reload 4x -> 1x transition.
- Coverage boundaries X/Z +2 valid, X/Z +3 rejected, Y+10 valid, Y+11 rejected.

Not tested / still pending:

- Direct source restoration 1x -> 4x.
- Upstream relay-only power transition (explicitly not tested).
- Remaining Stage 3 coverage/sunlight cases.
- Chunk unload/reload.
- Dedicated server with remote client.
- Long-duration sealed-room survival/power-loss behavior.
- Dense-farm performance.
