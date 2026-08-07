# v0.5.3-dev3 live mid-stage test

The direct power/toggle rescheduler has now passed its first V3.1 live tests.

## Confirmed live behavior

Observed on **7 Days to Die V3.1.0 (b14)**:

- T6 `1x -> 4x`: remaining scheduled time reduced to approximately one quarter.
  - example: `105436 -> 26359` ticks
- T6 `4x -> 1x`: remaining scheduled time expanded by exactly four times.
  - example: `26055 -> 104220` ticks
- T6 OFF period followed by `1x -> 4x`:
  - example: `103945 -> 25986` ticks
- T4 `1x -> 1.5x`:
  - example: `24870 -> 16580` ticks
- T4 `1.5x -> 1x`:
  - example: `16774 -> 25161` ticks

These results match the intended remaining-work conversion:

```text
remaining vanilla work = remaining queued ticks * old multiplier
new remaining ticks    = remaining vanilla work / new multiplier
```

This validates that progress already earned before a direct lamp power/toggle transition is preserved and only the remaining work changes speed.

## dev3 startup check

v0.5.3-dev2 installed three transition hooks successfully but produced a non-fatal Harmony warning for inherited `HandleDisconnect` discovery.

v0.5.3-dev3 fixes that by patching the declaring implementation directly:

`PowerItem.HandleDisconnect()`

Expected dev3 startup result: four electrical transition hooks installed with no LeeZ Harmony warning.

## Remaining mid-stage tests

- T1 -> T6 effective transition.
- T6 -> T1 effective transition.
- Lower-tier lamp toggled while an active T6 still wins: no reschedule should occur.
- Direct source power loss/restoration, not just the lamp's own toggle.
- Lamp removal midway through a stage.
- Save/reload during a partially accelerated stage.
- Chunk unload/reload.
- Dedicated server / remote client authority.
- Upstream relay-only propagation.
