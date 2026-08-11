# Leez Beans - tuning map

The initial rule is simple: **start from Super Corn and change one attribute at a time**. Stable gameplay IDs should remain unchanged so planted crops and learned recipes survive future tuning releases.

## Current inherited baseline

| Area | Current source/baseline | Where to change later |
|---|---|---|
| Raw bean food/effects | `foodCropGraceCorn` | `Config/items.xml` |
| Stack/economic behavior | `foodCropGraceCorn` | `Config/items.xml` |
| Seed placement behavior | `plantedGraceCorn1` | `Config/blocks.xml` |
| Growing-stage rate | 63.0 minutes per configured growing stage | `Config/blocks.xml` |
| Fertility threshold | 15 while growing | `Config/blocks.xml` |
| Mature fertility behavior | inherited from Super Corn harvest block | `Config/blocks.xml` |
| Base mature harvest | 2 beans with `cropHarvest` tag | `Config/blocks.xml` |
| Bonus mature harvest | 1 bean at 50% base roll with `bonusCropHarvest` tag | `Config/blocks.xml` |
| Seed return | 1 seed at 50% on mature destroy/harvest | `Config/blocks.xml` |
| Seed crafting | 5 beans -> 1 seed | `Config/recipes.xml` |
| Recipe unlock | read bean seed schematic | `Config/items.xml` |
| Rare schematic loot | `veryLow`, alongside Super Corn schematic | `Config/loot.xml` |
| Special bootstrap cache | 5 seeds + schematic in Super Corn footlocker | `Config/loot.xml` |
| Challenge schematic route | alongside Super Corn schematic reward | `Config/quests.xml` |
| Visuals before bundle | inherited vanilla Super Corn models | `Config/blocks.xml` |
| Final custom visuals | `leezbeans.unity3d` prefabs | `Resources/` + `Config/blocks.xml` |

## Good future attributes to tune

- bean nutrition / health / recipes
- sell price and trader availability
- stack size
- seed crafting cost
- seed-return chance
- base and perk-scaled harvest quantities
- growth speed
- light and fertility requirements
- whether immature plants refund a seed
- dedicated POI/world-spawn bean plants
- challenge and loot availability
- crop-specific recipes (stew, canned beans, chili, etc.)
- plant collision size
- stage scale and silhouette
- leaf and pod materials
- LOD/shadow settings

## Do not casually rename

Keep these IDs stable unless a save-breaking migration is intentional:

- `foodCropLeezBeans`
- `plantedLeezBeans1`
- `plantedLeezBeans2`
- `plantedLeezBeans3Harvest`
- `plantedLeezBeans3HarvestPlayer`
- `plantedLeezBeans1Schematic`

Changing display names is safe through `Config/Localization.csv`; changing these internal IDs can affect existing inventories, learned recipes, planted blocks, and saves.
