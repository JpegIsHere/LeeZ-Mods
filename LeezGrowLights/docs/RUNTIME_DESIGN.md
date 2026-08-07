# LeezGrowLights runtime design — V0.5.1

Target: **7 Days to Die V3.1.x**

## Authoritative growth rule

For a growing plant at `plantPos`:

1. supporting farm block = `plantPos + Vector3i.down`;
2. qualifying lamp centre Y = any level from `farmY + 1` through `farmY + 10`;
3. qualifying horizontal footprint = `dx/dz -2..+2` (5x5);
4. candidate block must carry `LeezGrowTier` / `LeezGrowMultiplier`;
5. lamp must be powered and switched on;
6. overlapping lamps do not stack: highest active multiplier wins;
7. otherwise use vanilla `1.0x`.

The runtime multiplier calculation is server-side.

## Speed math

`adjusted stage ticks = vanilla stage ticks / active speed multiplier`

For a 63-minute vanilla stage:

- T1 1.2x = 52.5 min
- T2 1.3x = ~48.46 min
- T3 1.4x = 45 min
- T4 1.5x = 42 min
- T5 1.6x = 39.375 min
- T6 4.0x = 15.75 min

## V0.5.1 V3.1-validated scheduling hook

The successful installed-game probe confirms `BlockPlantGrowing.GetTickRate()` returns `UInt64` and that `addScheduledTick` / `UpdateTick` exist.

Because PowerShell 5.1 cannot enumerate the parameter metadata for several V3.1 methods, `PatchInstaller` no longer calls `GetParameters()` during method discovery. It resolves the crop methods by declared name and uses Harmony's generic `object[] __args` to locate the `WorldBase` and `Vector3i` arguments at runtime.

`GrowthScheduleContext` is thread-static, so the multiplier applies only while the current crop scheduling/update call is executing. `GetTickRate` is then divided by that multiplier.

## V0.5.1 V3.1 electrical path

The probe confirms:

- `TileEntityPowered.IsPowered`;
- `TileEntityPoweredBlock.IsToggled`;
- `TileEntityPowered.GetPowerItem()`;
- `PowerConsumerToggle.IsPowered`;
- `PowerConsumerToggle.IsToggled`.

`PowerStateResolver` now uses those concrete game types directly. Generic reflection guessing has been removed.

Primary path:

`TileEntityPoweredBlock.IsPowered && TileEntityPoweredBlock.IsToggled`

Fallback:

`TileEntityPowered.IsPowered && PowerConsumerToggle.IsPowered && PowerConsumerToggle.IsToggled`

Unknown shapes fail safe as OFF and log once.

## Mid-stage transitions — still pending ticker validation

V0.5.1 does not claim exact preservation when a light turns on/off while a crop already has an older block update queued.

To solve this correctly we still need the V3.1 world-block-ticker API for safe invalidation/rescheduling or a persisted per-plant progress ledger.

Until that is implemented:

- newly scheduled growth uses the correct current multiplier;
- an already queued tick may reflect the power state that existed when it was queued.

## Colour system

Allowed choices remain:

- Blue
- Green
- Red
- Purple
- White
- Yellow

Colour persistence, multiplayer synchronization, and runtime light-component tinting remain a later stage.


## V0.5.1 artificial sunlight

An active LeeZ grow light is now intended to replace direct sunlight for covered vanilla crops. The runtime patches the crop placement/stay/alive/update validation path and temporarily sets `lightLevelGrow` and `lightLevelStay` to zero for the duration of the vanilla call. Original values are restored in both postfix and Harmony finalizer paths.

The substitution is conditional on the same electrical and geometric rules as the speed boost. It is not a global crop-light bypass.
