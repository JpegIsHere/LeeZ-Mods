using System;
using System.Collections.Generic;
using System.Globalization;

namespace LeezGrowLights
{
    internal static class GrowLightScanner
    {
        private const int DefaultRadius = 2;
        private const int DefaultMinVerticalOffsetFromFarm = 1;
        private const int DefaultMaxVerticalOffsetFromFarm = 10;

        private static readonly Dictionary<Vector3i, bool> LastLampStates =
            new Dictionary<Vector3i, bool>();

        /// <summary>
        /// Server-authoritative growth-speed lookup. The caller supplies the plant block position.
        /// </summary>
        public static float GetBestActiveMultiplier(WorldBase world, Vector3i plantPos)
        {
            if (world == null || world.IsRemote()) return 1f;
            Vector3i farmPos = plantPos + Vector3i.down;
            return ScanFarmFootprint(world, farmPos, true);
        }

        /// <summary>
        /// Same server-authoritative lookup without the per-crop boost diagnostic.
        /// Used by electrical transition handling where many nearby plants can be sampled.
        /// </summary>
        internal static float GetBestActiveMultiplierQuiet(WorldBase world, Vector3i plantPos)
        {
            if (world == null || world.IsRemote()) return 1f;
            Vector3i farmPos = plantPos + Vector3i.down;
            return ScanFarmFootprint(world, farmPos, false);
        }

        internal static bool TryGetGrowLightCoverage(
            Block block,
            out int radius,
            out int minVerticalOffset,
            out int maxVerticalOffset)
        {
            radius = DefaultRadius;
            minVerticalOffset = DefaultMinVerticalOffsetFromFarm;
            maxVerticalOffset = DefaultMaxVerticalOffsetFromFarm;

            if (!TryGetProperty(block, "LeezGrowTier", out _))
                return false;

            radius = Math.Max(0, GetIntProperty(block, "LeezGrowRadius", DefaultRadius));
            minVerticalOffset = Math.Max(
                1,
                GetIntProperty(
                    block,
                    "LeezGrowMinFarmBlockVerticalOffset",
                    DefaultMinVerticalOffsetFromFarm));
            maxVerticalOffset = Math.Max(
                minVerticalOffset,
                GetIntProperty(
                    block,
                    "LeezGrowMaxFarmBlockVerticalOffset",
                    DefaultMaxVerticalOffsetFromFarm));

            return true;
        }

        /// <summary>
        /// True when a powered/switched LeeZ grow light can substitute for sunlight at this
        /// position. Placement APIs have varied between game versions, so the supplied position
        /// is tested both as a plant position and as a farm-block position.
        ///
        /// Unlike growth-speed calculation this intentionally runs on both client and server so
        /// the local placement preview and the authoritative world check agree.
        /// </summary>
        public static bool HasActiveSunlightReplacement(WorldBase world, Vector3i plantOrFarmPos)
        {
            if (world == null) return false;

            // Normal interpretation: caller supplied the plant position.
            if (ScanFarmFootprint(world, plantOrFarmPos + Vector3i.down, false) > 1.0001f)
                return true;

            // Defensive V3 placement interpretation: caller supplied the supporting farm block.
            return ScanFarmFootprint(world, plantOrFarmPos, false) > 1.0001f;
        }

        private static float ScanFarmFootprint(WorldBase world, Vector3i farmPos, bool logBoost)
        {
            float best = 1f;

            // Valid grow-light height: 1 through 10 blocks above the supporting farm plot.
            // At 11+ blocks above the plot, the lamp is outside the effective range.
            for (int verticalOffset = DefaultMinVerticalOffsetFromFarm;
                 verticalOffset <= DefaultMaxVerticalOffsetFromFarm;
                 verticalOffset++)
            {
                int lightY = farmPos.y + verticalOffset;

                for (int dx = -DefaultRadius; dx <= DefaultRadius; dx++)
                {
                    for (int dz = -DefaultRadius; dz <= DefaultRadius; dz++)
                    {
                        Vector3i lightPos = new Vector3i(farmPos.x + dx, lightY, farmPos.z + dz);
                        BlockValue lightValue = world.GetBlock(lightPos);
                        Block lightBlock = lightValue.Block;
                        if (lightBlock == null) continue;

                        if (!TryGetProperty(lightBlock, "LeezGrowTier", out string tierText))
                            continue;

                        if (!TryGetGrowLightCoverage(
                                lightBlock,
                                out int radius,
                                out int minVerticalOffset,
                                out int maxVerticalOffset))
                            continue;

                        if (Math.Abs(dx) > radius || Math.Abs(dz) > radius)
                            continue;

                        int actualVerticalOffset = lightPos.y - farmPos.y;
                        if (actualVerticalOffset < minVerticalOffset ||
                            actualVerticalOffset > maxVerticalOffset)
                            continue;

                        float multiplier = GetFloatProperty(lightBlock, "LeezGrowMultiplier", 1f);
                        bool active = PowerStateResolver.IsActive(world, lightPos);

                        if (!world.IsRemote())
                            LogLampStateChange(lightPos, tierText, multiplier, active);

                        if (!active)
                            continue;

                        if (multiplier > best)
                            best = multiplier;
                    }
                }
            }

            if (logBoost && best > 1.0001f)
            {
                LeezLog.Info(
                    "Grow boost detected for farm " + farmPos +
                    ": " + best.ToString("0.###", CultureInfo.InvariantCulture) + "x");
            }

            return best;
        }

        private static void LogLampStateChange(
            Vector3i lightPos,
            string tierText,
            float multiplier,
            bool active)
        {
            lock (LastLampStates)
            {
                if (LastLampStates.TryGetValue(lightPos, out bool previous) && previous == active)
                    return;

                LastLampStates[lightPos] = active;
            }

            LeezLog.Info(
                "Grow lamp T" + tierText + " at " + lightPos +
                " state=" + (active ? "ACTIVE" : "INACTIVE") +
                ", multiplier=" +
                multiplier.ToString("0.###", CultureInfo.InvariantCulture) + "x");
        }

        private static bool TryGetProperty(Block block, string name, out string value)
        {
            value = null;
            return block?.Properties?.Values != null &&
                   block.Properties.Values.TryGetValue(name, out value);
        }

        private static int GetIntProperty(Block block, string name, int fallback)
        {
            return TryGetProperty(block, name, out string value) &&
                   int.TryParse(
                       value,
                       NumberStyles.Integer,
                       CultureInfo.InvariantCulture,
                       out int parsed)
                ? parsed
                : fallback;
        }

        private static float GetFloatProperty(Block block, string name, float fallback)
        {
            return TryGetProperty(block, name, out string value) &&
                   float.TryParse(
                       value,
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       out float parsed)
                ? parsed
                : fallback;
        }
    }
}
