# Testing status

Target test build: **7 Days to Die V3.1.0 (b14)**.

Current runtime candidate: **v0.5.3-dev5** on `dev/midstage-reschedule-probe`.

Detailed live evidence: `docs/DEV5_LIVE_VALIDATION_2026-08-07.md`.

Legend: `PASS` = observed live/build evidence; `STATIC VERIFIED` = source/XML proof only; `PENDING` = live validation incomplete; `NOT TESTED` = explicitly not live-tested.

## Build and startup

| Test | Status | Notes |
|---|---|---|
| v0.5.3-dev5 Release build | PASS | 0 warnings, 0 errors on the target V3.1.0 b14 installation |
| dev5 DLL deployment | PASS | rebuilt DLL copied into the game Mods directory |
| dev5 runtime banner | PASS | `v0.5.3-dev5` observed at startup |
| crop scheduling/tick/update hooks | PASS | all installed at startup |
| sunlight hooks | PASS | 11 crop/base methods patched |
| electrical hooks | PASS | toggle, received-power, propagated-power and `PowerItem.HandleDisconnect` installed |
| removal hook | PASS | `BlockPowered.OnBlockRemoved` installed |
| relevant Leez startup warnings | PASS | none observed in dev5 startup evidence |

## Coverage and sunlight

| Test | Status |
|---|---|
| enclosed active-light farming | PASS regression evidence |
| X/Z +2 valid | PASS |
| X/Z +3 rejected | PASS |
| Y+10 valid | PASS |
| Y+11 rejected | PASS |
| X+2/Z+2 corner | PENDING |
| same-Y/below rejection | PENDING |
| switched-OFF rejection | PENDING |
| unpowered rejection | PENDING |
| no global sunlight bypass live check | PENDING |
| paired sunlight/acceleration geometry proof | PENDING |

Formal Stage 3 is **not complete**.

## Tier and overlap behavior

Configured multipliers remain T1=1.2x, T2=1.3x, T3=1.4x, T4=1.5x, T5=1.6x, T6=4.0x. Highest active multiplier wins; active multipliers do not stack.

Live evidence confirms:

- T1 -> T6: **PASS**, `1.2x -> 4x`, `89828 -> 26948` ticks.
- T6 -> T1: **PASS**, `4x -> 1.2x`, `26577 -> 88590` ticks.
- T4 `1x -> 1.5x`: **PASS**, `99296 -> 66197` ticks.
- T4 `1.5x -> 1x`: **PASS**, `66169 -> 99254` ticks.
- T6 -> T4: **PASS**, `4x -> 1.5x`, `21629 -> 57677` ticks.
- Lower-tier toggle/activation under active T6: **PASS**, no crop reschedule when effective multiplier remained 4x.

Standalone full-duration timing for T2, T3 and T5 remains pending.

## Mid-stage power and removal

Reschedule rule:

`new remaining ticks = old remaining ticks × old effective multiplier ÷ new effective multiplier`

Rounding is AwayFromZero.

| Test | Status | Evidence |
|---|---|---|
| direct source power loss | PASS | `4x -> 1x`, `20659 -> 82636` |
| direct source restoration | PENDING | explicit `1x -> 4x` restoration run not yet recorded |
| upstream relay-only transition | NOT TESTED | explicitly skipped; hook installation is not treated as behavior proof |
| active effective T6 physical removal | PASS on dev5 | `4x -> 1x`; examples `207 -> 828`, `28089 -> 112356` |
| lower-tier T1 removal while T6 remains winner | PASS on dev5 | T1 became inactive; no crop reschedule because effective multiplier stayed 4x |

The dev4 active-T6 physical-removal test failed despite the hook being installed. Dev5 fixes the removal-specific re-scan by excluding the block position being removed; the dev5 live retest passed.

## Save/reload and lifecycle

| Test | Status | Evidence |
|---|---|---|
| save/reload during partial stage with active T6 | PASS | after reload, T6 OFF produced `4x -> 1x`, `1603 -> 6412` |
| chunk unload/reload | PENDING | not yet validated |
| dedicated server / remote client | PENDING | not yet validated |

## Remaining release-oriented tests

- Remaining Stage 3 coverage/sunlight cases listed above.
- Direct source restoration `1x -> 4x`.
- Chunk unload/reload.
- Dedicated server with remote client.
- Long-duration sealed-room survival and underground power-loss response.
- Dense-farm performance.
- Optional exploratory placement/connect behavior.

## Colour system (future)

Allowed design colours currently stored in XML metadata: Blue, Green, Red, Purple, White and Yellow. Selection UI, persistence, network sync and runtime tinting are not implemented yet.
