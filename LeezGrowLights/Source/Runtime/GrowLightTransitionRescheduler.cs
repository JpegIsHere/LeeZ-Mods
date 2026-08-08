using System;
using System.Collections.Generic;

namespace LeezGrowLights
{
    /// <summary>
    /// Captures the crops that a particular grow light can affect before a lamp-state
    /// transition, then re-scans after the transition and reschedules only plants whose
    /// effective highest multiplier actually changed.
    ///
    /// This shared path is used by electrical changes and physical lamp removal.
    /// </summary>
    internal static class GrowLightTransitionRescheduler
    {
        internal sealed class TransitionState
        {
            public WorldBase World;
            public Vector3i LampPos;
            public bool ExcludeLampOnApply;
            public readonly List<PlantSnapshot> Plants = new List<PlantSnapshot>();
        }

        internal struct PlantSnapshot
        {
            public Vector3i Position;
            public int BlockId;
            public float OldMultiplier;
        }

        internal static TransitionState Capture(
            WorldBase world,
            Vector3i lampPos,
            Block lampBlock,
            bool excludeLampOnApply = false)
        {
            if (world == null || world.IsRemote() || lampBlock == null)
                return null;

            if (!GrowLightScanner.TryGetGrowLightCoverage(
                    lampBlock,
                    out int radius,
                    out _,
                    out int maxVerticalOffset))
            {
                return null;
            }

            var state = new TransitionState
            {
                World = world,
                LampPos = lampPos,
                ExcludeLampOnApply = excludeLampOnApply
            };

            // Conservative candidate capture: downward lights use Y+2..Y+10, while a horizontal
            // panel can illuminate crops whose farm block is one block below the lamp. Capturing
            // from offset 1 is safe because Apply still reschedules only when the effective
            // multiplier actually changes.
            for (int verticalOffset = 1;
                 verticalOffset <= maxVerticalOffset;
                 verticalOffset++)
            {
                int farmY = lampPos.y - verticalOffset;

                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        Vector3i farmPos =
                            new Vector3i(lampPos.x + dx, farmY, lampPos.z + dz);
                        Vector3i plantPos = farmPos + Vector3i.up;

                        BlockValue plantValue = world.GetBlock(plantPos);
                        if (!(plantValue.Block is BlockPlantGrowing))
                            continue;

                        state.Plants.Add(new PlantSnapshot
                        {
                            Position = plantPos,
                            BlockId = plantValue.type,
                            OldMultiplier =
                                GrowLightScanner.GetBestActiveMultiplierQuiet(world, plantPos)
                        });
                    }
                }
            }

            return state.Plants.Count == 0 ? null : state;
        }

        internal static void Apply(TransitionState state)
        {
            if (state == null || state.World == null || state.World.IsRemote())
                return;

            WorldBase world = state.World;

            foreach (PlantSnapshot plant in state.Plants)
            {
                BlockValue currentValue = world.GetBlock(plant.Position);
                if (!(currentValue.Block is BlockPlantGrowing))
                    continue;

                int currentBlockId = currentValue.type;
                if (currentBlockId != plant.BlockId)
                    continue;

                float newMultiplier = state.ExcludeLampOnApply
                    ? GrowLightScanner.GetBestActiveMultiplierQuietExcluding(
                        world,
                        plant.Position,
                        state.LampPos)
                    : GrowLightScanner.GetBestActiveMultiplierQuiet(
                        world,
                        plant.Position);

                if (Math.Abs(newMultiplier - plant.OldMultiplier) <= 0.0001f)
                    continue;

                TickerScheduleAccessor.RescheduleRemainingWork(
                    world,
                    plant.Position,
                    plant.BlockId,
                    plant.OldMultiplier,
                    newMultiplier);
            }
        }
    }
}
