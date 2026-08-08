# Colour system handoff (dev8 current baseline)

Date: 2026-08-08  
Game: 7 Days to Die V3.1.0 (b14)  
Branch: `dev/colour-system`  
Known-good colour source baseline: `0a1967e94dcd153e8ad8ff40de545b8b9245903b`  
Runtime banner: `v0.7.0-dev8`  
`ModInfo.xml` version: `0.7.0.0`

> Historical filename retained for links. This document now reflects the dev8 validated colour baseline.

## Current status

The per-light colour system is working in live single-player/local testing.

Validated behaviour:

- Friendly radial-menu labels display correctly, e.g. `Grow light colour: Blue`.
- A single selection advances both the menu label and visible lamp colour immediately.
- All six colours cycle correctly.
- Colour state persists on the placed block.
- Save/quit/restart restores the previously selected colour.
- Normal grow-light power/toggle state remains separate from colour state.
- Growth speed, crop coverage, sunlight substitution and tier behaviour are not changed by colour selection.

Final dev8 regression result: `Grow light colour: Blue` displayed correctly; selecting once advanced the label and visible lamp to Green immediately.

## Colour order and default

`Blue -> Green -> Red -> Purple -> White -> Yellow -> Blue`

Legacy/uninitialised placed lights default to **White**.

## Runtime architecture

### Interaction

`Source/Harmony/GrowLightColourInstaller.cs`

- Patches `GetBlockActivationCommands` through the relevant `BlockPoweredLight`/base hierarchy.
- Patches `OnBlockActivated` through the relevant hierarchy.
- Patches `BlockPoweredLight.OnBlockEntityTransformAfterActivated` and `updateLightState` for visual application/cache population.

`Source/Harmony/GrowLightColourPatches.cs`

- Adds the colour activation command only for LeeZ grow lights.
- V3.1 identifies the selected radial command by `_commandName:String`, not a numeric index.
- dev8 uses stable per-colour command tokens such as `growlightcolour_blue`, with localized display text such as `Grow light colour: Blue`.
- The handler remains compatible with the earlier friendly text-prefix form.
- Vanilla `light` activation continues through vanilla code untouched.
- A successful local/server colour change persists the new block value and immediately calls the cached visual refresh.
- Remote-client authoring is still intentionally rejected until server routing is added.

### Persistence

`Source/Runtime/GrowLightColourState.cs`

- Colour is stored in `BlockValue.meta2`.
- Stored value `0` means legacy/uninitialised and maps to White.
- Stored values `1..6` map to the six colours.
- Electrical toggle state remains in `TileEntityPoweredBlock.isToggled`.
- V3.1 persistence uses `BlockChangeInfo` plus `BlockValueRef` and the normal block-change RPC path.

### Visuals

`Source/Runtime/GrowLightColourVisual.cs`

- Applies `BlockEntityData.SetMaterialColor(colour)`.
- Applies the same colour to child Unity `Light` components.
- Caches live `BlockEntityData` by `Vector3i` using weak references.
- `TryApplyCached(position, updatedValue)` repaints the live lamp immediately after a successful colour write.

## Development history that matters

- dev1: command did not appear.
- dev2: command appeared, activation not recognized.
- dev3: diagnostics proved V3.1 activation uses a string command rather than numeric index.
- dev4: string command recognition worked; persistence exposed the wrong `BlockChangeInfo` constructor assumption.
- dev5: diagnostics exposed `blockValueRef`, `bChangeBlockValue`, and `blockValue`.
- dev6: `BlockValueRef` persistence worked; save/restart restored colour, but live visual changes waited for a rebuild.
- dev7: cached live `BlockEntityData`; immediate live visual cycling passed.
- dev8: replaced raw key-style menu output with stable per-colour localization tokens; friendly menu + immediate visual regression passed.

Important commits:

- `73d40bb44754f421b07d8e7b397dd6f362d7bb94` - dev6 working colour persistence.
- `c7f70f408ba72d8657422caf131a9d2404e5457a` - dev7 immediate live visual refresh.
- `0a1967e94dcd153e8ad8ff40de545b8b9245903b` - dev8 friendly menu localization; current known-good colour source baseline.

## Multiplayer status

Single-player/local behaviour is validated.

Remote client colour authoring is deliberately blocked in `GrowLightColourPatches` until proper server command routing is implemented. Dedicated-server colour synchronization is not yet claimed.

The next development section is **Multiplayer Light Sync**. See `docs/MULTIPLAYER_LIGHT_SYNC_HANDOFF.md`.

## Deferred brightness idea

Player-controlled cosmetic brightness is still planned, but is deferred until Multiplayer Light Sync is addressed. See `ROADMAP.md`.
