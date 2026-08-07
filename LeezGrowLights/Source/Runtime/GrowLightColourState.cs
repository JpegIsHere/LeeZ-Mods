using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LeezGrowLights
{
    /// <summary>
    /// Stores the selected LeeZ colour in BlockValue.meta2.
    ///
    /// Stored value 0 is reserved for legacy/uninitialised blocks and maps to White.
    /// Values 1..6 map to the six GrowLightColour enum values + 1.  The powered
    /// light's electrical toggle state remains in TileEntityPoweredBlock.isToggled;
    /// changing this metadata therefore does not replace the powered tile entity.
    /// </summary>
    internal static class GrowLightColourState
    {
        private const byte StorageOffset = 1;
        private const byte MaxStoredValue = 6;

        public static GrowLightColour Get(BlockValue blockValue)
        {
            byte stored = blockValue.meta2;
            if (stored < StorageOffset || stored > MaxStoredValue)
                return GrowLightColourPalette.Default;

            return (GrowLightColour)(stored - StorageOffset);
        }

        public static BlockValue WithColour(BlockValue blockValue, GrowLightColour colour)
        {
            blockValue.meta2 = (byte)((byte)colour + StorageOffset);
            return blockValue;
        }

        public static bool TrySet(
            WorldBase world,
            Vector3i position,
            BlockValue currentValue,
            GrowLightColour colour)
        {
            if (world == null)
                return false;

            Block block = currentValue.Block;
            if (block == null ||
                !GrowLightScanner.TryGetGrowLightCoverage(block, out _, out _, out _))
            {
                return false;
            }

            BlockValue updatedValue = WithColour(currentValue, colour);
            if (updatedValue.rawData == currentValue.rawData)
                return true;

            try
            {
                BlockChangeInfo change = CreateBlockChange(position, updatedValue);

                // Prefer the normal WorldBase single-block RPC.  It preserves the block
                // type and tile entity while replicating the BlockValue to connected peers.
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
                    return true;
                }

                // V3.1 also exposes GameManager.SetBlocksRPC.  Keep this reflection fallback
                // so minor signature changes do not force the colour feature to replace blocks.
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
                    return false;

                ParameterInfo[] parameters = SafeParameters(batchMethod);
                object[] args = new object[parameters.Length];
                args[0] = changes;
                for (int i = 1; i < args.Length; i++)
                    args[i] = DefaultValue(parameters[i].ParameterType);

                batchMethod.Invoke(manager, args);
                return true;
            }
            catch (Exception ex)
            {
                LeezLog.Warning(
                    "Could not persist grow-light colour at " + position + ": " + ex.Message);
                return false;
            }
        }

        private static BlockChangeInfo CreateBlockChange(Vector3i position, BlockValue value)
        {
            ConstructorInfo[] constructors = typeof(BlockChangeInfo)
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .OrderBy(c => SafeParameters(c).Length)
                .ToArray();

            foreach (ConstructorInfo constructor in constructors)
            {
                ParameterInfo[] parameters = SafeParameters(constructor);
                bool hasPosition = parameters.Any(p => p.ParameterType == typeof(Vector3i));
                bool hasValue = parameters.Any(p => p.ParameterType == typeof(BlockValue));
                if (!hasPosition || !hasValue)
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

                return (BlockChangeInfo)constructor.Invoke(args);
            }

            throw new MissingMethodException(
                "No BlockChangeInfo constructor containing Vector3i and BlockValue was found.");
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
