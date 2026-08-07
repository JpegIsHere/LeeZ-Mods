using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace LeezGrowLights
{
    internal static class PowerTransitionPatches
    {
        internal sealed class TransitionState
        {
            public WorldBase World;
            public Vector3i LampPos;
            public readonly List<PlantSnapshot> Plants = new List<PlantSnapshot>();
        }

        internal struct PlantSnapshot
        {
            public Vector3i Position;
            public int BlockId;
            public float OldMultiplier;
        }

        private static readonly FieldInfo PowerItemTileEntityField =
            AccessTools.Field(typeof(PowerItem), "TileEntity");

        public static void Prefix(object __instance, ref TransitionState __state)
        {
            __state = Capture(__instance);
        }

        public static void Postfix(TransitionState __state)
        {
            if (__state == null || __state.World == null)
                return;

            WorldBase world = __state.World;
            if (world.IsRemote())
                return;

            foreach (PlantSnapshot plant in __state.Plants)
            {
                BlockValue currentValue = world.GetBlock(plant.Position);
                if (!(currentValue.Block is BlockPlantGrowing))
                    continue;

                int currentBlockId = currentValue.type;
                if (currentBlockId != plant.BlockId)
                    continue;

                float newMultiplier =
                    GrowLightScanner.GetBestActiveMultiplierQuiet(world, plant.Position);

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

        private static TransitionState Capture(object instance)
        {
            if (!(instance is PowerConsumerToggle))
                return null;

            WorldBase world = ResolveServerWorld();
            if (world == null || world.IsRemote())
                return null;

            TileEntityPowered tileEntity = ResolveTileEntity(instance);
            if (tileEntity == null)
                return null;

            Vector3i lampPos = tileEntity.ToWorldPos();
            Block lampBlock = world.GetBlock(lampPos).Block;

            if (!GrowLightScanner.TryGetGrowLightCoverage(
                    lampBlock,
                    out int radius,
                    out int minVerticalOffset,
                    out int maxVerticalOffset))
            {
                return null;
            }

            var state = new TransitionState
            {
                World = world,
                LampPos = lampPos
            };

            // Only inspect positions this lamp can possibly affect. For the current XML
            // this is at most 5*5*10 = 250 candidate farm columns/levels.
            for (int verticalOffset = minVerticalOffset;
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

        private static TileEntityPowered ResolveTileEntity(object instance)
        {
            if (PowerItemTileEntityField == null)
                return null;

            try
            {
                return PowerItemTileEntityField.GetValue(instance) as TileEntityPowered;
            }
            catch
            {
                return null;
            }
        }

        private static WorldBase ResolveServerWorld()
        {
            try
            {
                if (GameManager.Instance == null)
                    return null;

                return GameManager.Instance.World;
            }
            catch
            {
                return null;
            }
        }
    }
}
