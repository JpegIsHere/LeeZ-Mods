# Stage 0-5 implementation and validation status

Updated: 2026-08-08 (Australia/Melbourne)

Branch: `dev/midstage-reschedule-probe`
Runtime source candidate: `ec07001f713d595c9ca22098e37c1e7a71b14dbd`
Package version: `0.5.3.0`
Runtime banner: `v0.5.3-dev5`
Target game build: `7 Days to Die V3.1.0 (b14)`

Detailed evidence is recorded in `docs/DEV5_LIVE_VALIDATION_2026-08-07.md`. Current high-level status is also maintained in `TESTING.md`.

## Stage 0-1

- Baseline branch/version/build identified: **PASS**.
- dev5 Release rebuild: **PASS**, 0 warnings / 0 errors.
- dev5 DLL load/startup: **PASS**.
- Crop scheduling, tick-rate and update-context hooks: **PASS**.
- 11 sunlight hooks: **PASS**.
- All four electrical transition hooks: **PASS**.
- `BlockPowered.OnBlockRemoved` removal hook: **PASS**.
- No relevant LeezGrowLights startup/Harmony warning observed.

## Stage 2

Core six-tier definitions, 10 W configuration and vanilla wiring basis remain in place. Existing in-game use confirms the lights are usable; full recipe/localization/progression presentation smoke remains release-oriented work.

## Stage 3 — partial, not complete

Live PASS:

- X/Z +2 valid.
- X/Z +3 rejected.
- Y+10 valid.
- Y+11 rejected.

Still pending: X+2/Z+2 corner, same-Y/below rejection, switched-OFF rejection, unpowered rejection, explicit no-global-sunlight-bypass check, and paired sunlight/acceleration geometry proof.

## Stage 4

Configured multipliers remain T1=1.2x, T2=1.3x, T3=1.4x, T4=1.5x, T5=1.6x, T6=4.0x. Highest active multiplier wins and lower-tier changes under an active T6 have been live-confirmed not to reschedule crops.

T4 and T6 proportional behavior is live-validated; T1 is also live-observed through direct tier-transition tests. Standalone full-duration timing for T2/T3/T5 remains pending.

## Stage 5

Live PASS:

- T1 -> T6: `1.2x -> 4x`, `89828 -> 26948`.
- T6 -> T1: `4x -> 1.2x`, `26577 -> 88590`.
- T4 `1x -> 1.5x`: `99296 -> 66197`.
- T4 `1.5x -> 1x`: `66169 -> 99254`.
- T6 -> T4: `4x -> 1.5x`, `21629 -> 57677`.
- Lower-tier toggle/activation while T6 remains winner: no reschedule.
- Direct source power loss: `4x -> 1x`, `20659 -> 82636`.
- Active effective T6 physical removal on dev5: correct `4x -> 1x`; examples `207 -> 828` and `28089 -> 112356`.
- Lower-tier T1 physical removal while T6 remains winner: no crop reschedule.

Direct source restoration `1x -> 4x`: **PENDING**.
Upstream relay-only transition: **NOT TESTED** by explicit test choice.

## Beyond Stage 5

Save/reload with an active T6 during a partial crop stage: **PASS for active-multiplier continuity**. After reload, switching T6 OFF produced `4x -> 1x`, `1603 -> 6412`.

Stage 6 is not complete. Chunk unload/reload, dedicated-server/remote-client behavior and other lifecycle/release tests remain pending.
