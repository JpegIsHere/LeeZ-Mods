# LeeZ Grow Light model progress

Latest checkpoint date: **2026-08-10**

This file is the continuation checkpoint for the custom 3D model/prefab work for **LeezGrowLights**.

Previous model-design checkpoint: **`b6f230633000815d01f7d3b06d66eea4a2c16f3b`**.

## Baseline that must not regress

- Game: **7 Days to Die V3.1.0 (b14)**
- Mod baseline: **LeezGrowLights v0.7.0.2 / dev12**
- Working branch: **`dev/colour-system`**
- Validated dev12 source commit: **`1449cba3cb663ab0ae3ea78458e6e02e54b69f13`**
- Preserve vanilla-style electrical wiring and ON/OFF switching.
- Preserve **10 W when ON / 0 W when the grow light itself is OFF**.
- Preserve colour selection and persistence.
- Preserve brightness selection and persistence.
- Preserve radial-menu icons.
- Preserve corrected next-brightness hover labels.
- Preserve underground sunlight substitution.
- Preserve crop-growth multipliers.
- Preserve **5x5** horizontal coverage.
- Preserve **1..10 block** vertical coverage.

The model phase should initially change only the visual asset path plus any model-specific offset/collider setup required for the custom prefab.

## Locked visual design

The concept remains approved and requires no design changes at this stage.

### Overall fixture

- Single-block, ceiling-mounted **square grow light**.
- Clean commercial/hydroponic grow-light fixture rather than salvaged or sci-fi styling.
- Housing colour/style: **dark gunmetal**, clean industrial/hydroponic.
- Flush or near-flush ceiling mount.
- Deliberate **top-centre origin/pivot** for ceiling placement.
- No chains or hanging cables in the first version.
- Same overall housing shared by T1-T6.

### Tier visual language

The underside uses **concentric square LED zones**.

- T1 = 1 square LED zone/ring.
- T2 = 2 concentric square LED zones.
- T3 = 3.
- T4 = 4.
- T5 = 5.
- T6 = 6 square zones, with the centre element also **square**, not circular.

Higher tiers should be immediately readable from below by the number of square LED zones.

### LED appearance

- OFF: frosted white / pale diffuser appearance.
- ON: selected LeeZ colour with a **dotted illuminated LED appearance** if practical.
- Do **not** model hundreds of individual LED cubes for the finished asset unless testing later proves necessary.
- Preferred later approach: simple LED ring geometry plus material/texture detail representing individual diode dots.
- Do not solve emissive shaders/material masks during the greybox phase.

### Detail policy

Bolts, screws, decals, LeeZ branding, vents and other small details remain optional later and are intentionally excluded from the first greybox.

## Approved target proportions

These are block-relative target dimensions for the first in-game size test, not yet a claim about final metre scale in Unity/7DTD.

- Reference 7DTD block: **1.00 x 1.00 x 1.00** conceptual units.
- Grow-light overall footprint: **0.80 x 0.80 block**.
- Main housing thickness: **0.06 block**.
- Total final thickness including shallow raised/back components: approximately **0.10-0.12 block**.
- Available LED region: approximately **0.68 x 0.68 block**.
- Outer T1 LED square: approximately **0.64 x 0.64 block**.
- LED strip width: approximately **0.055 block**.
- Visible housing border: approximately **0.05-0.06 block**.
- LED recess: approximately **0.01-0.015 block**.

The greybox must be tested in game before spending time on fine detail.

## Tier brightness decision

Tier governs **base visual brightness**, while the existing player brightness selector remains functional.

Conceptually:

```text
final visual brightness = tier base brightness x selected brightness multiplier
```

Therefore:

- T1 should have the lowest base visual intensity.
- T6 should have the highest base visual intensity.
- Existing Dim / Normal / Bright / Very Bright / Maximum selection must remain intact for every tier.
- Do not yet choose final numeric Unity Light intensities. Tune those after the custom prefab is working in Unity and in game.
- Tier brightness is visual; do not couple it to crop-growth calculations.

This remains compatible with the existing `GrowLightColourVisual` approach, which treats Unity Light intensity as a baseline and layers the selected brightness multiplier over it.

## Blender learning/build approach

The user is a Blender beginner. Continue with explicit, one-step-at-a-time instructions and do not assume knowledge of modes, menus, transforms, dimensions, origins, pivots, UVs, materials, export settings or Unity concepts.

Working filename used during the session:

`LeeZGrowLight_T1_Greybox.blend`

The `.blend` file itself is currently local to the user's machine and is **not stored in this repository checkpoint**.

## Current Blender scene — verified state

### Reference object

`BlockReference`

```text
Location:   X 0, Y 0, Z 0
Rotation:   X 0, Y 0, Z 0
Scale:      X 1, Y 1, Z 1
Dimensions: X 1, Y 1, Z 1
```

- Scale was applied with `Ctrl+A -> Scale`.
- Object Properties -> Viewport Display -> **Display As = Wire**.
- 3D Viewport itself is in normal **Solid** shading.
- The reference remains outside the grow-light parent hierarchy.

### Main housing

`Housing_Greybox`

```text
Location:   X 0, Y 0, Z 0.47
Rotation:   X 0, Y 0, Z 0
Scale:      X 1, Y 1, Z 1
Dimensions: X 0.80, Y 0.80, Z 0.06
```

This places the housing slab flush against the top of the conceptual 1x1 block.

### Underside frame

All frame pieces have applied scale and use separate simple cube geometry.

`Frame_Front`

```text
Location:   X 0, Y -0.37, Z 0.43
Dimensions: X 0.80, Y 0.06, Z 0.02
Scale:      X 1, Y 1, Z 1
```

`Frame_Back`

```text
Location:   X 0, Y 0.37, Z 0.43
Dimensions: X 0.80, Y 0.06, Z 0.02
Scale:      X 1, Y 1, Z 1
```

`Frame_Left`

```text
Location:   X -0.37, Y 0, Z 0.43
Dimensions: X 0.06, Y 0.68, Z 0.02
Scale:      X 1, Y 1, Z 1
```

`Frame_Right`

```text
Location:   X 0.37, Y 0, Z 0.43
Dimensions: X 0.06, Y 0.68, Z 0.02
Scale:      X 1, Y 1, Z 1
```

The four pieces form a complete rectangular underside frame.

### Recessed centre panel

`Centre_Panel`

```text
Location:   X 0, Y 0, Z 0.435
Dimensions: X 0.68, Y 0.68, Z 0.01
Scale:      X 1, Y 1, Z 1
```

The panel is recessed slightly above the lower edge of the surrounding frame.

### T1 LED square greybox

The T1 LED zone is represented by four simple strips. All have applied scale.

`LED1_Front`

```text
Location:   X 0, Y -0.2925, Z 0.4275
Dimensions: X 0.64, Y 0.055, Z 0.005
Scale:      X 1, Y 1, Z 1
```

`LED1_Back`

```text
Location:   X 0, Y 0.2925, Z 0.4275
Dimensions: X 0.64, Y 0.055, Z 0.005
Scale:      X 1, Y 1, Z 1
```

`LED1_Left`

```text
Location:   X -0.2925, Y 0, Z 0.4275
Dimensions: X 0.055, Y 0.53, Z 0.005
Scale:      X 1, Y 1, Z 1
```

`LED1_Right`

```text
Location:   X 0.2925, Y 0, Z 0.4275
Dimensions: X 0.055, Y 0.53, Z 0.005
Scale:      X 1, Y 1, Z 1
```

A visual underside check passed: the ring is centred, square, evenly inset and sits correctly on the recessed centre panel.

## Root/pivot hierarchy — completed and tested

An Empty of type **Plain Axes** was created and named:

`LeeZGrowLight_T1`

Verified root location:

```text
Location: X 0, Y 0, Z 0.50
```

This is the deliberate **top-centre mounting pivot** at the upper surface of the fixture.

The following mesh objects are parented to `LeeZGrowLight_T1` using **Object (Keep Transform)**:

```text
Centre_Panel
Frame_Back
Frame_Front
Frame_Left
Frame_Right
Housing_Greybox
LED1_Back
LED1_Front
LED1_Left
LED1_Right
```

`BlockReference` is **not** parented to the grow light.

### Pivot validation

The root was temporarily rotated **20 degrees on X**.

Result:

- the complete grow-light fixture moved as one unit;
- `BlockReference` remained stationary;
- the fixture visibly hinged around the intended top-centre mounting point.

The test rotation was then cleared with `Alt+R`, returning the fixture to flat orientation, and the Blender file was saved.

This pivot test **passed**.

## Important Blender parenting lesson from this session

Blender Outliner selection caused two temporary incorrect-parent attempts because the intended Empty was not the active object.

The reliable method that worked was:

1. Deselect everything.
2. In the Outliner, select the continuous range of grow-light mesh objects.
3. Use **Ctrl + Left Click** on `LeeZGrowLight_T1` so it becomes the active object while preserving the mesh selection.
4. Move the mouse over the 3D Viewport.
5. `Ctrl+P -> Object (Keep Transform)`.

Do not assume Shift-clicking the Empty makes it the active object in the required way.

## Current milestone

The **T1 greybox geometry and top-centre parent pivot are complete enough for the first external scale/orientation test**.

Do **not** add materials, textures, UVs, bevels, bolts, branding, vents, detailed diode meshes or emissive work yet.

## Exact next Blender step

Continue one step at a time.

1. Select the root Empty `LeeZGrowLight_T1`.
2. Verify its current transforms after the successful pivot test and reset:
   - Location should still be **X 0, Y 0, Z 0.50**.
   - Rotation should be **X 0, Y 0, Z 0**.
   - Scale should be **X 1, Y 1, Z 1**.
3. Verify the fixture remains flat and all intended mesh pieces remain parented to the Empty.
4. Save.
5. Only then prepare the first greybox export/scale-orientation test.

Do not make a final export-format or transform decision casually: check how the chosen Blender export will preserve the top-centre pivot and object hierarchy before changing the scene.

## Unity version is still intentionally unresolved

Do **not** recommend or install a Unity editor version yet.

The exact Unity engine version for the user's **7 Days to Die V3.1.0 b14** installation must first be read from the user's `Player.log`, specifically the line containing:

`Initialize engine version:`

Do not infer the editor version from A21/V1 tutorials.

## Future Unity/prefab constraints

Once the Blender greybox has passed a scale/orientation test, the Unity prefab must eventually preserve the existing visual runtime requirements:

- normal renderable mesh/material hierarchy that can work with the current material-colour path;
- at least one child Unity `Light` component because the runtime uses `transform.GetComponentsInChildren<Light>(true)`;
- sensible root/pivot/origin for ceiling placement;
- collider and required 7DTD tag/layer arrangement verified against V3.1 rather than assumed from older tutorials;
- T1-only asset-bundle integration first;
- full T1 regression testing before adapting T2-T6.

## Integration order remains unchanged

1. Finish T1 greybox. **DONE for first scale/orientation test.**
2. Learn/check scale, transforms and origin/pivot. **Pivot test passed; final export transform check is next.**
3. Test size/orientation before heavy detailing.
4. UV unwrap and texture only after greybox success.
5. Determine exact Unity version from `Player.log`.
6. Set up matching Unity project.
7. Build 7DTD prefab with root/collider/tag/layer, mesh and child Unity Light.
8. Export appropriate 7DTD asset bundle.
9. Add bundle to the mod `Resources` folder.
10. Point **only T1** at the custom model.
11. Thoroughly test T1 scale, pivot, collision, materials, wiring, power, colour, brightness, persistence and crop behavior.
12. Only after T1 works correctly, apply the model system to T2-T6.
