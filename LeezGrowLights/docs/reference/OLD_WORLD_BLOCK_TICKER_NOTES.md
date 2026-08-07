# Historical WorldBlockTicker reference

This file records an older decompiled 7 Days to Die implementation used only as a design reference for the V3.1 mid-stage scheduling investigation.

Historically, `BlockPlantGrowing.addScheduledTick` called:

`world.GetWBT().AddScheduledBlockUpdate(clrIdx, blockPos, blockID, ticks)`

and `WorldBlockTickerEntry` stored:

- `worldPos`
- `blockID`
- `scheduledTime`
- `clrIdx`
- `tickEntryID`

Historical `WorldBlockTicker` internals included:

- `scheduledTicksSorted`
- `scheduledTicksDict`
- `chunkToScheduledTicks`
- `lockObject`
- `AddScheduledBlockUpdate(...)`

Do **not** assume this private layout is unchanged in V3.1.0. The development runtime probe on `dev/midstage-reschedule-probe` exists specifically to validate the live installed-game API before implementing cancellation/rescheduling.
