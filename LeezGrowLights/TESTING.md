# Testing status

Target test build: **7 Days to Die V3.1.0 (b14)**.

Current runtime candidate: **v0.5.3-dev4**, code commit `e4bffacff6ef18c4b6faad41439627c1c142e381` on `dev/midstage-reschedule-probe`.

Legend: `PASS` = observed in-game/build output; `STATIC VERIFIED` = source/XML/project structure proves the intended rule but exact gameplay still needs a live run; `IMPLEMENTED` = code path exists and is wired but the exact scenario still needs validation; `PENDING` = not complete.

For the detailed Stage 0-5 gate, see `docs/STAGE_0_5_STATUS.md`.

## Build and startup

| Test | Status | Notes |
|---|---|---|
| Stable v0.5.1 Release build | PASS | 0 warnings, 0 errors on the V3.1 test installation |
| v0.5.3-dev1 Release build | FAIL (fixed in dev2) | missing explicit `UnityEngine.CoreModule.dll` reference |
| v0.5.3-dev2 Release build | PASS | DLL loaded as `LeezGrowLights 0.5.3.0` and runtime banner reported `v0.5.3-dev2` |
| v0.5.3-dev4 project compile manifest | STATIC VERIFIED | fixed missing explicit compile entries for transition/removal source; Release build on target installation still required |
| crop scheduling hook | PASS | installed at startup on prior candidate; re-run on dev4 |
| crop tick-rate hook | PASS | installed at startup on prior candidate; re-run on dev4 |
| crop update context hook | PASS | installed at startup on prior candidate; re-run on dev4 |
| sunlight substitution hooks | PASS | 11 crop/base methods patched during prior startup; re-run on dev4 |
| V3.1 ticker API probe | PASS | confirmed live ticker invalidation/add APIs, scheduled entry time and `GetWBT()` |
| v0.5.3-dev3 disconnect patch target | IMPLEMENTED | patches declaring `PowerItem.HandleDisconnect()` directly; still needs one clean startup confirmation |
| v0.5.3-dev4 removal hook | IMPLEMENTED | installs shared transition handling on V3.1 `BlockPowered.OnBlockRemoved`; live startup/removal validation pending |

## Electrical behavior

| Test | Status | Notes |
|---|---|---|
| vanilla wiring | PASS | tier lights are usable in-game |
| powered + switched on | PASS | active lamp detected |
| switched off | PASS | state transition observed during testing |
| power draw | STATIC VERIFIED | XML requests 10 W for each tier |
| toggle transition hook | PASS | `PowerConsumerToggle.set_IsToggled` installed on prior candidate |
| received-power hook | PASS | `PowerConsumerToggle.HandlePowerReceived` installed on prior candidate |
| propagated-power hook | PASS (hook only) | `PowerConsumerToggle.HandlePowerUpdate` installed; upstream relay-only behavior still needs a dedicated test |
| disconnect hook | IMPLEMENTED | dev3 corrects the dev2 inherited-method Harmony warning; clean dev4 startup confirmation pending |

## Coverage and underground farming

| Test | Status | Notes |
|---|---|---|
| fully enclosed planting with active light | PASS | underground planting works; re-run on dev4 |
| no global sunlight bypass | STATIC VERIFIED | substitution is conditional on active LeeZ coverage |
| 5x5 footprint | PASS | core scanner/tier system works in-game |
| X/Z radius 2 | STATIC VERIFIED | scanner limit is inclusive radius 2 |
| X/Z radius 3 rejected | STATIC VERIFIED; LIVE PENDING | offset 3 is not scanned; record explicit boundary evidence |
| Y+1 valid | STATIC VERIFIED; LIVE PENDING | range includes 1; isolate explicit boundary test |
| Y+10 valid | PASS | active T6 detected 10 blocks above farm plot |
| Y+11 rejected | STATIC VERIFIED; LIVE PENDING | scanner stops at 10; explicit boundary evidence pending |
| sunlight/speed geometry match | STATIC VERIFIED; LIVE PENDING | both paths delegate to the same farm-footprint scanner |

## Tier multipliers

| Tier | XML multiplier | Status |
|---|---:|---|
| T1 | 1.2x | STATIC VERIFIED; prior tier-system user verification recorded |
| T2 | 1.3x | STATIC VERIFIED; prior tier-system user verification recorded |
| T3 | 1.4x | STATIC VERIFIED; prior tier-system user verification recorded |
| T4 | 1.5x | PASS; live mid-stage proportional reschedule observed |
| T5 | 1.6x | STATIC VERIFIED; prior tier-system user verification recorded |
| T6 | 4.0x | PASS; 4x runtime detection and live mid-stage reschedule observed |

Overlap selection is implemented as highest-active-multiplier-wins. The transition engine skips rescheduling when the effective multiplier does not change. Isolated overlap transition tests remain pending.

## Mid-stage progress preservation

| Test | Status | Notes |
|---|---|---|
| live V3.1 ticker lookup | PASS | probe confirmed `scheduledTicksDict` and `WorldBlockTickerEntry.scheduledTime` |
| invalidate/re-add scheduled crop tick | PASS | live dev2 test successfully rescheduled existing crop updates |
| remaining-work conversion old -> new multiplier | PASS | live tick counts changed by the expected multiplier ratio |
| T6 `1x -> 4x` | PASS | example: `105436 -> 26359` ticks; approximately one quarter |
| T6 `4x -> 1x` | PASS | example: `26055 -> 104220` ticks; exactly 4x |
| T6 `1x -> 4x` after OFF period | PASS | example: `103945 -> 25986` ticks; approximately one quarter |
| T4 `1x -> 1.5x` | PASS | example: `24870 -> 16580` ticks; expected 2/3 remaining time |
| T4 `1.5x -> 1x` | PASS | example: `16774 -> 25161` ticks; expected 1.5x remaining time |
| already-earned progress preserved | PASS | rescheduling operates only on remaining queued work; live ratios match design |
| T1 -> T6 / T6 -> T1 | IMPLEMENTED; LIVE PENDING | shared old-effective -> new-effective conversion is in place; isolated test required |
| lower tier toggled while T6 remains effective | STATIC VERIFIED; LIVE PENDING | equality guard skips reschedule when effective multiplier is unchanged |
| direct source power loss/restoration | IMPLEMENTED; LIVE PENDING | received/update/disconnect paths are patched; source-side outage requires explicit test |
| upstream relay propagation | IMPLEMENTED; LIVE PENDING | propagated-power hook is installed; relay-only scenario pending |
| lamp removal mid-stage | IMPLEMENTED IN DEV4; LIVE PENDING | removal installer patches `BlockPowered.OnBlockRemoved` and uses shared transition engine |
| lower-tier lamp removal under higher winner | IMPLEMENTED IN DEV4; LIVE PENDING | shared equality guard should prevent unnecessary reschedule |
| save/reload during partial stage | PENDING | persistence behavior not yet validated |
| chunk unload/reload | PENDING | not yet validated |

## Pending correctness / release tests

- Release-build v0.5.3-dev4 against the exact V3.1.0 b14 installation and confirm a clean startup.
- Confirm all four electrical hooks plus the grow-light removal hook install without Harmony warnings.
- Record explicit X/Z offset 3 and Y+11 rejection evidence.
- Live-validate T1 -> T6 and T6 -> T1.
- Live-validate lower-tier toggle while T6 remains effective produces no reschedule.
- Live-validate direct source power loss/restoration and upstream relay propagation.
- Live-validate effective-lamp removal and lower-tier removal while a higher tier remains effective.
- Save/reload and chunk unload/reload.
- Dedicated server with remote client.
- Long-duration crop survival in a sealed room while powered.
- Loss of power underground and vanilla survival response.
- Performance with dense farms / many lamps.

## Colour system (future)

Allowed design colours currently stored in XML metadata:

- Blue
- Green
- Red
- Purple
- White
- Yellow

Selection UI, persistence, network sync and runtime tinting are not implemented yet.
