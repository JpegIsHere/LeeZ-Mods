# Colour system dev7 handoff

Date: 2026-08-08  
Game: 7 Days to Die V3.1.0 (b14)  
Branch: `dev/colour-system`  
Known-good colour code baseline: `c7f70f408ba72d8657422caf131a9d2404e5457a`  
Runtime banner: `v0.7.0-dev7`  
`ModInfo.xml` version: `0.7.0.0`

## Current status

The per-light colour system is working in live single-player testing.

Validated behaviour:

- A LeeZ grow light exposes a colour radial-menu command.
- The command cycles through all six configured colours.
- Colour state persists on the placed block.
- Save/quit/restart restores the previously selected colour.
- The visible lamp colour now updates immediately while the game is running; no restart is required.
- The normal grow-light power/toggle system remains separate from colour state.
- Growth speed, crop coverage, sunlight substitution and tier behaviour are not changed by colour selection.

User validation at the dev7 gate: "the lights are now cycling through the colours perfectly."

## Colour order and default

`GrowLightColourPalette` uses:

1. Blue
2. Green
3. Red
4. Purple
5. White
6. Yellow

Cycle order:

`Blue -> Green -> Red -> Purple -> White -> Yellow -> Blue`

Legacy/uninitialised placed lights default to **White**.

## Runtime architecture

### Interaction

`Source/Harmony/GrowLightColourInstaller.cs`

- Patches `GetBlockActivationCommands` through the relevant `BlockPoweredLight`/base hierarchy.
- Patches `OnBlockActivated` through the relevant hierarchy.
- Patches `BlockPoweredLight.OnBlockEntityTransformAfterActivated` and `updateLightState` for visual application/cache population.

`Source/Harmony/GrowLightColourPatches.cs`

- Adds the dynamic colour activation command only for blocks recognized as LeeZ grow lights.
- V3.1 identifies the selected radial command by the `_commandName` string, not by a numeric command index.
- The colour handler therefore recognizes command names beginning with `Grow light colour:`.
- Vanilla `light` activation continues through vanilla code untouched.
- On a successful local/server colour change, the handler persists the new block value and immediately calls the cached live visual refresh.
- Remote-client authoring is intentionally not implemented yet; a remote world is rejected with a warning until server command routing is added.

### Persistence

`Source/Runtime/GrowLightColourState.cs`

- Colour is stored in `BlockValue.meta2`.
- Stored value `0` means legacy/uninitialised and maps to White.
- Stored values `1..6` map to the six colours.
- Electrical toggle state remains in `TileEntityPoweredBlock.isToggled`; the colour system does not replace the powered tile entity.
- V3.1 persistence uses `BlockChangeInfo` plus `BlockValueRef` and the normal block RPC path.
- Dev4/dev5 assumptions about a direct `BlockChangeInfo(Vector3i, BlockValue, ...)` shape were wrong; dev6 added the working `BlockValueRef` construction path.

### Visuals

`Source/Runtime/GrowLightColourVisual.cs`

- Applies `BlockEntityData.SetMaterialColor(colour)`.
- Applies the same colour to child Unity `Light` components.
- Caches the live `BlockEntityData` by `Vector3i` using weak references when the game builds/refreshes the block entity.
- After a successful colour write, `TryApplyCached(position, updatedValue)` repaints the already-live lamp immediately.
- This cache-based live refresh was the dev7 fix that removed the previous restart requirement.

## Development history that matters

- dev1: first runnable colour build; command did not appear.
- dev2: command appeared, but activation was not recognized.
- dev3: diagnostics proved V3.1 activation did not provide the expected numeric index.
- dev4: switched activation handling to `_commandName`; persistence then exposed the incorrect `BlockChangeInfo` constructor assumption.
- dev5: runtime diagnostics exposed `BlockChangeInfo.blockValueRef`, `bChangeBlockValue`, and `blockValue`.
- dev6: implemented `BlockValueRef`-based persistence. Colour names cycled and save/restart restored the selected visual colour, but live colour changes did not repaint immediately.
- dev7: cached live `BlockEntityData` and repainted it after each successful colour write. Live colour cycling passed.

Important commits:

- `76395da082e96887725283abd2a75b57d70f678e` - first runnable dev1 colour build.
- `f30c03d2cb3848acee33dbde16b260131474660f` - dev2 baseline.
- `6a24b0340d53c0a4b9ff9f08152adf0eba55de13` - dev3 activation diagnostics.
- `ccc938a0b6d0b6b548e8504e8333a11ed2123f54` - dev4 string command activation.
- `6b75edcd9354cc25995ed79b723866a30b01040a` - dev5 persistence diagnostics.
- `73d40bb44754f421b07d8e7b397dd6f362d7bb94` - dev6 working colour persistence.
- `c7f70f408ba72d8657422caf131a9d2404e5457a` - dev7 immediate live visual refresh baseline.

## Known cosmetic issue

The radial menu can still display the raw-looking text form such as:

`blockcommand_growlightcolour: Blue`

`Config/Localization.csv` contains `blockcommand_growlightcolour = Grow light colour`, but the dynamic suffix/activation-command localization behaviour still results in the raw key-style label in testing. This is cosmetic and does not block colour cycling, persistence or visuals.

## Multiplayer status

Single-player/local-server behaviour is the validated target at this point.

Remote client colour authoring is still deliberately blocked in `GrowLightColourPatches` until proper server command routing is implemented. Do not claim multiplayer colour synchronization is finished.

## Next-chat target: brightness interaction

Requested follow-up: add optional player-controlled lamp brightness for fun/cosmetic interaction.

Recommended design starting point:

- Add a second radial-menu command independent of colour, e.g. `Grow light brightness: Normal`.
- Keep brightness cosmetic only: do not modify crop growth multiplier, coverage, artificial sunlight logic, or electrical power draw unless deliberately redesigned later.
- Reuse the live visual cache already proven in dev7.
- Change Unity `Light.intensity` on the cached child `Light` components.
- Candidate levels: Dim, Normal, Bright, Very Bright, Maximum.
- Persist brightness per placed light. One compact option is to encode colour + brightness together in `BlockValue.meta2`; six colours x five brightness levels is 30 states and fits easily. Preserve compatibility by interpreting existing dev7 values `1..6` as their current colours at Normal brightness.
- Do not destabilize the now-working colour persistence or live-refresh path while adding brightness.

## First checks for the next chat

Before coding brightness:

1. Read this file and `docs/COLOUR_SYSTEM.md`.
2. Fetch the current `GrowLightColourVisual.cs`, `GrowLightColourPatches.cs`, and `GrowLightColourState.cs` from `dev/colour-system`.
3. Treat `c7f70f408ba72d8657422caf131a9d2404e5457a` as the known-good colour behaviour baseline.
4. Keep brightness changes cosmetic and independently testable.
