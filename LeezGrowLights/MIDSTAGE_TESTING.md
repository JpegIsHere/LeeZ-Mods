# v0.5.3-dev2 live mid-stage test

This build is the first implementation candidate for progress-preserving power changes.

## Primary test

1. Plant a crop under a powered T6.
2. Let it run part-way through the stage.
3. Switch the T6 OFF.
4. The log should show:
   `Mid-stage crop rescheduled ... 4x -> 1x`
5. Switch it ON again later.
6. The log should show:
   `Mid-stage crop rescheduled ... 1x -> 4x`

The reported remaining tick count should expand when switching OFF and shrink when
switching ON.

## Also test

- T1 -> T6 effective transition.
- T6 -> T1 effective transition.
- A lower-tier overlapping lamp toggled while T6 remains active: no reschedule should occur.
- Direct loss/restoration of electrical power.

Upstream relay-only propagation is not claimed by this dev build yet.

## Not claimed by this dev build yet

Full save/restart persistence validation will be handled after live transition behavior
is proven in V3.1.
