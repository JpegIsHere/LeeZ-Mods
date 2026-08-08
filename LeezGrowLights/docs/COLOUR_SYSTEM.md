# Colour system development

Development branch: `dev/colour-system`  
Validated game build: `V 3.1.0 (b14)`  
Current runtime candidate: `v0.7.0-dev7`  
Known-good colour baseline: `c7f70f408ba72d8657422caf131a9d2404e5457a`

## Goal

Provide per-placed-light colour selection without changing grow-light tier, power draw, coverage, artificial-sunlight behaviour, or growth-speed behaviour.

Allowed colours:

- Blue
- Green
- Red
- Purple
- White
- Yellow

Default colour: **White**.

## Current status

The colour system is working in live single-player testing.

Validated on V3.1.0 b14:

- LeeZ grow lights expose a colour radial-menu command.
- The colour name cycles through all six configured colours.
- The selected colour persists on the placed block.
- Save/quit/restart restores the selected colour.
- The visible lamp colour changes immediately during live play in dev7.
- Existing grow-light electrical and crop-growth behaviour remains separate from colour selection.

The only currently observed UI defect is cosmetic: the radial menu may display a raw-looking label such as `blockcommand_growlightcolour: Blue` instead of a fully friendly localized string.

## Runtime files

### `Runtime/GrowLightColourPalette.cs`

Defines the canonical colour enum, cycle order, White default and Unity colour mappings.

Cycle order:

`Blue -> Green -> Red -> Purple -> White -> Yellow -> Blue`

### `Runtime/GrowLightColourState.cs`

Stores colour in `BlockValue.meta2` while preserving the existing powered tile entity.

Storage contract:

- `0` = legacy/uninitialised -> White
- `1..6` = six colour enum values + 1

Electrical switch state remains in `TileEntityPoweredBlock.isToggled`.

V3.1 persistence is performed through the normal block-change/RPC path using `BlockChangeInfo` with its `BlockValueRef`, `bChangeBlockValue`, and `blockValue` data.

### `Runtime/GrowLightColourVisual.cs`

Applies the selected colour to:

- `BlockEntityData.SetMaterialColor(colour)`
- child Unity `Light.color` values

Dev7 also caches each live lamp `BlockEntityData` by block position using weak references. After a successful colour write, the existing live block entity is repainted immediately instead of waiting for a chunk/world reload.

### `Harmony/GrowLightColourInstaller.cs`

Patches the relevant `BlockPoweredLight`/base activation hierarchy plus the visual build/update callbacks.

### `Harmony/GrowLightColourPatches.cs`

Adds the LeeZ-only colour command and handles activation.

Important V3.1 detail: `BlockPoweredLight.OnBlockActivated` identifies the selected radial action by `_commandName:String`, not by a numeric command index. The working handler therefore recognizes command names beginning with `Grow light colour:`. Vanilla `light` commands pass through untouched.

After a successful local/server state write, the patch immediately calls the dev7 cached visual refresh.

## Validation history

- dev1: command missing from radial menu.
- dev2: command appeared; activation did not change state.
- dev3: diagnostics proved the numeric-index assumption was wrong.
- dev4: command-name activation worked; persistence failed on an invalid `BlockChangeInfo` constructor assumption.
- dev5: diagnostics exposed the actual `BlockChangeInfo` fields and `BlockValueRef` requirement.
- dev6: persistence worked; colour names cycled and save/restart restored the selected visual colour, but live visuals only updated after reload.
- dev7: cached live `BlockEntityData` and applied the new colour immediately. Live colour cycling passed.

See `docs/COLOUR_DEV7_HANDOFF.md` for the detailed commit/test handoff and the brightness follow-up starting point.

## Multiplayer gate

Remote-client colour authoring is not complete. The current handler deliberately rejects a remote world until proper server command routing is implemented.

Do not claim multiplayer colour synchronization is finished.

## Known cosmetic issue

`Config/Localization.csv` contains:

`blockcommand_growlightcolour = Grow light colour`

In current testing the dynamic activation command can still render in key-style form such as `blockcommand_growlightcolour: Blue`. This does not affect state, persistence, or visual tinting.

## Next optional feature

Player-controlled brightness is a suitable extension because the dev7 visual path already reaches child Unity `Light` components. The intended follow-up is cosmetic brightness control only, independent of crop mechanics and electrical power draw. Details are recorded in `docs/COLOUR_DEV7_HANDOFF.md`.
