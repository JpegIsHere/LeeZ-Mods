# Development decisions

This file records the gameplay and technical decisions that should not be accidentally lost during later refactors.

## Gameplay rules

- Six tiers of grow lights.
- Horizontal crop coverage is exactly 5x5 (radius 2 from the supporting farm plot).
- Valid vertical placement is 1-10 blocks above the farm plot.
- 11+ blocks above is out of range.
- Grow lights must be both powered and switched on.
- An active grow light substitutes for sunlight for planting, staying alive and growth checks.
- Artificial sunlight is conditional; vanilla crop requirements remain unchanged outside active coverage.
- Overlapping lights do not stack. Highest active multiplier wins.
- Current tier multipliers: 1.2x, 1.3x, 1.4x, 1.5x, 1.6x, 4.0x.
- Current Wiring 101 unlock levels: 10, 20, 30, 45, 60, 75.
- Current power draw is provisional 10 W per tier.

## Data ownership

Balance and geometry values belong in XML wherever practical. C# should read tier/multiplier/radius/vertical metadata rather than duplicate balance constants unnecessarily.

## Server authority

Growth-speed selection must be server-authoritative. Placement needs enough client-side awareness to avoid rejecting an enclosed-room seed locally, but gameplay state should not be trusted to the client.

## Electrical API

V3.1 validation established these useful paths:

- `TileEntityPowered.IsPowered`
- `TileEntityPoweredBlock.IsToggled`
- `TileEntityPowered.GetPowerItem()`
- `PowerConsumerToggle.IsPowered`
- `PowerConsumerToggle.IsToggled`

The primary active-state path is `TileEntityPoweredBlock.IsPowered && IsToggled`, with a typed `PowerConsumerToggle` fallback. Unknown electrical shapes fail safe as OFF.

## Crop timing

Do not repeatedly force `UpdateTick` merely to simulate faster growth. The mod currently adjusts the vanilla tick rate during scheduling/update context so vanilla plant checks stay in control.

Exact mid-stage power changes are deliberately unfinished until the block-ticker/rescheduling API is validated. Do not treat 'lamp state at tick completion' as though it represented the entire preceding growth stage.

## Colour design

The intended selectable colours are exactly:

- Blue
- Green
- Red
- Purple
- White
- Yellow

Colour persistence/network synchronization are future work.
