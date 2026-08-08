# Colour and brightness system development

Development branch: `dev/colour-system`  
Validated game build: `V 3.1.0 (b14)`  
Current runtime candidate: `v0.7.0-dev8`  
Known-good colour baseline: `c7f70f408ba72d8657422caf131a9d2404e5457a`

## Goal

Provide per-placed-light colour and cosmetic brightness selection without changing grow-light tier, electrical power draw, crop coverage, artificial-sunlight behaviour, or growth-speed behaviour.

Allowed colours:

- Blue
- Green
- Red
- Purple
- White
- Yellow

Default colour: **White**.

Brightness levels:

- Dim (`0.35x` vanilla light intensity)
- Normal (`1.00x`)
- Bright (`1.50x`)
- Very Bright (`2.00x`)
- Maximum (`3.00x`)

Default brightness: **Normal**.

Brightness cycle from the default state:

`Normal -> Bright -> Very Bright -> Maximum -> Dim -> Normal`

## Current status

### Live-validated dev7 baseline

The colour system is working in live single-player testing on V3.1.0 b14:

- LeeZ grow lights expose a colour radial-menu command.
- The colour name cycles through all six configured colours.
- The selected colour persists on the placed block.
- Save/quit/restart restores the selected colour.
- The visible lamp colour changes immediately during live play.
- Existing grow-light electrical and crop-growth behaviour remains separate from colour selection.

### dev8 candidate

The branch now extends the dev7 path with cosmetic brightness control and stable localization tokens.

Implemented but still awaiting live in-game validation:

- A second LeeZ-only radial command for brightness.
- Five persisted brightness levels.
- Live brightness changes through child Unity `Light.intensity` components.
- Colour changes preserve brightness; brightness changes preserve colour.
- Existing dev7 `meta2` values `1..6` retain their original colours at Normal brightness.
- Stable `growlightcolour_<colour>` and `growlightbrightness_<level>` activation/localization tokens replace dynamic display text while legacy dev7 colour text remains accepted during activation.

Do not treat dev8 brightness or the localization-token change as live-validated until the V3.1 test checklist passes.

## Runtime files

### `Runtime/GrowLightColourPalette.cs`

Defines the canonical colour enum and the brightness enum/palette.

Colour cycle:

`Blue -> Green -> Red -> Purple -> White -> Yellow -> Blue`

Brightness intensity multipliers are relative to the vanilla/current powered-light intensity, not absolute light values. This preserves tier/prefab differences and keeps brightness cosmetic.

### `Runtime/GrowLightColourState.cs`

Stores colour + brightness together in `BlockValue.meta2` while preserving the existing powered tile entity.

Storage contract:

- `0` = legacy/uninitialised -> White + Normal
- `1..6` = Blue/Green/Red/Purple/White/Yellow at Normal brightness (the exact dev7 colour values)
- `7..12` = the same six colours at Dim
- `13..18` = the same six colours at Bright
- `19..24` = the same six colours at Very Bright
- `25..30` = the same six colours at Maximum

Electrical switch state remains in `TileEntityPoweredBlock.isToggled`.

V3.1 persistence continues through the already-working normal block-change/RPC path using `BlockChangeInfo` with its `BlockValueRef`, `bChangeBlockValue`, and `blockValue` data.

### `Runtime/GrowLightColourVisual.cs`

Applies the selected colour to:

- `BlockEntityData.SetMaterialColor(colour)`
- child Unity `Light.color` values

It also applies the selected brightness multiplier to child Unity `Light.intensity` values.

The dev7 block-entity cache remains the live-refresh path. dev8 additionally tracks a weak per-`Light` intensity baseline. When vanilla changes a light intensity during power/toggle updates, the visual layer treats that post-vanilla value as the new baseline before applying the cosmetic multiplier. This avoids repeatedly multiplying an already-multiplied value and avoids replacing vanilla powered-light state.

### `Harmony/GrowLightColourInstaller.cs`

Patches the relevant `BlockPoweredLight`/base activation hierarchy plus the visual build/update callbacks. No extra Harmony surface was needed for brightness because the existing dev7 visual callbacks already reach the child Unity lights.

### `Harmony/GrowLightColourPatches.cs`

Adds the LeeZ-only colour and brightness commands and handles activation.

Important V3.1 detail: `BlockPoweredLight.OnBlockActivated` identifies the selected radial action by `_commandName:String`, not by a numeric command index.

dev8 uses stable command tokens:

- `growlightcolour_blue`, `growlightcolour_green`, etc.
- `growlightbrightness_dim`, `growlightbrightness_normal`, etc.

`Config/Localization.csv` supplies the corresponding `blockcommand_<token>` labels. Legacy `Grow light colour:` command text remains recognized for dev7 compatibility. Vanilla `light` commands pass through untouched.

After a successful local/server state write, the patch immediately calls the same cached live visual refresh used by dev7.

## Validation history

- dev1: command missing from radial menu.
- dev2: command appeared; activation did not change state.
- dev3: diagnostics proved the numeric-index assumption was wrong.
- dev4: command-name activation worked; persistence failed on an invalid `BlockChangeInfo` constructor assumption.
- dev5: diagnostics exposed the actual `BlockChangeInfo` fields and `BlockValueRef` requirement.
- dev6: persistence worked; colour names cycled and save/restart restored the selected visual colour, but live visuals only updated after reload.
- dev7: cached live `BlockEntityData` and applied the new colour immediately. Live colour cycling passed.
- dev8 candidate: stable localization tokens plus persisted cosmetic brightness using the dev7 persistence/live-refresh path. Live validation pending.

See `docs/COLOUR_DEV7_HANDOFF.md` for the known-good colour handoff and `docs/BRIGHTNESS_DEV8_HANDOFF.md` for the dev8 candidate/test gate.

## Multiplayer gate

Remote-client colour and brightness authoring are not complete. The current handler deliberately rejects a remote world until proper server command routing is implemented.

Do not claim multiplayer visual-state synchronization is finished.

## dev8 live test gate

Before promoting dev8, verify at minimum:

1. Both colour and brightness radial commands appear with friendly localized text.
2. Colour still cycles through all six states and updates immediately.
3. Brightness cycles `Normal -> Bright -> Very Bright -> Maximum -> Dim -> Normal` and updates immediately.
4. Changing colour does not reset brightness, and changing brightness does not reset colour.
5. Save/quit/restart restores both colour and brightness.
6. Turning the powered light off/on preserves the chosen brightness and does not create compounding intensity changes.
7. Crop growth multiplier, coverage, artificial sunlight, tier behaviour and electrical power draw remain unchanged.
8. Existing dev7 saves with `meta2` values `1..6` load with the same colour at Normal brightness.
