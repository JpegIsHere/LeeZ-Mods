# Changelog

## 0.5.3.0-dev4

- Fixed the explicit old-style C# project manifest so the active transition engine and removal patches are actually compiled: `GrowLightTransitionRescheduler.cs`, `BlockRemovalPatches.cs`, and the new removal installer are now included.
- Wired physical grow-light removal into the V3.1 `BlockPowered.OnBlockRemoved` path so removal can use the same progress-preserving transition engine as electrical changes.
- Made removal capture prefer Harmony's concrete block instance, with `BlockValue` as a fallback, to reduce dependence on an exact V3 callback signature.
- Updated the runtime banner to `v0.5.3-dev4`.
- Added a Stage 0-5 static audit/evidence record. Live V3.1 build/startup, boundary, tier-to-tier, source/relay, and removal validation still remain release-gate work.

## 0.5.3.0-dev3

- Live V3.1 test confirmed progress-preserving mid-stage rescheduling.
- T6 `1x -> 4x` quartered remaining scheduled ticks and `4x -> 1x` multiplied remaining ticks by four.
- T4 `1x <-> 1.5x` transitions also produced the expected proportional reschedule.
- Fixed the non-fatal `HandleDisconnect` Harmony warning by patching the declaring `PowerItem.HandleDisconnect()` implementation directly.
- Direct toggle/power transitions are now live-validated; save/reload and dedicated-server validation remain pending.

## 0.5.3.0-dev2

- Added first live progress-preserving mid-stage crop rescheduler for V3.1.0 b14.
- Added the required `UnityEngine.CoreModule.dll` compile reference for `GameManager.Instance` / MonoBehaviour-linked V3.1 APIs.
- Confirmed and uses V3.1 `WorldBlockTicker.InvalidateScheduledBlockUpdate` and `AddScheduledBlockUpdate`.
- Electrical transitions are observed on PowerConsumerToggle toggle, received-power, propagated-power, and disconnect paths.
- Remaining scheduled time is converted back to equivalent vanilla work using the old effective multiplier, then rescheduled using the new effective multiplier.
- Overlapping lights still use the highest active multiplier; transitions that do not change the effective multiplier do not reschedule.
- This first live candidate intentionally uses the already-validated `IsPowered && IsToggled` state; upstream relay-only propagation is deferred until direct transition behavior is proven.
- Save/restart persistence of transition metadata is not yet claimed by this development build; this build is for live ON/OFF/tier-transition validation first.

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
