# V3.1 API validation — successful installed-game probe

Probe generated: 2026-08-07 (Australia/Melbourne)
Target assembly: `7DaysToDie_Data/Managed/Assembly-CSharp.dll`

## Confirmed for crop scheduling

`BlockPlantGrowing` exists and exposes:

- `GetTickRate()` returning `System.UInt64`;
- `addScheduledTick`;
- `UpdateTick`;
- `growthRate`, `growthDeviation`, `nextPlant`, `lightLevelGrow`, and related crop fields.

The PowerShell 5.1 reflection host cannot materialize parameter lists for several V3.1 methods and throws:

`Non-abstract, non-.cctor method in an interface.`

Therefore the runtime intentionally discovers `addScheduledTick` and `UpdateTick` by declared method name without requiring parameter enumeration during discovery.

## Confirmed for electrical state

`TileEntityPowered` exposes:

- `IsPowered : bool`;
- `GetPowerItem() : PowerItem`;
- `PowerUsed`;
- `RequiredPower`.

`TileEntityPoweredBlock` derives from `TileEntityPowered` and additionally exposes:

- `IsToggled : bool`.

`PowerConsumerToggle` exposes:

- `IsPowered : bool`;
- `IsToggled : bool`;
- a reference back to its `TileEntityPowered`.

`TileEntityElectricityLightBlock` was **not found** in this V3.1 assembly.

The runtime therefore uses `TileEntityPoweredBlock.IsPowered && IsToggled` as the primary path and a `PowerConsumerToggle` fallback.

## Still not validated by this probe

The report did not include the world-block-ticker type/API needed to query remaining scheduled time or safely invalidate/reschedule an already queued crop tick.

That means exact mid-stage power transitions are still not claimed complete. The current runtime correctly determines the multiplier whenever the crop is scheduled/updated, but a power change can still occur while an older scheduled update is waiting.

The next API target is the object returned by the world block ticker (`GetWBT` / equivalent), specifically:

- add scheduled block update;
- invalidate scheduled block update;
- query remaining or target tick, if exposed.

## Reference

The successful raw probe is stored as eight line-preserving parts under `docs/reference/`. See `docs/reference/README.md`; concatenating parts 01 through 08 in numeric order reconstructs the original report exactly.
