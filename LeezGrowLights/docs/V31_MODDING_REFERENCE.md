# 7 Days to Die V3.1 modding reference

This document captures reusable API and engineering lessons confirmed while developing **LeezGrowLights** against **7 Days to Die V3.1.0 (b14)**. It is intended as a starting reference for future DLL/Harmony mods in this repository.

## Evidence levels

- **LIVE VERIFIED** — observed in an actual V3.1 game session.
- **CI VERIFIED** — compiled/probed successfully against a fresh V3.1 dedicated-server installation in GitHub Actions.
- **SOURCE/IL VERIFIED** — confirmed by reflection/IL inspection of the V3.1 game assemblies used by the build pipeline.
- **IMPLEMENTATION NOTE** — a pattern that worked for this mod but should still be rechecked when reused elsewhere.

Do not assume these details remain identical in a later 7DTD release. Re-run the API probes and build workflow after a game update.

## Build against the real game assemblies

**CI VERIFIED.** The reliable build path used by this project is `.github/workflows/build-leezgrowlights-v31.yml`.

The workflow:

1. installs SteamCMD;
2. installs the 7 Days to Die Dedicated Server (`app 294420`);
3. locates the current managed assembly directory;
4. locates TFP Harmony's `0Harmony.dll`;
5. runs the V3.1 reflection/IL probes;
6. compiles the mod with MSBuild against those exact assemblies.

Primary compile references used by LeezGrowLights:

- `Assembly-CSharp.dll`
- `LogLibrary.dll`
- `UnityEngine.CoreModule.dll`
- `0Harmony.dll`

This is preferable to keeping copied game DLLs in Git because it prevents stale binary references from silently compiling against the wrong game build.

## Powered blocks: configured watts versus live consumer watts

**LIVE VERIFIED / SOURCE VERIFIED.** A V3.1 powered light has two concepts that matter when debugging power draw:

- the tile/block's configured required power (for LeezGrowLights this is 10 W in XML);
- the live `PowerItem`/`PowerConsumerToggle` value used by the electrical graph.

A directly wired `PowerConsumerToggle` could continue reserving its configured watts while its lamp toggle was OFF. The visual/electrical toggle state and the live graph load were therefore not equivalent.

The dev11 fix in `Source/Harmony/GrowLightV31Fixes.cs` keeps the block/tile configuration unchanged and synchronizes only the **live consumer load**:

```text
IsToggled = ON  -> live RequiredPower = tile configured watts
IsToggled = OFF -> live RequiredPower = 0 W
```

Useful lifecycle points found for resynchronization:

- `PowerConsumerToggle.IsToggled` setter
- `TileEntityPowered.InitializePowerData`
- `PowerItem.SetValuesFromBlock`
- `PowerConsumerToggle.read`

After changing the live consumer load, the working implementation requests graph/tile refresh through the available root-change and tile-change methods (`SendHasLocalChangesToRoot` and `TileEntityPowered.MarkChanged` when present).

### Reuse warning

Do not globally patch all `PowerConsumerToggle` objects unless the mod genuinely intends to change vanilla power semantics. LeezGrowLights gates the behavior using the grow-light block metadata discovered by `GrowLightScanner`.

## Powered/toggled state for gameplay logic

**LIVE VERIFIED.** Grow-light gameplay behavior uses the powered tile entity rather than inventing a replacement electrical system.

The relevant V3.1 state includes:

- `TileEntityPowered.IsPowered`
- `TileEntityPoweredBlock.IsToggled`
- `PowerConsumerToggle.IsPowered`
- `PowerConsumerToggle.IsToggled`

Keeping custom visual/gameplay metadata separate from the tile's toggle state avoided breaking vanilla wiring behavior.

## Radial activation commands

**LIVE VERIFIED / CI VERIFIED.** `BlockPoweredLight.GetBlockActivationCommands` supplies `BlockActivationCommand[]` entries. LeezGrowLights adds/updates its own commands in a Harmony postfix.

Important findings:

- V3.1 activation handling identifies the selected command by a **string command name**, not by a stable custom numeric index.
- Stable custom command tokens are safer than treating localized display text as program logic.
- LeezGrowLights uses prefixes such as `growlightcolour_` and `growlightbrightness_` and lets `Localization.csv` provide the user-facing text.
- `BlockActivationCommand.icon` can be blank even when the command is otherwise valid.
- Known built-in icon keys successfully used by this project include `tool` and `wrench`; existing vanilla commands also demonstrated keys such as `electric_switch` and `hand`.

The dev11 icon repair is deliberately installed after the colour/brightness command builder and runs its postfix at `Priority.Last`, so it sees the final command array and fills only blank icons.

## Menu labels should describe the click action

**CI VERIFIED; underlying cycle behavior previously LIVE VERIFIED.** For cycling radial commands, the text should advertise the state that clicking will apply, not the current state.

Colour already followed this rule. The dev12 brightness fix changed the menu token from `current brightness` to `Next(current brightness)`.

Correct brightness offer sequence:

```text
current Dim         -> menu offers Normal
current Normal      -> menu offers Bright
current Bright      -> menu offers Very Bright
current Very Bright -> menu offers Maximum
current Maximum     -> menu offers Dim
```

This pattern is reusable for any cyclic radial selector.

## Per-block state in `BlockValue.meta2`

**LIVE VERIFIED / CI PROBED.** LeezGrowLights stores per-placed-light colour and brightness in `BlockValue.meta2` rather than mixing cosmetic state with electrical toggle state.

The project includes dedicated storage probes and CI metadata-capacity probes. Re-run them before expanding the encoding or reusing the approach on a later game version.

Relevant files:

- `tools/Probe-ColourStorage.ps1`
- `.github/workflows/build-leezgrowlights-v31.yml`
- `Source/Runtime/GrowLightColourState.cs`

For authoritative block-state changes, the working V3.1 path uses the game's block-change/RPC structures (`BlockChangeInfo` / `BlockValueRef`) rather than only mutating a local struct copy.

## Visual colour and brightness

**LIVE VERIFIED.** Colour is applied to the live block entity/material and child Unity lights. Brightness uses a multiplier over the light's tracked base intensity.

A key V3.1 discovery was that `LightLOD.FrameUpdate` can rewrite Unity `Light.intensity` continuously. Applying a brightness value once is therefore not sufficient.

The working solution:

1. cache the grow light's visual state;
2. track base/vanilla intensity separately from the selected multiplier;
3. patch `LightLOD.FrameUpdate` with a postfix;
4. reapply the LeeZ brightness multiplier only to registered LeeZ light components.

This avoids progressive compounding and prevents unrelated lights from being modified.

Relevant files:

- `Source/Runtime/GrowLightColourVisual.cs`
- `Source/Harmony/GrowLightColourInstaller.cs`
- `tools/Probe-ColourApi.ps1`

## Crop scheduling and progress-preserving speed changes

**LIVE VERIFIED.** When a crop's effective multiplier changes mid-stage, LeezGrowLights preserves already-earned progress by converting the queued remaining time back to equivalent vanilla work and then rescheduling that remaining work with the new multiplier.

Conceptually:

```text
remaining vanilla work = remaining queued ticks * old multiplier
new remaining ticks    = remaining vanilla work / new multiplier
```

The working V3.1 scheduling path uses `WorldBlockTicker` invalidation/rescheduling APIs. See:

- `Source/Runtime/GrowLightTransitionRescheduler.cs`
- `Source/Runtime/TickerScheduleAccessor.cs`
- `MIDSTAGE_TESTING.md`
- `docs/STAGE_0_5_STATUS.md`

## Server authority and multiplayer

**IMPLEMENTATION NOTE / PARTIALLY PROBED.** Local single-player/host behavior and server-side state changes are proven. Full remote-client colour/brightness authoring and visual synchronization are intentionally not claimed complete.

The current runtime rejects unsupported remote-client authoring rather than pretending a local-only visual change is authoritative.

Future work should preserve this order:

1. client sends a small intent/request;
2. server validates the targeted block and requested operation;
3. server changes authoritative block state;
4. authoritative state replicates to clients;
5. each client refreshes its live visual from the replicated state.

See:

- `docs/MULTIPLAYER_LIGHT_SYNC_HANDOFF.md`
- `docs/MULTIPLAYER_V31_B14_API_EVIDENCE.md`
- `docs/MULTIPLAYER_RUNTIME_EVIDENCE_2026-08-08.md`
- `tools/Probe-MultiplayerLightSync.ps1`

## Harmony patching practices that worked well

- Resolve exact game types/methods at runtime where V3.1 overloads or inheritance paths vary.
- Patch compatible methods across a hierarchy when the game can dispatch through a base implementation.
- Deduplicate methods by module/token before patching.
- Keep patches narrowly gated to the mod's own blocks/entities.
- Log installed hooks at startup so a changed game API is obvious.
- Prefer fail-safe behavior if reflection cannot find a field/method.
- Use postfix priority intentionally when one mod patch must observe another patch's final result.

## Diagnostic workflow for future mods

A productive sequence during this project was:

1. reproduce one exact in-game symptom;
2. identify the smallest relevant game type/method;
3. extend a reflection/IL probe rather than guessing signatures;
4. compile against a fresh real V3.1 server install;
5. add low-noise runtime logging around the suspected state transition;
6. create an isolated test build;
7. validate the exact regression matrix in game;
8. only then promote/publish the build;
9. write the confirmed API finding back into the repo.

This repository now keeps the probe scripts, CI workflow, handoffs and release evidence so that process can be reused for later mods.