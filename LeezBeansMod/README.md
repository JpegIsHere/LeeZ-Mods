# Leez Beans Mod

A new **7 Days to Die** mod project for bean plants.

## Current asset

`Source/Models/leez_bean_plant.obj` is a lightweight, original bean-plant source mesh created for this project.

- Approximate height: 0.95 m
- Approximate width: 0.95 m
- 508 vertices
- 744 faces
- Y-up, origin at soil level
- Separate material groups for stem, leaves and pods
- ASCII Wavefront OBJ so the source remains easy to inspect and version in Git

The OBJ is a **source asset**, not a finished 7 Days to Die runtime asset. Import it into Blender/Unity, refine textures/UVs as desired, then package it using the asset-bundle workflow for the game version targeted by the mod.

## Reference models found during research

### Best visual reference: Bean Sprouting Scan 01 (Low Poly)

- Creator: Marcos Silva (`marcosramone25`)
- Species/reference: Kraido Green Dwarf French Bean
- Geometry: about 14.9k triangles / 7.6k vertices
- License: CC BY 4.0
- Source: https://sketchfab.com/3d-models/bean-sprouting-scan-01-low-poly-83d907b3dbd848af8fb20620464a6ba3

This is a strong candidate if a scanned, realistic young bean plant is preferred. It is **not bundled here**; download it from the creator/source and retain the required attribution.

### Open-source Blender reference: PlantDreamer bean model

PlantDreamer supports bean plants and publishes a `bean.blend` source model and bean L-system generation code.

- Project: https://github.com/Lewis-Stuart-11/PlantDreamer
- Bean Blender source: `l_systems/blender_models/bean.blend`
- Project license: Apache-2.0

This is also **not bundled here**. The project source is useful as a higher-detail reference or starting point for future asset refinement.

## Suggested next build steps

1. Open `Source/Models/leez_bean_plant.obj` in Blender.
2. Add UVs and final leaf/stem/pod textures.
3. Make a lower-cost collision mesh if the plant needs collision.
4. Export to the format required by the Unity/7DTD asset-bundle build.
5. Add the runtime bundle under this mod's `Resources/` folder.
6. Add the 7DTD block/crop XML once the final bundle and prefab path are known.
7. Test placement, harvesting, growth stages, LOD/culling and multiplayer loading.

## Status

**Scaffold / source-model stage.** Runtime crop/block XML and a final asset bundle are intentionally not included yet.
