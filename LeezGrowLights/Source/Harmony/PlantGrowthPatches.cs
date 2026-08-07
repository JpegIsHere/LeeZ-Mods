using System;

namespace LeezGrowLights
{
    internal static class PlantGrowthPatches
    {
        /// <summary>
        /// Harmony-compatible prefix used on both the crop scheduling method and UpdateTick.
        /// The patch receives the arguments generically so it survives the V3 API removing
        /// or adding unrelated parameters such as a cluster index.
        /// </summary>
        public static void ContextPrefix(object[] __args, ref float __state)
        {
            __state = GrowthScheduleContext.CurrentMultiplier;

            if (!TryGetWorldAndPosition(__args, out WorldBase world, out Vector3i plantPos))
            {
                GrowthScheduleContext.CurrentMultiplier = 1f;
                return;
            }

            GrowthScheduleContext.CurrentMultiplier = GrowLightScanner.GetBestActiveMultiplier(world, plantPos);
        }

        public static void ContextPostfix(float __state)
        {
            GrowthScheduleContext.CurrentMultiplier = __state;
        }

        /// <summary>
        /// Converts the vanilla stage duration into a speed-multiplied duration.
        /// 63 minutes at 1.2x becomes 52.5 minutes; at 4x it becomes 15.75 minutes.
        /// </summary>
        public static void TickRatePostfix(ref ulong __result)
        {
            float multiplier = GrowthScheduleContext.CurrentMultiplier;
            if (multiplier <= 1.0001f) return;

            double adjusted = __result / (double)multiplier;
            __result = (ulong)Math.Max(1d, Math.Round(adjusted));
        }

        private static bool TryGetWorldAndPosition(object[] args, out WorldBase world, out Vector3i position)
        {
            world = null;
            position = Vector3i.zero;
            bool foundPosition = false;

            if (args == null) return false;

            foreach (object arg in args)
            {
                if (world == null && arg is WorldBase worldArg)
                    world = worldArg;

                if (!foundPosition && arg is Vector3i posArg)
                {
                    position = posArg;
                    foundPosition = true;
                }
            }

            return world != null && foundPosition;
        }
    }
}
