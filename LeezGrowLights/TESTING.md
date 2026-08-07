# Testing status

Target test build: **7 Days to Die V3.1.0 (b14)**.

Legend: `PASS` = observed in-game/build output; `IMPLEMENTED` = code path exists but the exact scenario still needs an isolated test; `PENDING` = not complete.

## Build and startup

| Test | Status | Notes |
|---|---|---|
| Stable v0.5.1 Release build | PASS | 0 warnings, 0 errors on the V3.1 test installation |
| v0.5.3-dev1 Release build | FAIL (fixed in dev2) | missing explicit `UnityEngine.CoreModule.dll` reference |
| v0.5.3-dev2 Release build | PASS | DLL loaded as `LeezGrowLights 0.5.3.0` and runtime banner reported `v0.5.3-dev2` |
| crop scheduling hook | PASS | installed at startup |
| crop tick-rate hook | PASS | installed at startup |
| crop update context hook | PASS | installed at startup |
| sunlight substitution hooks | PASS | 11 crop/base methods patched during startup |
| V3.1 ticker API probe | PASS | confirmed live ticker invalidation/add APIs, scheduled entry time and `GetWBT()` |
| v0.5.3-dev3 disconnect patch target | IMPLEMENTED | patches declaring `PowerItem.HandleDisconnect()` directly; needs one startup confirmation |

## Electrical behavior

| Test | Status | Notes |
|---|---|---|
| vanilla wiring | PASS | tier lights are usable in-game |
| powered + switched on | PASS | active lamp detected |
| switched off | PASS | state transition observed during testing |
| power draw | IMPLEMENTED | XML currently requests 10 W for each tier |
| toggle transition hook | PASS | `PowerConsumerToggle.set_IsToggled` installed |
| received-power hook | PASS | `PowerConsumerToggle.HandlePowerReceived` installed |
| propagated-power hook | PASS (hook only) | `PowerConsumerToggle.HandlePowerUpdate` installed; upstream relay-only behavior still needs a dedicated test |
| disconnect hook | IMPLEMENTED | dev3 corrects the dev2 inherited-method Harmony warning |

## Coverage and underground farming

| Test | Status | Notes |
|---|---|---|
| fully enclosed planting with active light | PASS | underground planting works |
| no global sunlight bypass | IMPLEMENTED | substitution is conditional on active LeeZ coverage |
| 5x5 footprint | PASS | core scanner/tier system works in-game |
| X/Z radius 2 | IMPLEMENTED | scanner limit is 2 |
| X/Z radius 3 rejected | PENDING | isolate and record explicit boundary test |
| Y+1 valid | IMPLEMENTED | range includes 1; isolate explicit boundary test |
| Y+10 valid | PASS | active T6 detected 10 blocks above farm plot |
| Y+11 rejected | PENDING | boundary test still pending |

## Tier multipliers

| Tier | XML multiplier | Status |
|---|---:|---|
| T1 | 1.2x | PASS (tier system user-verified) |
| T2 | 1.3x | PASS (tier system user-verified) |
| T3 | 1.4x | PASS (tier system user-verified) |
| T4 | 1.5x | PASS; live mid-stage proportional reschedule observed |
| T5 | 1.6x | PASS (tier system user-verified) |
| T6 | 4.0x | PASS; 4x runtime detection and live mid-stage reschedule observed |

Overlap behavior is implemented as highest-active-multiplier-wins and should receive an isolated multi-light transition test before release.

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
| T1 -> T6 / T6 -> T1 | PENDING | tier-to-tier overlap/replacement transition still needs isolated test |
| lower tier toggled while T6 remains effective | PENDING | should not reschedule because effective multiplier is unchanged |
| direct source power loss/restoration | PENDING | direct lamp toggle is proven; source-side outage needs explicit test |
| lamp removal mid-stage | PENDING | not yet implemented/validated as a transition source |
| save/reload during partial stage | PENDING | persistence behavior not yet validated |
| chunk unload/reload | PENDING | not yet validated |

## Pending correctness / release tests

- Confirm v0.5.3-dev3 installs all four transition hooks with no Harmony warning.
- Remove a lamp midway through a stage and preserve progress.
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
