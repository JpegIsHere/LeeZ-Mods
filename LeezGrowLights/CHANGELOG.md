# Changelog

## 0.5.1.0

- Changed the grow-light height rule from fixed Y+6 to Y+1 through Y+10 above the farm plot.
- Lamps at Y+11 or higher are ignored.
- The same height range applies to artificial sunlight and growth acceleration.
- Horizontal coverage and tier multipliers are unchanged.

## 0.5.0.0

- Added underground farming / artificial-sunlight support.
- A powered and switched-on LeeZ grow light now temporarily satisfies vanilla crop light thresholds during placement, survival, and growth checks.
- Vanilla farm-plot, space, and other plant validation remains in control; sunlight is not disabled globally.
- An unpowered or switched-off LeeZ light does not bypass the vanilla light requirement.
- Sunlight replacement uses the same exact 5x5 footprint and +6Y farm-block geometry as growth acceleration.
- Placement-side scanning runs on both client and server so enclosed-room placement can be accepted locally and authoritatively.
- Added startup diagnostics listing every V3.1 crop method receiving the sunlight substitution hook.

## 0.4.2.0

- Added temporary low-noise in-game diagnostics for first grow-light validation.
- Logs each discovered Leez lamp when its ACTIVE/INACTIVE state changes.
- Logs the multiplier applied when a crop has an active grow light.
- Corrected the runtime startup banner to match the package version.
- No grow-speed balance or coverage behavior changes.

## 0.4.1.0

- Added the missing `LogLibrary.dll` project reference required by `LeezLog.cs`.
- No runtime behaviour changes from 0.4.0.0.
- This fixes compile errors CS0103 for `Log.Out`, `Log.Warning`, and `Log.Error`.

## 0.4.0.0

- Integrated successful API probe from the exact V3.1 installation.
- Confirmed `BlockPlantGrowing.GetTickRate()` returns `UInt64`.
- Confirmed `addScheduledTick` and `UpdateTick` exist.
- Removed `GetParameters()` dependency from crop method discovery.
- Removed the old `bool UpdateTick` return-type assumption.
- Confirmed `TileEntityPowered.IsPowered`.
- Confirmed `TileEntityPoweredBlock.IsToggled`.
- Confirmed `PowerConsumerToggle.IsPowered` / `IsToggled`.
- Replaced generic reflection-based electrical guessing with direct V3.1 game types.
- Added successful V3.1 probe report to `Reference/`.
- Added `Source/API_VALIDATION_V3.1.md`.
- Exact mid-stage power-transition re-scheduling and colour persistence/sync remain pending.

## 0.3.0.0

- Added C#/.NET Framework 4.8 runtime project.
- Added V3.x `IModApi.InitMod(Mod)` Harmony entry point.
- Added runtime discovery of `BlockPlantGrowing` scheduling methods.
- Added exact 5x5 / +6Y grow-light scanner.
- Added highest-tier-wins multiplier lookup from XML metadata.
- Added server-only, fail-safe powered/switch-state resolver.
- Added tick-rate speed conversion hook.
- Added V3.1 `Assembly-CSharp.dll` API probe script.
- Added build instructions and expanded runtime design notes.
- Dynamic mid-stage power transitions and colour persistence/sync remain pending exact V3.1 API validation.

## 0.2.0.0

- Switched the six grow lights to the flat vanilla white LED panel visual.
- Based grow lights on `ceilingLight01_player` for player wiring/on-off behaviour.
- Added provisional 10 W power draw, tier multipliers, footprint metadata and Wiring 101 unlocks.
