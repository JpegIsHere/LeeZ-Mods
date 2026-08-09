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

The validated test FBX is also currently local to the user's machine and is **not stored in this repository checkpoint**:

`LeeZGrowLight_T1_Greybox.fbx`

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

### Pivot validation in the working `.blend`

The root was temporarily rotated **20 degrees on X**.

Result:

- the complete grow-light fixture moved as one unit;
- `BlockReference` remained stationary;
- the fixture visibly hinged around the intended top-centre mounting point.

The test rotation was then cleared with `Alt+R`, returning the fixture to flat orientation, and the Blender file was saved.

This pivot test **passed**.

## Important Blender parenting lesson

Blender Outliner selection caused two temporary incorrect-parent attempts because the intended Empty was not the active object.

The reliable method that worked was:

1. Deselect everything.
2. In the Outliner, select the continuous range of grow-light mesh objects.
3. Use **Ctrl + Left Click** on `LeeZGrowLight_T1` so it becomes the active object while preserving the mesh selection.
4. Move the mouse over the 3D Viewport.
5. `Ctrl+P -> Object (Keep Transform)`.

Do not assume Shift-clicking the Empty makes it the active object in the required way.

## FBX greybox export / round-trip validation — completed

The first Blender export-format/hierarchy check has now been completed using FBX.

### Selection method that produced the valid export

1. In the Outliner, right-click `LeeZGrowLight_T1`.
2. Choose **Select Hierarchy**.
3. Verify `LeeZGrowLight_T1` and all 10 mesh children are selected while `BlockReference` is not selected.
4. Move the mouse over the 3D Viewport and press **Numpad .** to verify the selected fixture frames correctly in the viewport.

An earlier export produced a suspicious **4.04 KB** FBX that re-imported as an empty scene. After explicitly confirming the hierarchy selection in the 3D Viewport, the retry produced an approximately **30 KB** FBX and imported correctly. Treat the approximately 30 KB retry as the validated test export; do not use the 4.04 KB file.

### Validated FBX export settings

The successful retry used:

```text
Selected Objects:      checked
Object Types:          Empty + Mesh only
Scale:                 1.00
Apply Scalings:        All Local
Forward:               -Z Forward
Up:                    Y Up
Apply Unit:            checked
Use Space Transform:   checked
Apply Transform:       unchecked
Bake Animation:        unchecked
```

No materials, textures, UVs, animation, bevels or detail work were added.

### Clean Blender FBX re-import results

The approximately 30 KB retry was imported into a fresh empty Blender scene with the default FBX import settings.

Validation passed:

- `LeeZGrowLight_T1` imported successfully.
- All **10 intended mesh children** remained parented beneath the root Empty.
- Root Location remained **X 0, Y 0, Z 0.50**.
- Root Rotation remained **X 0, Y 0, Z 0**.
- Root Scale remained **X 1, Y 1, Z 1**.
- `Housing_Greybox` dimensions remained **X 0.80, Y 0.80, Z 0.06**.
- A fresh **20-degree X rotation** of the imported root hinged the whole fixture around the correct top-centre mounting pivot.
- `Alt+R` returned the imported test fixture flat.

Therefore the tested FBX round-trip preserves the hierarchy, key dimensions, root transforms and top-centre pivot behavior inside Blender.

After the test, the imported test scene was discarded and the original `LeeZGrowLight_T1_Greybox.blend` was reopened. The original working scene remains the source Blender file.

This Blender round-trip is **not yet the in-game scale/orientation test**. Unity/prefab and 7DTD testing are still required.

## Exact Unity engine version — resolved from V3.1.0 b14 `Player.log`

The user supplied the exact line from their installed game log:

```text
Initialize engine version: 2022.3.62f2 (7670c08855a9)
```

Therefore the Unity editor version for this V3.1.0 b14 asset/prefab work is now resolved as:

**Unity 2022.3.62f2**

Changeset/build identifier from the log:

**`7670c08855a9`**

This value was obtained directly from the user's own **7 Days to Die V3.1.0 b14 `Player.log`**, not inferred from older A21/V1 tutorials.

Do not substitute a different 2022.3 LTS patch unless later evidence from the installed game/toolchain explicitly requires it.

## Current milestone

The following are complete for the first T1 greybox pipeline test:

- T1 greybox geometry.
- Top-centre parent root/pivot.
- 20-degree pivot test in the working `.blend`.
- Root transform/hierarchy verification and save.
- Controlled FBX export preparation.
- Successful FBX export after verified hierarchy selection.
- Clean Blender FBX round-trip validation of hierarchy, dimensions, transforms and pivot.
- Exact Unity engine version identified from the user's V3.1.0 b14 `Player.log` as **2022.3.62f2**.

Do **not** add materials, textures, UVs, bevels, bolts, branding, vents, detailed diode meshes or emissive work yet.

The next milestone is to set up the matching Unity editor/project and perform the first **external** scale/orientation/prefab test before heavy detailing.

## Exact next step for the next session

Continue one step at a time. The user is a Blender/Unity beginner; do not jump ahead.

1. Install/open **Unity 2022.3.62f2** using Unity Hub.
2. Do **not** casually choose additional Unity modules, a project template, render pipeline, package set, asset-bundle tooling or 7DTD tag/layer setup. Verify what V3.1.0 b14 requires before making those decisions.
3. Once the matching editor is available, create the minimum matching test project needed for the T1 greybox pipeline.
4. Import the validated local `LeeZGrowLight_T1_Greybox.fbx` and verify scale, orientation, hierarchy and top-centre pivot in Unity before proceeding to prefab detail.
5. Keep this first pass T1-only.

The next chat should start by reading this file from `dev/colour-system` and following this exact next-step section.

## Future Unity/prefab constraints

The Unity prefab must eventually preserve the existing visual runtime requirements:

- normal renderable mesh/material hierarchy that can work with the current material-colour path;
- at least one child Unity `Light` component because the runtime uses `transform.GetComponentsInChildren<Light>(true)`;
- sensible root/pivot/origin for ceiling placement;
- collider and required 7DTD tag/layer arrangement verified against V3.1 rather than assumed from older tutorials;
- T1-only asset-bundle integration first;
- full T1 regression testing before adapting T2-T6.

## Integration order remains unchanged

1. Finish T1 greybox. **DONE for first scale/orientation test.**
2. Learn/check scale, transforms and origin/pivot. **DONE for Blender + FBX round-trip; Unity/in-game validation still pending.**
3. Test size/orientation before heavy detailing. **NEXT: Unity, then in game.**
4. UV unwrap and texture only after greybox success.
5. Determine exact Unity version from `Player.log`. **DONE: Unity 2022.3.62f2 (`7670c08855a9`).**
6. Set up matching Unity project. **NEXT.**
7. Build 7DTD prefab with root/collider/tag/layer, mesh and child Unity Light.
8. Export appropriate 7DTD asset bundle.
9. Add bundle to the mod `Resources` folder.
10. Point **only T1** at the custom model.
11. Thoroughly test T1 scale, pivot, collision, materials, wiring, power, colour, brightness, persistence and crop behavior.
12. Only after T1 works correctly, apply the model system to T2-T6.
