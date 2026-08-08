# Colour system handoff (dev8 current baseline)

Date: 2026-08-08  
Game: 7 Days to Die V3.1.0 (b14)  
Branch: `dev/colour-system`  
Known-good colour source baseline: `0a1967e94dcd153e8ad8ff40de545b8b9245903b`  
Runtime banner: `v0.7.0-dev8`  
`ModInfo.xml` version: `0.7.0.0`

> Historical filename retained for links. This document reflects the dev8 validated colour baseline. Brightness work now has its own handoff at `docs/BRIGHTNESS_DEV8_HANDOFF.md`.

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

Final dev8 colour regression result: `Grow light colour: Blue` displayed correctly; selecting once advanced the label and visible lamp to Green immediately.

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

- Adds LeeZ-only visual-state activation commands.
- V3.1 identifies the selected radial command by `_commandName:String`, not a numeric index.
- dev8 uses stable per-colour command tokens such as `growlightcolour_blue`, with localized display text such as `Grow light colour: Blue`.
- The handler remains compatible with the earlier friendly text-prefix form.
- Vanilla `light` activation continues through vanilla code untouched.
- A successful local/server colour change persists the new block value and immediately calls the cached visual refresh.
- Remote-client authoring is still intentionally rejected until server routing is added.

### Persistence

`Source/Runtime/GrowLightColourState.cs`

The original colour-only dev8 baseline stored colour in `BlockValue.meta2` with values `0..6`. The current brightness candidate extends the same field while preserving compatibility:

- Stored value `0` means legacy/uninitialised and maps to White + Normal brightness.
- Stored values `1..6` remain the six original colours at Normal brightness.
- Extended values encode the same colours at the other brightness levels.
- Electrical toggle state remains in `TileEntityPoweredBlock.isToggled`.
- V3.1 persistence uses `BlockChangeInfo` plus `BlockValueRef` and the normal block-change RPC path.

See `docs/BRIGHTNESS_DEV8_HANDOFF.md` for the full 30-value encoding.

### Visuals

`Source/Runtime/GrowLightColourVisual.cs`

- Applies `BlockEntityData.SetMaterialColor(colour)`.
- Applies the same colour to child Unity `Light` components.
- Caches live `BlockEntityData` by `Vector3i` using weak references.
- `TryApplyCached(position, updatedValue)` repaints the live lamp immediately after a successful visual-state write.
- The current brightness candidate also applies a cosmetic intensity multiplier to child Unity lights while tracking their vanilla/base intensity to avoid repeated multiplier compounding.

## Development history that matters

- dev1: command did not appear.
- dev2: command appeared, activation not recognized.
- dev3: diagnostics proved V3.1 activation uses a string command rather than numeric index.
- dev4: string command recognition worked; persistence exposed the wrong `BlockChangeInfo` constructor assumption.
- dev5: diagnostics exposed `blockValueRef`, `bChangeBlockValue`, and `blockValue`.
- dev6: `BlockValueRef` persistence worked; save/restart restored colour, but live visual changes waited for a rebuild.
- dev7: cached live `BlockEntityData`; immediate live visual cycling passed.
- dev8 colour: replaced raw key-style menu output with stable per-colour localization tokens; friendly menu + immediate visual regression passed.
- dev8 brightness candidate: added the second brightness radial command, combined colour/brightness `meta2` encoding, and cosmetic intensity multipliers. This portion still requires live validation.

Important colour commits:

- `73d40bb44754f421b07d8e7b397dd6f362d7bb94` - dev6 working colour persistence.
- `c7f70f408ba72d8657422caf131a9d2404e5457a` - dev7 immediate live visual refresh.
- `0a1967e94dcd153e8ad8ff40de545b8b9245903b` - dev8 friendly menu localization; known-good validated colour source baseline.

Brightness candidate commits and validation gates are listed in `docs/BRIGHTNESS_DEV8_HANDOFF.md`.

## Multiplayer status

Single-player/local colour behaviour is validated.

Remote client colour/brightness authoring is deliberately blocked in `GrowLightColourPatches` until proper server command routing is implemented. Dedicated-server visual-state synchronization has not yet been validated.

See `docs/MULTIPLAYER_LIGHT_SYNC_HANDOFF.md` for the multiplayer architecture and test gates.

## Brightness continuation

Player-controlled cosmetic brightness is no longer merely deferred: it is implemented on `dev/colour-system` as a **dev8 live-test candidate**.

The next brightness step is live V3.1 validation, especially:

- friendly brightness radial text;
- `Normal -> Bright -> Very Bright -> Maximum -> Dim -> Normal` cycling;
- immediate live intensity changes;
- colour/brightness preservation when changing either setting;
- save/quit/restart persistence;
- repeated powered-light off/on without intensity compounding or drift;
- relative intensity behaviour on multiple tiers;
- no regression to crop growth, coverage, artificial sunlight, tier rules or power draw;
- compatibility with dev7 `meta2` values `1..6`.

Use `docs/BRIGHTNESS_DEV8_HANDOFF.md` and `TESTING.md` as the active brightness continuation documents.
