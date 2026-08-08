using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace LeezGrowLights
{
    internal static class GrowLightScanner
    {
        private const int DefaultRadius = 2;
        private const int DefaultMinVerticalOffsetFromFarm = 2;
        private const int DefaultMaxVerticalOffsetFromFarm = 10;
        private const int DefaultHorizontalWidth = 2;
        private const int DefaultHorizontalDepth = 2;
        private const int DefaultHorizontalFarmBlockVerticalOffset = 1;
        private const int DefaultCandidateRadius = 2;

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

        /// <summary>
        /// Removal-transition lookup that behaves as though one specific lamp position is already
        /// gone. V3.1 can invoke OnBlockRemoved before the world block slot is cleared, so a normal
        /// post-callback scan can still see the lamp that is in the process of being removed.
        /// </summary>
        internal static float GetBestActiveMultiplierQuietExcluding(
            WorldBase world,
            Vector3i plantPos,
            Vector3i excludedLightPos)
        {
            if (world == null || world.IsRemote()) return 1f;
            Vector3i farmPos = plantPos + Vector3i.down;
            return ScanFarmFootprint(
                world,
                farmPos,
                false,
                true,
                excludedLightPos);
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
                2,
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
        /// Returns true when the specific placed grow light geometrically illuminates the supplied
        /// supporting farm block. Power state is intentionally not checked here so transition
        /// capture can use the same geometry before and after electrical changes.
        /// </summary>
        internal static bool IsFarmCoveredByLight(
            WorldBase world,
            Vector3i farmPos,
            Vector3i lightPos,
            BlockValue lightValue)
        {
            Block lightBlock = lightValue.Block;
            if (lightBlock == null)
                return false;

            if (!TryGetGrowLightCoverage(
                    lightBlock,
                    out int radius,
                    out int minVerticalOffset,
                    out int maxVerticalOffset))
            {
                return false;
            }

            if (!GrowLightOrientationResolver.TryGetEmissionDirection(
                    lightValue,
                    lightBlock,
                    out Vector3i emissionDirection))
            {
                return false;
            }

            int dx = farmPos.x - lightPos.x;
            int dz = farmPos.z - lightPos.z;
            int verticalOffset = lightPos.y - farmPos.y;

            // Down-facing panel: full 5x5 footprint, but only from Y+2 through Y+10.
            if (emissionDirection.y < 0)
            {
                return verticalOffset >= minVerticalOffset &&
                       verticalOffset <= maxVerticalOffset &&
                       Math.Abs(dx) <= radius &&
                       Math.Abs(dz) <= radius;
            }

            // Up-facing panel never illuminates crops below it.
            if (emissionDirection.y > 0)
                return false;

            // Horizontal panel: a directional 2x2 footprint in front of the emitting face.
            // The lamp is aligned with the plant block, one block above the supporting farm block.
            int horizontalFarmOffset = Math.Max(
                1,
                GetIntProperty(
                    lightBlock,
                    "LeezGrowHorizontalFarmBlockVerticalOffset",
                    DefaultHorizontalFarmBlockVerticalOffset));

            if (verticalOffset != horizontalFarmOffset)
                return false;

            int horizontalWidth = Math.Max(
                1,
                GetIntProperty(lightBlock, "LeezGrowHorizontalWidth", DefaultHorizontalWidth));
            int horizontalDepth = Math.Max(
                1,
                GetIntProperty(lightBlock, "LeezGrowHorizontalDepth", DefaultHorizontalDepth));

            int forward = dx * emissionDirection.x + dz * emissionDirection.z;
            if (forward < 1 || forward > horizontalDepth)
                return false;

            // Local right vector for a cardinal horizontal emission direction.
            int rightX = emissionDirection.z;
            int rightZ = -emissionDirection.x;
            int side = dx * rightX + dz * rightZ;

            // Even widths are anchored to the local-left/centre pair. For width=2 this is -1..0.
            int sideMin = -(horizontalWidth / 2);
            int sideMax = sideMin + horizontalWidth - 1;
            return side >= sideMin && side <= sideMax;
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
            return ScanFarmFootprint(
                world,
                farmPos,
                logBoost,
                false,
                default(Vector3i));
        }

        private static float ScanFarmFootprint(
            WorldBase world,
            Vector3i farmPos,
            bool logBoost,
            bool excludeLight,
            Vector3i excludedLightPos)
        {
            float best = 1f;

            // Candidate search is deliberately conservative. Down-facing lights can be Y+2..Y+10;
            // horizontal lights are aligned at farm Y+1. Exact acceptance is delegated to the
            // orientation-aware IsFarmCoveredByLight check below.
            for (int verticalOffset = 1;
                 verticalOffset <= DefaultMaxVerticalOffsetFromFarm;
                 verticalOffset++)
            {
                int lightY = farmPos.y + verticalOffset;

                for (int dx = -DefaultCandidateRadius; dx <= DefaultCandidateRadius; dx++)
                {
                    for (int dz = -DefaultCandidateRadius; dz <= DefaultCandidateRadius; dz++)
                    {
                        Vector3i lightPos = new Vector3i(farmPos.x + dx, lightY, farmPos.z + dz);

                        if (excludeLight && SamePosition(lightPos, excludedLightPos))
                            continue;

                        BlockValue lightValue = world.GetBlock(lightPos);
                        Block lightBlock = lightValue.Block;
                        if (lightBlock == null) continue;

                        if (!TryGetProperty(lightBlock, "LeezGrowTier", out string tierText))
                            continue;

                        if (!IsFarmCoveredByLight(world, farmPos, lightPos, lightValue))
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

        private static bool SamePosition(Vector3i left, Vector3i right)
        {
            return left.x == right.x && left.y == right.y && left.z == right.z;
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

        /// <summary>
        /// Resolves the placed block's world-space emitting direction without taking a compile-time
        /// dependency on an unverified V3.1 rotation helper. It first asks the live game types for a
        /// Quaternion rotation through reflection, then falls back to the basic 0..3 ceiling-light
        /// rotations as downward-facing only. Unknown advanced rotations fail closed (no benefit).
        /// </summary>
        private static class GrowLightOrientationResolver
        {
            private const BindingFlags AnyMember =
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            private static bool reportedAccessor;
            private static bool reportedFallback;
            private static bool reportedUnavailable;
            private static bool searchedRotationTable;
            private static Quaternion[] cachedRotationTable;
            private static string cachedRotationTableSource;

            internal static bool TryGetEmissionDirection(
                BlockValue blockValue,
                Block block,
                out Vector3i direction)
            {
                direction = default(Vector3i);

                if (TryGetRotationQuaternion(blockValue, block, out Quaternion rotation, out string source))
                {
                    Vector3 emitted = rotation * Vector3.down;
                    if (TrySnapToCardinal(emitted, out direction))
                    {
                        ReportAccessorOnce(source);
                        return true;
                    }
                }

                if (TryGetRawRotation(blockValue, out int rawRotation) &&
                    rawRotation >= 0 && rawRotation <= 3)
                {
                    direction = Vector3i.down;
                    ReportFallbackOnce(rawRotation);
                    return true;
                }

                ReportUnavailableOnce();
                return false;
            }

            private static bool TryGetRotationQuaternion(
                BlockValue blockValue,
                Block block,
                out Quaternion rotation,
                out string source)
            {
                rotation = Quaternion.identity;
                source = null;

                object boxedValue = blockValue;
                TryGetRawRotation(blockValue, out int rawRotation);

                if (TryInvokeQuaternionAccessor(
                        boxedValue,
                        typeof(BlockValue),
                        blockValue,
                        rawRotation,
                        out rotation,
                        out source))
                {
                    return true;
                }

                if (block != null &&
                    TryInvokeQuaternionAccessor(
                        block,
                        block.GetType(),
                        blockValue,
                        rawRotation,
                        out rotation,
                        out source))
                {
                    return true;
                }

                object shape = block == null
                    ? null
                    : GetNamedMemberValue(block, block.GetType(), "shape", "Shape", "blockShape", "BlockShape");

                if (shape != null &&
                    TryInvokeQuaternionAccessor(
                        shape,
                        shape.GetType(),
                        blockValue,
                        rawRotation,
                        out rotation,
                        out source))
                {
                    return true;
                }

                if (rawRotation >= 0 && TryGetRotationFromStaticTable(rawRotation, out rotation, out source))
                    return true;

                return false;
            }

            private static bool TryInvokeQuaternionAccessor(
                object target,
                Type type,
                BlockValue blockValue,
                int rawRotation,
                out Quaternion rotation,
                out string source)
            {
                rotation = Quaternion.identity;
                source = null;

                if (type == null)
                    return false;

                MethodInfo[] methods;
                try
                {
                    methods = type.GetMethods(AnyMember);
                }
                catch
                {
                    return false;
                }

                object boxedValue = blockValue;

                foreach (MethodInfo method in methods)
                {
                    if (method == null || method.ReturnType != typeof(Quaternion))
                        continue;

                    if (method.Name.IndexOf("rot", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    ParameterInfo[] parameters;
                    try
                    {
                        parameters = method.GetParameters();
                    }
                    catch
                    {
                        continue;
                    }

                    object[] args = null;
                    if (parameters.Length == 0 && !method.IsStatic)
                    {
                        args = Array.Empty<object>();
                    }
                    else if (parameters.Length == 1)
                    {
                        Type parameterType = parameters[0].ParameterType;
                        if (parameterType == typeof(BlockValue))
                            args = new[] { boxedValue };
                        else if (rawRotation >= 0 && parameterType == typeof(byte))
                            args = new object[] { (byte)rawRotation };
                        else if (rawRotation >= 0 && parameterType == typeof(int))
                            args = new object[] { rawRotation };
                    }

                    if (args == null)
                        continue;

                    try
                    {
                        object result = method.Invoke(method.IsStatic ? null : target, args);
                        if (result is Quaternion quaternion)
                        {
                            rotation = quaternion;
                            source = type.FullName + "." + method.Name;
                            return true;
                        }
                    }
                    catch
                    {
                        // Keep probing other validated-looking rotation accessors.
                    }
                }

                return false;
            }

            private static bool TryGetRotationFromStaticTable(
                int rawRotation,
                out Quaternion rotation,
                out string source)
            {
                rotation = Quaternion.identity;
                source = null;

                if (!searchedRotationTable)
                {
                    lock (typeof(GrowLightOrientationResolver))
                    {
                        if (!searchedRotationTable)
                        {
                            searchedRotationTable = true;
                            DiscoverRotationTable();
                        }
                    }
                }

                if (cachedRotationTable == null ||
                    rawRotation < 0 ||
                    rawRotation >= cachedRotationTable.Length)
                {
                    return false;
                }

                rotation = cachedRotationTable[rawRotation];
                source = cachedRotationTableSource;
                return true;
            }

            private static void DiscoverRotationTable()
            {
                Type[] types;
                try
                {
                    types = typeof(BlockValue).Assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }
                catch
                {
                    return;
                }

                if (types == null)
                    return;

                foreach (Type type in types)
                {
                    if (type == null)
                        continue;

                    string typeName = type.FullName ?? type.Name;
                    if (typeName.IndexOf("rot", StringComparison.OrdinalIgnoreCase) < 0 &&
                        typeName.IndexOf("block", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    FieldInfo[] fields;
                    try
                    {
                        fields = type.GetFields(AnyMember);
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (FieldInfo field in fields)
                    {
                        if (field == null || !field.IsStatic || field.FieldType != typeof(Quaternion[]))
                            continue;

                        if (field.Name.IndexOf("rot", StringComparison.OrdinalIgnoreCase) < 0)
                            continue;

                        try
                        {
                            Quaternion[] table = field.GetValue(null) as Quaternion[];
                            if (table != null && table.Length >= 24)
                            {
                                cachedRotationTable = table;
                                cachedRotationTableSource = typeName + "." + field.Name;
                                return;
                            }
                        }
                        catch
                        {
                            // Ignore inaccessible/unsafe static candidates.
                        }
                    }
                }
            }

            private static bool TryGetRawRotation(BlockValue blockValue, out int rawRotation)
            {
                rawRotation = -1;
                object boxed = blockValue;
                Type type = typeof(BlockValue);

                object value = GetNamedMemberValue(boxed, type, "rotation", "Rotation");
                if (value == null)
                    return false;

                try
                {
                    rawRotation = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            private static object GetNamedMemberValue(
                object target,
                Type type,
                params string[] names)
            {
                if (target == null || type == null || names == null)
                    return null;

                foreach (string name in names)
                {
                    try
                    {
                        FieldInfo field = type.GetField(name, AnyMember);
                        if (field != null)
                            return field.GetValue(target);
                    }
                    catch
                    {
                    }

                    try
                    {
                        PropertyInfo property = type.GetProperty(name, AnyMember);
                        if (property != null && property.GetIndexParameters().Length == 0)
                            return property.GetValue(target, null);
                    }
                    catch
                    {
                    }
                }

                return null;
            }

            private static bool TrySnapToCardinal(Vector3 vector, out Vector3i direction)
            {
                direction = default(Vector3i);

                if (vector.sqrMagnitude < 0.25f)
                    return false;

                vector.Normalize();
                float ax = Math.Abs(vector.x);
                float ay = Math.Abs(vector.y);
                float az = Math.Abs(vector.z);
                float dominant = Math.Max(ax, Math.Max(ay, az));

                // Advanced block rotations should still land on cardinal axes. Reject diagonals.
                if (dominant < 0.9f)
                    return false;

                if (ax >= ay && ax >= az)
                {
                    direction = new Vector3i(vector.x >= 0f ? 1 : -1, 0, 0);
                    return true;
                }

                if (ay >= ax && ay >= az)
                {
                    direction = new Vector3i(0, vector.y >= 0f ? 1 : -1, 0);
                    return true;
                }

                direction = new Vector3i(0, 0, vector.z >= 0f ? 1 : -1);
                return true;
            }

            private static void ReportAccessorOnce(string source)
            {
                if (reportedAccessor)
                    return;

                reportedAccessor = true;
                LeezLog.Info("Grow-light orientation resolver using V3.1 rotation source: " + source);
            }

            private static void ReportFallbackOnce(int rawRotation)
            {
                if (reportedFallback)
                    return;

                reportedFallback = true;
                LeezLog.Warning(
                    "Grow-light orientation helper was not discoverable; using safe basic-rotation " +
                    "fallback (raw rotation " + rawRotation + " treated as downward). Advanced " +
                    "orientations fail closed until the runtime rotation source is resolved.");
            }

            private static void ReportUnavailableOnce()
            {
                if (reportedUnavailable)
                    return;

                reportedUnavailable = true;
                LeezLog.Warning(
                    "Grow-light orientation could not be resolved for an advanced rotation; " +
                    "that lamp will provide no sunlight substitution or growth bonus.");
            }
        }
    }
}
