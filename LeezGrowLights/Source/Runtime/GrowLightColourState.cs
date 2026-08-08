using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LeezGrowLights
{
    /// <summary>
    /// Stores the selected LeeZ colour and cosmetic brightness in BlockValue.meta2.
    ///
    /// Stored value 0 is reserved for legacy/uninitialised blocks and maps to
    /// White + Normal. Existing dev7 values 1..6 keep their original colours and
    /// also map to Normal brightness. The remaining values encode the same six
    /// colours at Dim, Bright, Very Bright, and Maximum brightness.
    ///
    /// Electrical toggle state remains in TileEntityPoweredBlock.isToggled;
    /// changing this metadata therefore does not replace the powered tile entity.
    /// </summary>
    internal static class GrowLightColourState
    {
        private const byte StorageOffset = 1;
        private const int ColourCount = 6;
        private const byte MaxStoredValue = 30;

        public static GrowLightColour Get(BlockValue blockValue)
        {
            if (!TryDecode(blockValue.meta2, out GrowLightColour colour, out _))
                return GrowLightColourPalette.Default;

            return colour;
        }

        public static GrowLightBrightness GetBrightness(BlockValue blockValue)
        {
            if (!TryDecode(blockValue.meta2, out _, out GrowLightBrightness brightness))
                return GrowLightBrightnessPalette.Default;

            return brightness;
        }

        public static BlockValue WithColour(BlockValue blockValue, GrowLightColour colour)
        {
            GrowLightBrightness brightness = GetBrightness(blockValue);
            blockValue.meta2 = Encode(colour, brightness);
            return blockValue;
        }

        public static BlockValue WithBrightness(
            BlockValue blockValue,
            GrowLightBrightness brightness)
        {
            GrowLightColour colour = Get(blockValue);
            blockValue.meta2 = Encode(colour, brightness);
            return blockValue;
        }

        public static bool TrySet(
            WorldBase world,
            Vector3i position,
            BlockValue currentValue,
            GrowLightColour colour)
        {
            return TryPersistUpdatedValue(
                world,
                position,
                currentValue,
                WithColour(currentValue, colour));
        }

        public static bool TrySetBrightness(
            WorldBase world,
            Vector3i position,
            BlockValue currentValue,
            GrowLightBrightness brightness)
        {
            return TryPersistUpdatedValue(
                world,
                position,
                currentValue,
                WithBrightness(currentValue, brightness));
        }

        private static bool TryPersistUpdatedValue(
            WorldBase world,
            Vector3i position,
            BlockValue currentValue,
            BlockValue updatedValue)
        {
            if (world == null)
                return false;

            Block block = currentValue.Block;
            if (block == null ||
                !GrowLightScanner.TryGetGrowLightCoverage(block, out _, out _, out _))
            {
                return false;
            }

            if (updatedValue.rawData == currentValue.rawData)
                return true;

            try
            {
                if (TryInvokeDirectSetBlockRpc(world, position, updatedValue))
                    return true;

                BlockChangeInfo change = CreateBlockChange(position, updatedValue);

                MethodInfo setBlockRpc = world.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => string.Equals(m.Name, "SetBlockRPC", StringComparison.Ordinal))
                    .FirstOrDefault(m =>
                    {
                        ParameterInfo[] p = SafeParameters(m);
                        return p.Length == 1 && p[0].ParameterType == typeof(BlockChangeInfo);
                    });

                if (setBlockRpc != null)
                {
                    setBlockRpc.Invoke(world, new object[] { change });
                    LeezLog.Info(
                        "Grow-light visual state persisted through " + DescribeMethod(setBlockRpc) + ".");
                    return true;
                }

                GameManager manager = GameManager.Instance;
                if (manager == null)
                    return false;

                var changes = new List<BlockChangeInfo> { change };
                MethodInfo batchMethod = manager.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => string.Equals(m.Name, "SetBlocksRPC", StringComparison.Ordinal))
                    .FirstOrDefault(m =>
                    {
                        ParameterInfo[] p = SafeParameters(m);
                        return p.Length >= 1 &&
                               p[0].ParameterType.IsAssignableFrom(changes.GetType());
                    });

                if (batchMethod == null)
                {
                    LeezLog.Warning(
                        "No compatible SetBlockRPC/SetBlocksRPC method was found for grow-light visual-state persistence.");
                    return false;
                }

                ParameterInfo[] parameters = SafeParameters(batchMethod);
                object[] args = new object[parameters.Length];
                args[0] = changes;
                for (int i = 1; i < args.Length; i++)
                    args[i] = DefaultValue(parameters[i].ParameterType);

                batchMethod.Invoke(manager, args);
                LeezLog.Info(
                    "Grow-light visual state persisted through " + DescribeMethod(batchMethod) + ".");
                return true;
            }
            catch (Exception ex)
            {
                Exception display = ex is TargetInvocationException && ex.InnerException != null
                    ? ex.InnerException
                    : ex;
                LeezLog.Warning(
                    "Could not persist grow-light visual state at " + position + ": " + display.Message);
                return false;
            }
        }

        private static bool TryDecode(
            byte stored,
            out GrowLightColour colour,
            out GrowLightBrightness brightness)
        {
            colour = GrowLightColourPalette.Default;
            brightness = GrowLightBrightnessPalette.Default;

            if (stored < StorageOffset || stored > MaxStoredValue)
                return false;

            int zeroBased = stored - StorageOffset;
            int colourIndex = zeroBased % ColourCount;
            int brightnessBucket = zeroBased / ColourCount;

            colour = (GrowLightColour)colourIndex;
            brightness = BrightnessFromStorageBucket(brightnessBucket);
            return true;
        }

        private static byte Encode(
            GrowLightColour colour,
            GrowLightBrightness brightness)
        {
            int colourIndex = (byte)colour;
            if (colourIndex < 0 || colourIndex >= ColourCount)
                colourIndex = (byte)GrowLightColourPalette.Default;

            int brightnessBucket = BrightnessToStorageBucket(brightness);
            return (byte)(StorageOffset + colourIndex + brightnessBucket * ColourCount);
        }

        private static int BrightnessToStorageBucket(GrowLightBrightness brightness)
        {
            switch (brightness)
            {
                case GrowLightBrightness.Dim:
                    return 1;
                case GrowLightBrightness.Bright:
                    return 2;
                case GrowLightBrightness.VeryBright:
                    return 3;
                case GrowLightBrightness.Maximum:
                    return 4;
                case GrowLightBrightness.Normal:
                default:
                    return 0;
            }
        }

        private static GrowLightBrightness BrightnessFromStorageBucket(int bucket)
        {
            switch (bucket)
            {
                case 1:
                    return GrowLightBrightness.Dim;
                case 2:
                    return GrowLightBrightness.Bright;
                case 3:
                    return GrowLightBrightness.VeryBright;
                case 4:
                    return GrowLightBrightness.Maximum;
                case 0:
                default:
                    return GrowLightBrightness.Normal;
            }
        }

        private static bool TryInvokeDirectSetBlockRpc(
            WorldBase world,
            Vector3i position,
            BlockValue value)
        {
            MethodInfo[] candidates;
            try
            {
                candidates = world.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => string.Equals(m.Name, "SetBlockRPC", StringComparison.Ordinal))
                    .Where(m =>
                    {
                        ParameterInfo[] p = SafeParameters(m);
                        return p.Any(x => x.ParameterType == typeof(Vector3i)) &&
                               p.Any(x => x.ParameterType == typeof(BlockValue));
                    })
                    .OrderBy(m => SafeParameters(m).Length)
                    .ToArray();
            }
            catch
            {
                return false;
            }

            foreach (MethodInfo method in candidates)
            {
                ParameterInfo[] parameters = SafeParameters(method);
                object[] args = new object[parameters.Length];
                bool positionAssigned = false;
                bool valueAssigned = false;

                for (int i = 0; i < parameters.Length; i++)
                {
                    Type type = parameters[i].ParameterType;
                    if (!positionAssigned && type == typeof(Vector3i))
                    {
                        args[i] = position;
                        positionAssigned = true;
                    }
                    else if (!valueAssigned && type == typeof(BlockValue))
                    {
                        args[i] = value;
                        valueAssigned = true;
                    }
                    else
                    {
                        args[i] = DefaultValue(type);
                    }
                }

                try
                {
                    object result = method.Invoke(world, args);
                    if (method.ReturnType == typeof(bool) && result is bool ok && !ok)
                    {
                        LeezLog.Warning(
                            "Grow-light direct block RPC returned false: " + DescribeMethod(method) + ".");
                        continue;
                    }

                    LeezLog.Info(
                        "Grow-light visual state persisted through direct " + DescribeMethod(method) + ".");
                    return true;
                }
                catch (Exception ex)
                {
                    Exception display = ex is TargetInvocationException && ex.InnerException != null
                        ? ex.InnerException
                        : ex;
                    LeezLog.Warning(
                        "Grow-light direct block RPC candidate failed: " +
                        DescribeMethod(method) + ": " + display.Message);
                }
            }

            return false;
        }

        private static BlockChangeInfo CreateBlockChange(Vector3i position, BlockValue value)
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            object boxed = Activator.CreateInstance(typeof(BlockChangeInfo));
            FieldInfo blockValueRefField = typeof(BlockChangeInfo).GetField("blockValueRef", flags);
            FieldInfo blockValueField = typeof(BlockChangeInfo).GetField("blockValue", flags);
            FieldInfo changeBlockValueField = typeof(BlockChangeInfo).GetField("bChangeBlockValue", flags);

            if (blockValueRefField == null ||
                blockValueField == null ||
                changeBlockValueField == null)
            {
                throw new MissingFieldException(
                    "V3.1 BlockChangeInfo is missing blockValueRef/bChangeBlockValue/blockValue fields.");
            }

            object blockValueRef = CreateBlockValueRef(position, value, blockValueRefField.FieldType);
            blockValueRefField.SetValue(boxed, blockValueRef);
            blockValueField.SetValue(boxed, value);
            changeBlockValueField.SetValue(boxed, true);

            FieldInfo updateLightField = typeof(BlockChangeInfo).GetField("bUpdateLight", flags);
            if (updateLightField != null && updateLightField.FieldType == typeof(bool))
                updateLightField.SetValue(boxed, true);

            LeezLog.Info(
                "Grow-light BlockChangeInfo prepared with BlockValueRef for " + position + ".");
            return (BlockChangeInfo)boxed;
        }

        private static object CreateBlockValueRef(
            Vector3i position,
            BlockValue value,
            Type blockValueRefType)
        {
            if (blockValueRefType == null)
                throw new ArgumentNullException(nameof(blockValueRefType));

            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            ConstructorInfo[] constructors = blockValueRefType
                .GetConstructors(flags)
                .OrderBy(c => SafeParameters(c).Length)
                .ToArray();

            foreach (ConstructorInfo constructor in constructors)
            {
                ParameterInfo[] parameters = SafeParameters(constructor);
                bool hasPosition = parameters.Any(p => p.ParameterType == typeof(Vector3i));
                if (!hasPosition)
                    continue;

                object[] args = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    Type type = parameters[i].ParameterType;
                    if (type == typeof(Vector3i))
                        args[i] = position;
                    else if (type == typeof(BlockValue))
                        args[i] = value;
                    else
                        args[i] = DefaultValue(type);
                }

                object created = constructor.Invoke(args);
                LeezLog.Info(
                    "Grow-light BlockValueRef created through " + DescribeConstructor(constructor) + ".");
                return created;
            }

            object boxed = Activator.CreateInstance(blockValueRefType);
            bool positionAssigned = TryAssignPosition(boxed, blockValueRefType, position, flags);
            if (positionAssigned)
            {
                TryAssignBlockValue(boxed, blockValueRefType, value, flags);
                LeezLog.Info(
                    "Grow-light BlockValueRef created through writable V3.1 members.");
                return boxed;
            }

            string fields = string.Join(
                ", ",
                blockValueRefType.GetFields(flags)
                    .Select(f => f.FieldType.Name + " " + f.Name)
                    .ToArray());
            string constructorsText = string.Join(
                "; ",
                constructors.Select(DescribeConstructor).ToArray());

            throw new MissingMethodException(
                "BlockValueRef exposes no usable position constructor or writable position members. " +
                "Fields=[" + fields + "] Constructors=[" + constructorsText + "]");
        }

        private static bool TryAssignPosition(
            object boxed,
            Type type,
            Vector3i position,
            BindingFlags flags)
        {
            foreach (FieldInfo field in type.GetFields(flags))
            {
                if (field.FieldType == typeof(Vector3i))
                {
                    field.SetValue(boxed, position);
                    return true;
                }
            }

            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                if (property.CanWrite && property.PropertyType == typeof(Vector3i))
                {
                    property.SetValue(boxed, position, null);
                    return true;
                }
            }

            bool x = TryAssignCoordinate(boxed, type, "x", position.x, flags);
            bool y = TryAssignCoordinate(boxed, type, "y", position.y, flags);
            bool z = TryAssignCoordinate(boxed, type, "z", position.z, flags);
            return x && y && z;
        }

        private static bool TryAssignCoordinate(
            object boxed,
            Type type,
            string axis,
            int value,
            BindingFlags flags)
        {
            string[] names =
            {
                axis,
                "block" + axis.ToUpperInvariant(),
                "pos" + axis.ToUpperInvariant(),
                "position" + axis.ToUpperInvariant()
            };

            foreach (string name in names)
            {
                FieldInfo field = type.GetField(name, flags | BindingFlags.IgnoreCase);
                if (field != null && field.FieldType == typeof(int))
                {
                    field.SetValue(boxed, value);
                    return true;
                }

                PropertyInfo property = type.GetProperty(name, flags | BindingFlags.IgnoreCase);
                if (property != null && property.CanWrite && property.PropertyType == typeof(int))
                {
                    property.SetValue(boxed, value, null);
                    return true;
                }
            }

            return false;
        }

        private static void TryAssignBlockValue(
            object boxed,
            Type type,
            BlockValue value,
            BindingFlags flags)
        {
            foreach (FieldInfo field in type.GetFields(flags))
            {
                if (field.FieldType == typeof(BlockValue))
                {
                    field.SetValue(boxed, value);
                    return;
                }
            }

            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                if (property.CanWrite && property.PropertyType == typeof(BlockValue))
                {
                    property.SetValue(boxed, value, null);
                    return;
                }
            }
        }

        private static string DescribeMethod(MethodInfo method)
        {
            if (method == null)
                return "<null>";

            ParameterInfo[] parameters = SafeParameters(method);
            return method.DeclaringType?.Name + "." + method.Name +
                   "(" + string.Join(", ", parameters.Select(p => p.ParameterType.Name + " " + p.Name).ToArray()) + ")";
        }

        private static string DescribeConstructor(ConstructorInfo constructor)
        {
            if (constructor == null)
                return "<null>";

            ParameterInfo[] parameters = SafeParameters(constructor);
            return constructor.DeclaringType?.Name +
                   "(" + string.Join(", ", parameters.Select(p => p.ParameterType.Name + " " + p.Name).ToArray()) + ")";
        }

        private static ParameterInfo[] SafeParameters(MethodBase method)
        {
            try
            {
                return method.GetParameters();
            }
            catch
            {
                return Array.Empty<ParameterInfo>();
            }
        }

        private static object DefaultValue(Type type)
        {
            if (type == null || !type.IsValueType)
                return null;

            return Activator.CreateInstance(type);
        }
    }
}
