# LeeZ Grow Light model progress

Checkpoint date: **2026-08-09**

This file is the continuation checkpoint for the custom 3D model/prefab work for **LeezGrowLights**.

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

The concept is approved and currently requires no design changes.

### Overall fixture

- Single-block, ceiling-mounted **square grow light**.
- Inspired by a clean commercial/hydroponic grow-light fixture rather than a salvaged or sci-fi design.
- Housing colour/style: **dark gunmetal**, clean industrial/hydroponic.
- Flush or near-flush ceiling mount.
- Deliberate **top-center origin/pivot** for ceiling placement.
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

Higher tiers should therefore be immediately readable from below by the number of square LED zones.

### LED appearance

- OFF: frosted white / pale diffuser appearance.
- ON: selected LeeZ colour with a **dotted illuminated LED appearance** if practical.
- Do **not** model hundreds of individual LED cubes for the finished asset unless testing later proves necessary.
- Preferred later approach: simple LED ring geometry plus material/texture detail that represents individual diode dots.
- Do not solve emissive shaders/material masks during the greybox phase.

### Detail policy

Bolts, screws, decals, LeeZ branding, vents and other small details are optional later. They are intentionally excluded from the first greybox.

## Approved target proportions

These are block-relative target dimensions for the first in-game size test, not yet a claim about final metre scale in Unity/7DTD.

- Reference 7DTD block: **1.00 x 1.00 x 1.00** conceptual units.
- Grow-light overall footprint: **0.80 x 0.80 block**.
- Main housing thickness: **0.06 block**.
- Total final thickness including shallow raised/back components: approximately **0.10-0.12 block**.
- Available LED region: approximately **0.68 x 0.68 block**.
- Proposed outer T1 LED square: approximately **0.64 x 0.64 block**.
- Proposed LED strip width: approximately **0.055 block**.
- Proposed visible housing border: approximately **0.05-0.06 block**.
- Proposed LED recess: approximately **0.01-0.015 block**.

The greybox must be tested in game before spending time on fine detail.

## Tier brightness decision

Tier should govern **base visual brightness**, while the existing player brightness selector remains functional.

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

This is compatible with the existing `GrowLightColourVisual` approach, which treats Unity Light intensity as a baseline and layers the selected brightness multiplier over it.

## Planned object structure

The simple conceptual Blender structure is:

```text
LeeZGrowLight_T1
  |- Housing_Frame / main housing
  |- Backplate
  |- Centre_Panel
  `- LED_Square_1
```

Later tiers can reuse the common housing and add `LED_Square_2` through `LED_Square_6`.

The large commercial-style driver box should **not** sit underneath in the centre because it would conflict with the concentric T5/T6 LED layout. Any driver/electronics detail should instead be shallow and integrated into the top/back housing later.

## Blender learning/build approach

The user is a Blender beginner. Continue with explicit, one-step-at-a-time instructions and do not assume knowledge of modes, menus, transforms, dimensions, origins, pivots, UVs, materials, export settings or Unity concepts.

### Current Blender file

Suggested working filename used during the session:

`LeeZGrowLight_T1_Greybox.blend`

### Completed Blender setup

A conceptual 7DTD reference block has been created:

`BlockReference`

Verified values:

```text
Location:   X 0, Y 0, Z 0
Rotation:   X 0, Y 0, Z 0
Scale:      X 1, Y 1, Z 1
Dimensions: X 1, Y 1, Z 1
```

The scale was explicitly applied with `Ctrl+A -> Scale` after sizing the Blender default cube.

### Completed first grow-light greybox object

Object:

`Housing_Greybox`

Verified from the user's Blender screenshot:

```text
Location:   X 0, Y 0, Z 0.47
Rotation:   X 0, Y 0, Z 0
Scale:      X 1, Y 1, Z 1
Dimensions: X 0.80, Y 0.80, Z 0.06
```

This positions the housing slab at the top of the 1x1 reference block:

- top of conceptual block = Z 0.50
- slab thickness = 0.06
- half slab thickness = 0.03
- slab centre = 0.50 - 0.03 = **Z 0.47**

The first scale/orientation check therefore passed visually.

## Exact next Blender step

Do **not** jump ahead to detailed modeling, LEDs, UVs or textures.

The next action planned was:

1. Select `BlockReference` in the Outliner.
2. Open **Object Properties**.
3. Expand **Viewport Display**.
4. Change **Display As** to **Wire** so only the reference cube is wireframe while `Housing_Greybox` remains solid.
5. If necessary, enable **In Front** for the reference object so its outline remains visible.
6. After that, begin constructing the underside/recessed frame from separate simple pieces with exact dimensions, rather than immediately using booleans or complicated mesh edits.

The current `Housing_Greybox` can become the main body/backplate as the greybox develops.

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

1. Finish T1 greybox.
2. Learn/check scale, transforms and origin/pivot.
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
