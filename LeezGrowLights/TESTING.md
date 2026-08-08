# Testing status

Target test build: **7 Days to Die V3.1.0 (b14)**.

Current runtime candidate: **v0.5.3-dev6** on `dev/midstage-reschedule-probe`.

Detailed prior live evidence: `docs/DEV5_LIVE_VALIDATION_2026-08-07.md`.

Legend: `PASS` = observed live/build evidence; `STATIC VERIFIED` = source/XML proof only; `PENDING` = live validation incomplete; `NOT TESTED` = explicitly not live-tested.

## Dev6 change under test

Dev6 changes Stage 3 grow-light geometry to be orientation-aware:

- downward-facing panel: full 5x5 footprint;
- downward minimum farm-block vertical offset: **Y+2**;
- downward maximum: **Y+10**;
- horizontal panel: directional **2x2** footprint in front of the emitting face;
- horizontal lamp alignment: lamp is at the plant-block level, one block above the supporting farm block;
- upward-facing panel: no sunlight substitution or growth bonus;
- horizontally facing away from the crop: no sunlight substitution or growth bonus;
- sunlight substitution and growth acceleration both use the same orientation-aware scanner.

The orientation resolver uses the placed block rotation from the live V3.1 runtime through reflection. Unknown advanced rotations fail closed rather than granting incorrect grow benefits.

**Dev6 has not yet passed the live build/startup/orientation gate.** All dev5 results below remain regression evidence and must not be treated as proof of the new directional geometry until rerun on dev6.

## Build and startup

| Test | Status | Notes |
|---|---|---|
| v0.5.3-dev6 Release build | PENDING | Must build against V3.1.0 b14 |
| dev6 DLL deployment | PENDING | Deploy rebuilt DLL after successful build |
| dev6 runtime banner | PENDING | Expect `v0.5.3-dev6` |
| crop scheduling/tick/update hooks | PENDING regression | dev5 passed |
| sunlight hooks | PENDING regression | dev5 patched 11 crop/base methods |
| electrical hooks | PENDING regression | dev5 installed toggle, received-power, propagated-power and disconnect |
| removal hook | PENDING regression | dev5 installed `BlockPowered.OnBlockRemoved` |
| orientation resolver startup/runtime diagnostics | PENDING | Confirm a V3.1 rotation source is discovered; investigate fallback/unavailable warnings |

## Stage 3 — directional coverage and sunlight

The previous Y+1-valid rule is obsolete as of dev6.

| Test | Status |
|---|---|
| downward Y+1 rejected | PENDING |
| downward Y+2 valid | PENDING |
| downward Y+10 valid | PENDING regression |
| downward Y+11 rejected | PENDING regression |
| downward X/Z +2 valid | PENDING regression |
| downward X/Z +3 rejected | PENDING regression |
| downward X+2/Z+2 corner valid | PENDING regression |
| downward outside 5x5 rejected | PENDING regression |
| horizontal 2x2 front-facing footprint valid | PENDING |
| horizontal position immediately outside 2x2 rejected | PENDING |
| horizontal lamp facing away from crop rejected | PENDING |
| upward-facing lamp rejected | PENDING |
| switched-OFF lamp rejected | PENDING |
| unpowered lamp rejected | PENDING |
| open-air vanilla crop outside LeeZ coverage behaves normally | PENDING |
| sunlight substitution and growth acceleration match the same directional geometry | PENDING |

Historical dev5/manual observations before the geometry change confirmed the old 5x5 edge/corner behavior, including that positions outside the 5x5 did not receive planting support or a grow bonus. These are regression anchors only for dev6.

## Tier and overlap behavior — dev5 regression evidence

Configured multipliers remain T1=1.2x, T2=1.3x, T3=1.4x, T4=1.5x, T5=1.6x, T6=4.0x. Highest active multiplier wins; active multipliers do not stack.

Live dev5 evidence confirms:

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
| direct source power loss | PASS on dev5 | `4x -> 1x`, `20659 -> 82636` |
| direct source restoration | PASS manual confirmation | Confirmed working in the 2026-08-08 test session; exact tick line not recorded in repository evidence yet |
| upstream relay/propagation loss and restoration | PASS manual confirmation | Confirmed working in the 2026-08-08 test session; exact tick lines not recorded in repository evidence yet |
| active effective T6 physical removal | PASS on dev5 | `4x -> 1x`; examples `207 -> 828`, `28089 -> 112356` |
| lower-tier T1 removal while T6 remains winner | PASS on dev5 | T1 became inactive; no crop reschedule because effective multiplier stayed 4x |

Because dev6 changes crop/light geometry, representative direct-source, relay, winner-change and removal tests should be rerun after the orientation gate passes.

## Save/reload and lifecycle

| Test | Status | Evidence |
|---|---|---|
| save/reload during partial stage with active T6 | PASS on dev5 | after reload, T6 OFF produced `4x -> 1x`, `1603 -> 6412` |
| chunk unload/reload | PENDING |
| dedicated server / remote client | PENDING |

## Immediate dev6 live gate

1. Release-build dev6 against V3.1.0 b14 and confirm 0 errors.
2. Launch and confirm the `v0.5.3-dev6` banner plus clean hook installation.
3. Check the orientation resolver diagnostic. A discovered V3.1 rotation source is preferred; fallback/unavailable warnings require focused orientation testing.
4. Down-facing T6: prove Y+1 rejected, Y+2 valid, Y+10 valid and Y+11 rejected.
5. Down-facing T6: prove the full 5x5 including the corner, and rejection immediately outside it.
6. Horizontal T6: prove only the directional 2x2 area in front receives planting support and 4x growth.
7. Rotate the same lamp away from the crop, then upward; both must provide no planting support and no growth bonus.
8. Repeat one electrical power loss/restoration transition while using a valid directional setup to prove the Stage 5 rescheduler still follows the effective multiplier.

## Colour system (future)

Allowed design colours currently stored in XML metadata: Blue, Green, Red, Purple, White and Yellow. Selection UI, persistence, network sync and runtime tinting are not implemented yet.
