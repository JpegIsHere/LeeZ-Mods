# Leez Beans Mod - test checklist

Target: current 7 Days to Die V3 XML/modlet structure. Test again after every game update that changes farming, blocks, loot, quests, or Unity.

## XML-only smoke test

- [ ] Put `LeezBeansMod` in the game's `Mods` folder.
- [ ] Launch and confirm `LeezBeansMod` loads with no red XML errors.
- [ ] Enable Creative Mode and confirm these IDs resolve:
  - `plantedLeezBeans1`
  - `plantedLeezBeans2`
  - `plantedLeezBeans3Harvest`
  - `plantedLeezBeans3HarvestPlayer`
  - `foodCropLeezBeans`
  - `plantedLeezBeans1Schematic`
- [ ] Confirm localized names appear rather than raw IDs.

## Unlock and crafting parity

- [ ] Before reading the schematic, confirm the Leez Bean Seed recipe is locked.
- [ ] Read `Leez Beans (Seed) Recipe`.
- [ ] Confirm reading it gives the normal schematic XP behavior.
- [ ] Confirm the seed recipe unlocks.
- [ ] Confirm exactly 5 `Leez Beans` craft 1 `Leez Beans (Seed)` in inventory/hand crafting.

## Farming parity

- [ ] Place the seed on a valid Farm Plot.
- [ ] Confirm the seed cannot be planted where Super Corn cannot be planted.
- [ ] Confirm growth follows the current Super Corn fertility/light rules.
- [ ] Confirm each configured growing stage uses a 63-minute `PlantGrowing.GrowthRate` baseline.
- [ ] Confirm the plant advances only through Leez Bean block IDs and never becomes vanilla Super Corn.
- [ ] Break an immature plant and verify the expected seed recovery inherited/configured for that stage.
- [ ] Harvest the mature player crop.
- [ ] Confirm base crop output uses `foodCropLeezBeans`.
- [ ] Confirm Living Off The Land / crop-harvest bonuses affect the crop via the vanilla `cropHarvest` and `bonusCropHarvest` tags.
- [ ] Confirm mature harvest has a 50% seed-return roll.

## Acquisition

- [ ] Confirm the rare-book pool can roll `plantedLeezBeans1Schematic`.
- [ ] Confirm the Super Corn special footlocker contains 5 Leez Bean Seeds and 1 Leez Bean Seed Recipe in addition to the vanilla Super Corn contents.
- [ ] Confirm the Cop Zombie Slayer challenge reward list includes the Leez Bean Seed Recipe beside the Super Corn recipe route.

## Food/economy parity

Because `foodCropLeezBeans` extends `foodCropGraceCorn`:

- [ ] Compare raw food/health values with Ear of Super Corn.
- [ ] Compare stack size with Ear of Super Corn.
- [ ] Compare buy/sell/economic behavior with Ear of Super Corn.
- [ ] Verify no unintended Super Corn localization/icon text leaks into the bean item.

## Custom model test

Do this only after `Resources/leezbeans.unity3d` is built and the custom model lines are enabled in `Config/blocks.xml`.

- [ ] Every client has the complete mod installed locally.
- [ ] Sprout prefab renders at soil level.
- [ ] Growing prefab renders at soil level.
- [ ] Mature prefab renders at soil level with visible pods.
- [ ] No pink/missing materials.
- [ ] No missing AssetBundle/prefab errors in the console.
- [ ] Melee hits register on the plant collider.
- [ ] Harvesting works while looking at leaves/stem from normal player angles.
- [ ] Plant does not block player movement unless intentionally configured later.
- [ ] LOD/culling and shadows are acceptable in a dense farm.
- [ ] Test dedicated server plus at least one remote client.

## Regression checks after tuning

Whenever we change an attribute, retest only the affected section plus startup/XML loading. Keep gameplay IDs stable so existing saves do not lose planted crops.
