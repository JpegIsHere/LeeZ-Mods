using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace LeezGrowLights
{
    /// <summary>
    /// Client-to-server request for cycling a LeeZ grow-light colour.
    /// The packet carries only the block position; the server chooses the next colour
    /// from its authoritative BlockValue and persists it through the existing RPC path.
    /// </summary>
    public sealed class NetPackageGrowLightColourRequest : NetPackage
    {
        private Vector3i position;

        public NetPackageGrowLightColourRequest Setup(Vector3i blockPosition)
        {
            position = blockPosition;
            return this;
        }

        public override void read(PooledBinaryReader reader)
        {
            // V3.1.0 b14's PooledBinaryReader also exposes Span-based overloads.
            // The mod targets .NET Framework 4.8, whose compiler cannot resolve those
            // metadata signatures. Bind primitive reads through BinaryReader instead;
            // PooledBinaryReader inherits it and ReadInt32 is the exact vanilla call.
            System.IO.BinaryReader binaryReader = reader;
            position = new Vector3i(
                binaryReader.ReadInt32(),
                binaryReader.ReadInt32(),
                binaryReader.ReadInt32());
        }

        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer);

            // Same b14 compatibility rule as read(): avoid member lookup over the
            // PooledBinaryWriter Span overload surface when compiling for .NET 4.8.
            System.IO.BinaryWriter binaryWriter = writer;
            binaryWriter.Write(position.x);
            binaryWriter.Write(position.y);
            binaryWriter.Write(position.z);
        }

        public override int GetLength()
        {
            return 20;
        }

        public override void ProcessPackage(World world, GameManager callbacks)
        {
            if (world == null)
            {
                LeezLog.Warning("Grow-light colour request reached ProcessPackage without a world.");
                return;
            }

            ConnectionManager connection = SingletonMonoBehaviour<ConnectionManager>.Instance;
            if (connection == null || !connection.IsServer)
            {
                LeezLog.Warning(
                    "Grow-light colour request ignored because this peer is not the authoritative server.");
                return;
            }

            BlockValue currentValue = world.GetBlock(position);
            Block block = currentValue.Block;
            if (block == null ||
                !GrowLightScanner.TryGetGrowLightCoverage(block, out _, out _, out _))
            {
                LeezLog.Warning(
                    "Rejected remote grow-light colour request for non-LeeZ target at " + position + ".");
                return;
            }

            GrowLightColour current = GrowLightColourState.Get(currentValue);
            GrowLightColour next = GrowLightColourPalette.Next(current);

            LeezLog.Info(
                "Server processing grow-light colour cycle request at " + position +
                ": " + current + " -> " + next + ".");

            if (!GrowLightColourState.TrySet(world, position, currentValue, next))
            {
                LeezLog.Warning(
                    "Server could not persist requested grow-light colour change at " + position + ".");
                return;
            }

            // Listen-server hosts may already have a cached live BlockEntityData. Remote
            // clients deliberately do not apply an optimistic colour here; they wait for
            // the authoritative replicated BlockValue.
            BlockValue updatedValue = GrowLightColourState.WithColour(currentValue, next);
            GrowLightColourVisual.TryApplyCached(position, updatedValue);

            LeezLog.Info(
                "Server-authoritative grow-light colour changed at " + position +
                ": " + current + " -> " + next + ".");
        }
    }

    internal static class GrowLightColourNetwork
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private const string ColourCommandIdPrefix = "growlightcolour_";
        private const string LegacyColourCommandPrefix = "Grow light colour:";

        public static void Install(Harmony harmony)
        {
            Type poweredLightType = AccessTools.TypeByName("BlockPoweredLight");
            if (poweredLightType == null)
            {
                LeezLog.Warning(
                    "BlockPoweredLight not found; multiplayer colour request routing was not installed.");
                return;
            }

            MethodInfo prefix = AccessTools.Method(
                typeof(GrowLightColourNetwork),
                nameof(RemoteActivationPrefix));

            if (prefix == null)
            {
                LeezLog.Warning(
                    "Grow-light multiplayer colour prefix was not found; request routing is disabled.");
                return;
            }

            int installed = 0;
            var seen = new HashSet<string>();

            for (Type type = poweredLightType;
                 type != null && type != typeof(object);
                 type = type.BaseType)
            {
                MethodInfo[] methods;
                try
                {
                    methods = type
                        .GetMethods(InstanceFlags)
                        .Where(m =>
                            string.Equals(m.Name, "OnBlockActivated", StringComparison.Ordinal) &&
                            m.ReturnType == typeof(bool))
                        .OrderBy(m => m.MetadataToken)
                        .ToArray();
                }
                catch (Exception ex)
                {
                    LeezLog.Warning(
                        "Could not enumerate " + type.Name +
                        ".OnBlockActivated for multiplayer colour routing: " + ex.Message);
                    continue;
                }

                foreach (MethodInfo method in methods)
                {
                    string key = method.Module.ModuleVersionId + ":" + method.MetadataToken;
                    if (!seen.Add(key))
                        continue;

                    try
                    {
                        HarmonyMethod harmonyPrefix = new HarmonyMethod(prefix)
                        {
                            priority = Priority.First
                        };
                        harmony.Patch(method, prefix: harmonyPrefix);
                        installed++;
                    }
                    catch (Exception ex)
                    {
                        LeezLog.Warning(
                            "Could not patch " + method.DeclaringType?.Name +
                            ".OnBlockActivated for multiplayer colour routing: " + ex.Message);
                    }
                }
            }

            if (installed > 0)
            {
                LeezLog.Info(
                    "Grow-light multiplayer colour request routing armed on " +
                    installed + " method(s).");
            }
            else
            {
                LeezLog.Warning(
                    "No OnBlockActivated method was patched for multiplayer colour routing.");
            }
        }

        // Priority.First ensures the request is sent before the existing dev8/dev9 colour
        // prefix suppresses remote mutation. This prefix never changes __result or args.
        public static void RemoteActivationPrefix(
            object __instance,
            MethodBase __originalMethod,
            object[] __args)
        {
            Block block = __instance as Block;
            if (block == null ||
                !GrowLightScanner.TryGetGrowLightCoverage(block, out _, out _, out _))
            {
                return;
            }

            WorldBase world = FindFirst<WorldBase>(__args);
            if (world == null || !world.IsRemote())
                return;

            if (!TryGetCommandName(__originalMethod, __args, out string commandName) ||
                !IsColourCommandName(commandName))
            {
                return;
            }

            if (!TryGetFirstVector3i(__args, out Vector3i position))
            {
                LeezLog.Warning(
                    "Remote grow-light colour command could not resolve block position.");
                return;
            }

            TrySendCycleRequest(position);
        }

        public static bool TrySendCycleRequest(Vector3i position)
        {
            try
            {
                ConnectionManager connection = SingletonMonoBehaviour<ConnectionManager>.Instance;
                if (connection == null || !connection.IsClient)
                {
                    LeezLog.Warning(
                        "Remote grow-light colour request could not find an active client connection.");
                    return false;
                }

                NetPackageGrowLightColourRequest package =
                    NetPackageManager.GetPackage<NetPackageGrowLightColourRequest>()
                        .Setup(position);

                connection.SendToServer(package, true);
                LeezLog.Info(
                    "Remote grow-light colour cycle request sent to server for " + position + ".");
                return true;
            }
            catch (Exception ex)
            {
                LeezLog.Warning(
                    "Could not send remote grow-light colour request for " + position +
                    ": " + ex.Message);
                return false;
            }
        }

        private static bool TryGetCommandName(
            MethodBase originalMethod,
            object[] args,
            out string commandName)
        {
            commandName = null;
            if (args == null)
                return false;

            ParameterInfo[] parameters;
            try
            {
                parameters = originalMethod != null
                    ? originalMethod.GetParameters()
                    : new ParameterInfo[0];
            }
            catch
            {
                parameters = new ParameterInfo[0];
            }

            int count = Math.Min(parameters.Length, args.Length);
            for (int i = 0; i < count; i++)
            {
                if (!(args[i] is string text))
                    continue;

                string parameterName = parameters[i].Name ?? string.Empty;
                if (parameterName.IndexOf("command", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    commandName = text;
                    return true;
                }
            }

            foreach (object arg in args)
            {
                if (arg is string text)
                {
                    commandName = text;
                    return true;
                }
            }

            return false;
        }

        private static bool IsColourCommandName(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                return false;

            return commandName.StartsWith(
                       ColourCommandIdPrefix,
                       StringComparison.OrdinalIgnoreCase) ||
                   commandName.StartsWith(
                       LegacyColourCommandPrefix,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static T FindFirst<T>(object[] args) where T : class
        {
            if (args == null)
                return null;

            foreach (object arg in args)
            {
                if (arg is T match)
                    return match;
            }

            return null;
        }

        private static bool TryGetFirstVector3i(object[] args, out Vector3i value)
        {
            value = default(Vector3i);
            if (args == null)
                return false;

            foreach (object arg in args)
            {
                if (arg is Vector3i vector)
                {
                    value = vector;
                    return true;
                }
            }

            return false;
        }
    }
}
