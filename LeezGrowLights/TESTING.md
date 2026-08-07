# Testing status

Target test build: **7 Days to Die V3.1.0 (b14)**.

Legend: `PASS` = observed in-game/build output; `IMPLEMENTED` = code path exists but the exact scenario still needs an isolated test; `PENDING` = not complete.

## Build and startup

| Test | Status | Notes |
|---|---|---|
| Stable v0.5.1 Release build | PASS | 0 warnings, 0 errors on the V3.1 test installation |
| v0.5.3-dev1 Release build | FAIL (fixed in dev2) | missing explicit `UnityEngine.CoreModule.dll` reference for `GameManager.Instance` / `MonoBehaviour` type chain |
| v0.5.3-dev2 Release build | PENDING | project now includes `UnityEngine.CoreModule.dll`; awaiting user rebuild |
| DLL discovered by mod loader | PASS | stable v0.5.1 and probe v0.5.2 loaded |
| crop scheduling hook | PASS | installed at startup |
| crop tick-rate hook | PASS | installed at startup |
| crop update context hook | PASS | installed at startup |
| sunlight substitution hooks | PASS | 11 crop/base methods patched during startup |
| V3.1 ticker API probe | PASS | confirmed live ticker invalidation/add APIs, scheduled entry time and `GetWBT()` |

## Electrical behavior

| Test | Status | Notes |
|---|---|---|
| vanilla wiring | PASS | tier lights are usable in-game |
| powered + switched on | PASS | active lamp detected |
| switched off | PASS | state transition observed during testing |
| power draw | IMPLEMENTED | XML currently requests 10 W for each tier |
| electrical transition Harmony hooks | IMPLEMENTED | v0.5.3-dev2 hooks toggle, power-received/update and disconnect paths |

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
| T4 | 1.5x | PASS (tier system user-verified) |
| T5 | 1.6x | PASS (tier system user-verified) |
| T6 | 4.0x | PASS; 4x runtime detection observed in log |

Overlap behavior is implemented as highest-active-multiplier-wins and should receive an isolated multi-light test before release.

## Mid-stage progress preservation

| Test | Status | Notes |
|---|---|---|
| live V3.1 ticker lookup | PASS | probe confirmed `scheduledTicksDict` and `WorldBlockTickerEntry.scheduledTime` |
| invalidate/re-add scheduled crop tick | IMPLEMENTED | v0.5.3-dev2 uses validated `InvalidateScheduledBlockUpdate` and `AddScheduledBlockUpdate` |
| remaining-work conversion old -> new multiplier | IMPLEMENTED | queued remaining ticks are converted back to equivalent vanilla work before rescheduling |
| T6 ON -> OFF | PENDING | expect ~4x expansion of remaining ticks |
| T6 OFF -> ON | PENDING | expect ~4x reduction of remaining ticks |
| T1 -> T6 / T6 -> T1 | PENDING | preserve already-earned progress |
| lower tier toggled while T6 remains effective | PENDING | should not reschedule because effective multiplier is unchanged |
| direct power loss/restoration | PENDING | transition hooks implemented; live behavior not yet proven |
| save/reload during partial stage | PENDING | not claimed by v0.5.3-dev2 |

## Pending correctness / release tests

- Remove a lamp midway through a stage.
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
