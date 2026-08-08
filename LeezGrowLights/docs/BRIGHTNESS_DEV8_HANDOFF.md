# Brightness dev8 handoff

Date: 2026-08-08  
Game target: 7 Days to Die V3.1.0 (b14)  
Branch: `dev/colour-system`  
Known-good colour baseline: `c7f70f408ba72d8657422caf131a9d2404e5457a`  
Runtime banner: `v0.7.0-dev8`  
`ModInfo.xml` version: `0.7.0.0`

## Status

Brightness is implemented as a dev8 **live-test candidate**. It has not yet been validated in-game.

The dev7 colour behaviour remains the known-good baseline and must not be considered superseded until dev8 passes the checklist below.

The branch also contains a concurrent dev8 localization improvement for colour commands. Brightness was merged onto that newer stable-token approach rather than restoring the old dynamic command text.

## What dev8 adds

- A second LeeZ-only radial-menu command for brightness.
- Five cosmetic brightness levels: Dim, Normal, Bright, Very Bright, Maximum.
- Per-placed-light brightness persistence in `BlockValue.meta2` alongside colour.
- Immediate live brightness refresh using the existing dev7 cached `BlockEntityData` path.
- Stable localization/activation tokens for both colour and brightness commands.
- Compatibility with existing dev7 colour metadata values.

Brightness does **not** deliberately change crop growth multipliers, grow-light coverage, artificial sunlight, tier rules, or electrical power draw.

## Brightness cycle and multipliers

Cycle from the default state:

`Normal -> Bright -> Very Bright -> Maximum -> Dim -> Normal`

Unity `Light.intensity` multipliers:

- Dim: `0.35x`
- Normal: `1.00x`
- Bright: `1.50x`
- Very Bright: `2.00x`
- Maximum: `3.00x`

The multiplier is relative to the current vanilla/prefab light intensity rather than an absolute intensity value.

## Persistence encoding

`BlockValue.meta2` now encodes colour + brightness together.

Compatibility rule:

- `0` = legacy/uninitialised -> White + Normal
- `1..6` = the existing dev7 colours at Normal brightness

Extended states:

- `7..12` = six colours at Dim
- `13..18` = six colours at Bright
- `19..24` = six colours at Very Bright
- `25..30` = six colours at Maximum

Within each six-value range the colour order remains:

1. Blue
2. Green
3. Red
4. Purple
5. White
6. Yellow

Changing colour preserves the current brightness bucket. Changing brightness preserves the current colour.

The existing V3.1 persistence path is reused: `BlockChangeInfo` + `BlockValueRef` + the normal block RPC route.

## Runtime implementation

### `Source/Runtime/GrowLightColourPalette.cs`

Adds `GrowLightBrightness` and `GrowLightBrightnessPalette` with the cycle, display name mapping, and intensity multipliers.

Brightness palette commit:

`f246e5c1c76e53303fc05060186ae833fb406ed3`

### `Source/Runtime/GrowLightColourState.cs`

Extends `meta2` encoding to 30 colour/brightness combinations while preserving dev7 values `1..6` as Normal brightness.

Adds:

- `GetBrightness`
- `WithBrightness`
- `TrySetBrightness`

Colour and brightness share the already-proven V3.1 block persistence path.

State commit:

`6db30a8488bad3a2f035c445e8b33e57117628ba`

### `Source/Runtime/GrowLightColourVisual.cs`

The existing dev7 cache still stores live `BlockEntityData` by block position.

Brightness applies to child Unity `Light.intensity` values. A weak per-`Light` state tracks:

- the current vanilla/base intensity,
- the last intensity applied by this mod,
- whether a modded value has already been applied.

If vanilla changes the intensity before a patched visual callback (for example during a power/toggle update), dev8 treats that new value as the fresh baseline before applying the cosmetic multiplier. This avoids compounding multipliers and avoids replacing vanilla powered-light state.

Visual commit:

`428e9eadc11565bdd15acd304e52599e3e6868fb`

### `Source/Harmony/GrowLightColourPatches.cs`

Adds a second command using stable tokens:

- colour: `growlightcolour_<colour>`
- brightness: `growlightbrightness_<level>`

The handler accepts the new tokens and keeps legacy `Grow light colour:` / `Grow light brightness:` display-text prefixes as compatibility fallbacks.

On a successful brightness write it calls `GrowLightColourVisual.TryApplyCached(...)` exactly as colour does.

Merged interaction commit:

`4d62bc1615936f527c485447069cb140c20f5b03`

### `Config/Localization.csv`

Adds friendly labels for:

- `blockcommand_growlightbrightness_dim`
- `blockcommand_growlightbrightness_normal`
- `blockcommand_growlightbrightness_bright`
- `blockcommand_growlightbrightness_verybright`
- `blockcommand_growlightbrightness_maximum`

Brightness localization commit:

`54c7feb46758e0691f5d924a571a81dd8899c061`

## Static checks completed

The colour/brightness encoding was checked across all `6 x 5 = 30` combinations and round-trips correctly.

The source path was reviewed to ensure:

- dev7 values `1..6` still decode to the same colours at Normal brightness;
- colour writes preserve brightness;
- brightness writes preserve colour;
- remote-client authoring is still rejected;
- brightness only touches Unity light intensity in the visual layer;
- the visual multiplier uses a tracked baseline rather than multiplying the last modded intensity repeatedly.

No live V3.1 build/game validation was performed in this chat environment.

## Required live test gate

1. Confirm the startup banner reports `v0.7.0-dev8` with no Harmony exceptions.
2. Open a LeeZ grow light radial menu and confirm both commands are present.
3. Confirm colour text is friendly/localized, not a raw `blockcommand_...` key.
4. Confirm brightness text is friendly/localized.
5. Starting from a legacy/dev7 light, confirm brightness initially shows Normal.
6. Cycle brightness through `Normal -> Bright -> Very Bright -> Maximum -> Dim -> Normal`.
7. Confirm each step changes the visible light intensity immediately without a reload.
8. Select a non-White colour, then change brightness several times; confirm colour is preserved.
9. Select a non-Normal brightness, then cycle colour; confirm brightness is preserved.
10. Save, quit, restart, and confirm both colour and brightness restore.
11. Toggle the powered light off/on several times and confirm the chosen brightness returns without getting progressively brighter/dimmer.
12. Repeat on at least two grow-light tiers to confirm the brightness multiplier remains relative to each light's vanilla/base intensity.
13. Confirm crop growth speed, coverage, artificial-sunlight behaviour, tier rules, and power draw are unchanged.
14. Confirm a dev7 save containing each existing `meta2` value `1..6` still restores the same colour at Normal brightness.

## Multiplayer gate

Remote-client colour and brightness authoring remain deliberately blocked until proper server command routing is implemented.

Do not claim multiplayer visual-state synchronization is complete.

## If dev8 fails

Use `c7f70f408ba72d8657422caf131a9d2404e5457a` as the known-good colour runtime baseline. The six documentation commits after that baseline do not change the validated dev7 runtime behaviour.

For a brightness-specific regression, inspect the candidate commits listed above before changing the dev7 persistence/live-refresh path.
