# Leez Beans runtime configuration

This folder now contains the playable XML layer for **Leez Beans Mod**.

## Files

- `blocks.xml` - seed, growing stage, POI mature stage, player mature stage, growth links and harvest/seed drops.
- `items.xml` - harvested bean item and read-to-unlock seed recipe schematic.
- `recipes.xml` - 5 harvested beans -> 1 bean seed, matching the Super Corn seed-recipe baseline.
- `loot.xml` - rare schematic route plus the Super Corn special-footlocker bootstrap route.
- `quests.xml` - adds the bean seed recipe alongside the Super Corn schematic's Cop Zombie Slayer challenge reward route.
- `Localization.csv` - English display names/descriptions using the V3 localization format.

## Mechanical baseline

The first playable version intentionally uses vanilla Super Corn / Grace Corn as its parent behavior wherever practical:

- harvested `foodCropLeezBeans` extends `foodCropGraceCorn`;
- seed/growing/mature blocks extend the equivalent Grace Corn definitions;
- growing stages explicitly use `PlantGrowing.GrowthRate=63.0`, `FertileLevel=15`, `IsRandom=false`;
- mature player harvest uses the vanilla crop perk tags (`cropHarvest` and `bonusCropHarvest`);
- mature seed return is 50%;
- seed crafting costs 5 beans;
- recipe unlock is a consumable schematic that sets the recipe CVar and grants 50 XP.

The bean-specific `Next` links and drop definitions keep the custom crop inside Leez Bean IDs rather than turning back into vanilla Super Corn.

## Visual state

`blocks.xml` currently leaves the inherited Super Corn visuals active as a **safe fallback**. This means the gameplay/XML can be loaded and tested before the custom Unity binary exists.

The custom bean visual contract and build script live under `Source/Unity/`. After `Resources/leezbeans.unity3d` is built and tested, enable the documented `ModelEntity`/`#@modfolder` lines for each growth stage.

## Tuning

See `../TUNING.md`. Gameplay IDs are intentionally stable; future versions should change values rather than rename IDs whenever possible.
