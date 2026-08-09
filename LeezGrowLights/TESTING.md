# Testing status

Target test build: **7 Days to Die V3.1.0 (b14)**.

Legend: `PASS` = observed in-game/build output; `IMPLEMENTED` = code path exists but the exact boundary scenario still needs an isolated test; `PENDING` = not complete.

## Build and startup

| Test | Status | Notes |
|---|---|---|
| Release build | PASS | 0 warnings, 0 errors on the V3.1 test installation |
| DLL discovered by mod loader | PASS | `LeezGrowLights 0.5.1.0` loaded |
| crop scheduling hook | PASS | installed at startup |
| crop tick-rate hook | PASS | installed at startup |
| crop update context hook | PASS | installed at startup |
| sunlight substitution hooks | PASS | 11 crop/base methods patched during startup |
| grow-light power-draw hooks | IMPLEMENTED | requires build/startup verification for this fix branch |
| grow-light radial icon hook | IMPLEMENTED | requires build/startup verification for this fix branch |

## Electrical behavior

| Test | Status | Notes |
|---|---|---|
| vanilla wiring | PASS | tier lights are usable in-game |
| powered + switched on | PASS | active lamp detected |
| switched off | PASS | state transition observed during testing |
| configured lamp load | PASS | XML requests 10 W for each tier |
| direct generator -> light ON | IMPLEMENTED | expected draw: 10 W |
| direct generator -> light OFF | IMPLEMENTED | expected draw: 0 W; live PowerConsumerToggle load is forced to zero while toggled off |
| external switch OFF -> light | PASS | troubleshooting observation reported 0 W draw |
| external switch ON -> light OFF | IMPLEMENTED | expected draw: 0 W |
| reload world while light OFF | IMPLEMENTED | power load is re-synchronized after tile deserialization |
| toggle light back ON | IMPLEMENTED | live consumer load restores the configured 10 W |

## Powered-light radial menu

| Command | Status | Notes |
|---|---|---|
| Switch | PASS | switch icon already visible |
| Take | PASS | hand icon already visible |
| Color selector | IMPLEMENTED | blank inherited icon is filled with built-in `tool` icon |
| Brightness selector | IMPLEMENTED | blank inherited icon is filled with built-in `wrench` icon |

The icon repair only fills empty icon names on LeeZ grow lights. Existing command text, enabled state, highlighting and activation handlers are preserved.

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
| Y+11 rejected | PENDING | next boundary test |

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

## Pending correctness tests

- Build the fix branch against the installed V3.1 managed assemblies and confirm zero compile warnings/errors.
- Direct-wire a grow light to a generator: confirm ON = 10 W and OFF = 0 W.
- Save/reload with the directly-wired grow light OFF: confirm it remains at 0 W.
- Toggle the same light ON after reload: confirm draw returns to 10 W.
- Confirm the color and brightness radial slots display their replacement icons and still open their original functions.
- Turn light on midway through a crop stage and preserve only earned vanilla progress.
- Turn light off midway through a crop stage without losing or gifting progress.
- Remove a lamp midway through a stage.
- Save/reload during partially accelerated growth.
- Multiple overlapping tiers: confirm only highest active tier wins.
- Dedicated server with remote client.
- Long-duration crop survival in a sealed room while powered.
- Loss of power underground and vanilla survival response.

## Colour system (future)

Allowed design colours currently stored in XML metadata:

- Blue
- Green
- Red
- Purple
- White
- Yellow

Custom LeeZ colour persistence/network sync/runtime tinting remains a future system; the current radial icon repair only restores a visible icon for the inherited powered-light color command.
