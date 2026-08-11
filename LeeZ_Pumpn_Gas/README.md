# LeeZ Pumpn Gas

Early design/research project for **7 Days to Die V3.1.0 b14**.

## Core idea

Make fuel a scarce, location-based resource rather than an easily crafted/harvested commodity.

Players should need to carry tiered portable fuel cans with limited capacity when refuelling vehicles, generators, and fuel-powered tools. Fuel primarily comes from world fuel repositories such as service-station pumps and appropriate tank-style props. Those repositories have finite stock and periodically restock on a cadence tied to the game's supply-drop cycle.

## Initial design direction

- Portable fuel is stored in **tiered gas cans** rather than unlimited loose stacks.
- Initial reference capacity: roughly **500 fuel units** for a basic/representative can; final tier capacities still need balancing.
- Vehicles/tools must draw fuel from fuel carried in an appropriate can/container.
- Remove ordinary fuel recovery/harvest paths from unrelated items.
- Remove/disable gasoline crafting from **oil shale**.
- Gas-station pumps become finite fuel sources.
- Initial reference pump stock: roughly **200 fuel units per working repository/tank**; final values still need balancing.
- Other suitable world tank/industrial fuel props may become collection points.
- Fuel repositories restock on the same general cadence as the supply-drop airplane/event, subject to confirming the exact V3.1 timing and hooks.
- Goal: make fuel a valuable strategic commodity and add route planning, storage, and resupply gameplay.

## First research pass required

Before implementation, inventory the V3.1 game files and record:

1. **Fuel item data**
   - gasoline/fuel item IDs
   - stack sizes
   - tags/classes
   - vehicle/tool fuel-use references
   - trader/loot references
   - harvest/scrap outputs that create fuel

2. **Fuel recipes and progression**
   - oil-shale-to-gas recipes
   - chemistry/workstation requirements
   - perks/books/unlocks affecting gasoline
   - any alternate recipes or conversion paths

3. **World fuel sources**
   - gas pumps
   - service-station tanks
   - barrels/drums
   - fuel storage tanks
   - industrial tanks/props
   - generators or other blocks that visually/functionally represent stored fuel
   - identify which are blocks, loot containers, tile entities, decorations, or prefab-only props

4. **Loot/restock systems**
   - existing loot groups containing gasoline
   - container respawn/restock rules
   - supply-drop scheduling/cadence
   - multiplayer/server-authoritative timing hooks suitable for synchronised fuel repository restocking

5. **Vehicle/tool refuelling code paths**
   - how vehicles currently consume/refill fuel
   - how generators consume/refill fuel
   - any fuel-powered tools/items that require integration
   - determine whether XML alone is enough or Harmony/runtime code is required for gas-can-only refuelling

6. **Images/assets associated with fuel**
   - gasoline inventory icon(s)
   - gas-can/jerry-can models and icons if present
   - gas-pump models/textures/icons
   - fuel barrel/drum assets
   - tank/storage props
   - relevant UI sprites
   - record source asset paths and screenshots/reference exports without redistributing game-owned assets unnecessarily

## Likely mod components

- `Config/items.xml` — gas-can tiers, capacities, stack/container behavior.
- `Config/recipes.xml` — remove/disable oil-shale gasoline crafting and define can progression if craftable.
- `Config/loot.xml` — remove loose fuel from unwanted loot and configure approved fuel sources.
- `Config/blocks.xml` — identify/extend gas pumps and tank-style world repositories where XML is sufficient.
- `Config/progression.xml` — tier unlocks if cans are progression-gated.
- `Localization.txt` — names/descriptions for cans and fuel interactions.
- Runtime/Harmony DLL — likely required for finite per-repository fuel state, synchronized restocking, and enforcing gas-can-only refuelling.
- Persistence/network state — repository fuel amounts and restock timestamps must survive save/reload and multiplayer synchronization.
- Optional custom UI interaction — e.g. `Fill Gas Can`, amount remaining, empty/replenished state.
- Test/debug tooling — commands/logging to inspect repository stock and force/observe restock cycles during development.

## Key design questions to resolve after research

- Exact number of gas-can tiers and capacities.
- Whether cans are reusable containers with stored quantity, item stacks representing filled amounts, or a hybrid.
- Whether fuel repositories are consumed per physical pump/tank or share stock across a POI.
- Whether destroyed pumps/tanks permanently lose their stored fuel.
- Whether player-placed fuel storage can exist and whether it can be refilled from cans.
- Exact restock rule: tied directly to supply-drop events, matched only by interval, or a configurable server timer.
- Whether traders can sell fuel/cans and at what rarity/pricing.
- How much fuel vehicles/tools consume relative to the new scarcity economy.

## First implementation milestone

Do **not** begin gameplay patches until the V3.1 fuel-data inventory is complete.

The first milestone should produce a documented table of every relevant fuel item, recipe, loot source, pump/tank block or prefab, associated icon/model path, and the exact V3.1 methods/events used for refuelling and supply-drop/restock timing.
