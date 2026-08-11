# Leez Beans Mod

**Development version: 0.2.0.0**

A 7 Days to Die bean crop mod built on the vanilla **Super Corn / Grace Corn** farming behavior so we have a known gameplay baseline and can add/remove attributes without redesigning the crop system.

## Current status

The **gameplay XML is implemented** and can be tested now with inherited vanilla visuals. The custom bean source model and Unity AssetBundle build pipeline are also in the repo; the final `.unity3d` runtime bundle still needs to be built with a Unity/7DTD-compatible editor setup before the reviewed bean visuals are enabled in game.

## Gameplay baseline

Leez Beans currently mirror Super Corn-style mechanics:

- Farm Plot planting behavior inherited from Super Corn.
- V3 `PlantGrowing` crop system.
- 63.0-minute configured growing-stage rate.
- Super Corn-derived raw-food, stack and economic behavior.
- Mature player harvest: 2 base beans using `cropHarvest`.
- 50% bonus-bean roll using `bonusCropHarvest`.
- 50% seed-return roll from the mature player plant.
- 5 harvested beans craft 1 seed.
- Seed crafting is locked until the Leez Beans seed-recipe schematic is read.
- Reading the schematic sets the recipe unlock CVar and grants the normal 50 schematic XP.
- The schematic is inserted beside Super Corn in rare book loot.
- The Super Corn special footlocker also provides 5 bean seeds + the bean seed recipe, giving a survival-world bootstrap path.
- The Cop Zombie Slayer reward route also includes the bean schematic beside the Super Corn recipe route.

All values are deliberately treated as **development defaults**. See `TUNING.md` for the list of attributes we can change safely later.

## Runtime IDs

Keep these stable unless a save-breaking migration is intentional:

- `foodCropLeezBeans`
- `plantedLeezBeans1`
- `plantedLeezBeans2`
- `plantedLeezBeans3Harvest`
- `plantedLeezBeans3HarvestPlayer`
- `plantedLeezBeans1Schematic`

## Project layout

```text
LeezBeansMod/
├── ModInfo.xml
├── README.md
├── TESTING.md
├── TUNING.md
├── ATTRIBUTION.md
├── Config/
│   ├── blocks.xml
│   ├── items.xml
│   ├── recipes.xml
│   ├── loot.xml
│   ├── quests.xml
│   └── Localization.csv
├── Resources/
│   └── README.md
└── Source/
    ├── Models/
    │   ├── leez_bean_plant.obj
    │   ├── leez_bean_plant.mtl
    │   └── README.md
    └── Unity/
        ├── README.md
        └── Assets/Editor/BuildLeezBeansBundle.cs
```

## Install for XML/gameplay testing

Copy the complete `LeezBeansMod` folder into the game's `Mods` directory and launch the game. Check the console/log for XML errors before testing in a save.

For the first test, leave the fallback visuals in place. Use Creative Mode to inspect the bean seed, bean crop item and schematic, then follow `TESTING.md`.

## Custom bean model

`Source/Models/leez_bean_plant.obj` is the editable source mesh created for this project.

- Approximate height: 0.94 m
- Approximate width: 0.81 m
- 130 vertices
- 140 faces
- Y-up, origin at soil level
- separate Stem, Leaf and Pod materials

The approved review render is the **visual target**: healthy broad leaves, natural green stems and obvious hanging bean pods.

### Unity handoff

`Source/Unity/Assets/Editor/BuildLeezBeansBundle.cs` provides a repeatable editor pipeline that:

1. imports the tracked OBJ/MTL;
2. prepares sprout, growing and mature prefab roots;
3. applies the 7DTD `T_Mesh_B` plant interaction tag;
4. adds simple capsule hit colliders;
5. builds `Resources/leezbeans.unity3d` for the three expected prefab names.

See `Source/Unity/README.md` for the exact prefab names and `#@modfolder` model references.

Until that bundle is built and tested, `Config/blocks.xml` intentionally inherits Super Corn visuals so a missing binary cannot break the XML implementation.

## Multiplayer

The XML portion of a modlet can be server-provided, but custom item icons and Unity AssetBundles are client-side assets. Once the custom bean bundle is enabled, every multiplayer client needs the complete Leez Beans Mod installed locally.

## Reference assets researched

### Bean Sprouting Scan 01 (Low Poly)

- Creator: Marcos Silva (`marcosramone25`)
- Kraido Green Dwarf French Bean reference
- approximately 14.9k triangles / 7.6k vertices
- CC BY 4.0
- https://sketchfab.com/3d-models/bean-sprouting-scan-01-low-poly-83d907b3dbd848af8fb20620464a6ba3

Not bundled; retained as a realistic visual reference.

### PlantDreamer bean model

- https://github.com/Lewis-Stuart-11/PlantDreamer
- `l_systems/blender_models/bean.blend`
- Apache-2.0 project license

Not bundled; useful as a higher-detail/open-source Blender reference.

## Development rule

Start from the Super Corn baseline, change one attribute at a time, and preserve the internal IDs. That lets us evolve nutrition, harvest yield, recipes, trader/loot access, growth conditions and visuals without continually rebuilding the underlying crop loop.
