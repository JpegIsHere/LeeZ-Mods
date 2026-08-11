using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace LeezGrowLights
{
    /// <summary>
    /// Final-stage radial-menu filter for the fixed-by-tier brightness design.
    ///
    /// The original selectable-brightness implementation remains compiled and intact
    /// in GrowLightColourPatches/GrowLightColourState for future reuse, but LeeZ grow
    /// lights no longer expose that command to players.
    /// </summary>
    internal static class GrowLightTierBrightness
    {
        private const string BrightnessCommandIdPrefix = "growlightbrightness_";
        private const string LegacyBrightnessCommandPrefix = "Grow light brightness:";
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public static void Install(Harmony harmony)
        {
            Type poweredLightType = AccessTools.TypeByName("BlockPoweredLight");
            if (poweredLightType == null)
            {
                LeezLog.Warning("BlockPoweredLight not found; fixed-tier brightness menu filter was not installed.");
                return;
            }

            MethodInfo postfix = AccessTools.Method(
                typeof(GrowLightTierBrightness),
                nameof(ActivationCommandsPostfix));
            if (postfix == null)
            {
                LeezLog.Warning("Fixed-tier brightness postfix could not be resolved.");
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
                            string.Equals(m.Name, "GetBlockActivationCommands", StringComparison.Ordinal) &&
                            m.ReturnType == typeof(BlockActivationCommand[]))
                        .OrderBy(m => m.MetadataToken)
                        .ToArray();
                }
                catch (Exception ex)
                {
                    LeezLog.Warning(
                        "Could not enumerate " + type.Name +
                        ".GetBlockActivationCommands for tier-brightness filtering: " + ex.Message);
                    continue;
                }

                foreach (MethodInfo method in methods)
                {
                    string key = method.Module.ModuleVersionId + ":" + method.MetadataToken;
                    if (!seen.Add(key))
                        continue;

                    try
                    {
                        var harmonyPostfix = new HarmonyMethod(postfix)
                        {
                            priority = Priority.Last
                        };
                        harmony.Patch(method, postfix: harmonyPostfix);
                        installed++;
                    }
                    catch (Exception ex)
                    {
                        LeezLog.Warning(
                            "Could not install fixed-tier brightness menu filter on " +
                            method.DeclaringType?.Name + "." + method.Name + ": " + ex.Message);
                    }
                }
            }

            if (installed > 0)
            {
                LeezLog.Info(
                    "Fixed-tier brightness active; player brightness command hidden on " +
                    installed + " activation-command method(s).");
            }
            else
            {
                LeezLog.Warning("No activation-command methods were patched for fixed-tier brightness.");
            }
        }

        public static void ActivationCommandsPostfix(
            object __instance,
            ref BlockActivationCommand[] __result)
        {
            Block block = __instance as Block;
            if (block == null ||
                !GrowLightScanner.TryGetGrowLightCoverage(block, out _, out _, out _) ||
                __result == null ||
                __result.Length == 0)
            {
                return;
            }

            int keepCount = 0;
            for (int i = 0; i < __result.Length; i++)
            {
                if (!IsBrightnessCommand(__result[i].text))
                    keepCount++;
            }

            if (keepCount == __result.Length)
                return;

            var filtered = new BlockActivationCommand[keepCount];
            int destination = 0;
            for (int i = 0; i < __result.Length; i++)
            {
                if (IsBrightnessCommand(__result[i].text))
                    continue;

                filtered[destination++] = __result[i];
            }

            __result = filtered;
        }

        private static bool IsBrightnessCommand(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                return false;

            return commandName.StartsWith(
                       BrightnessCommandIdPrefix,
                       StringComparison.OrdinalIgnoreCase) ||
                   commandName.StartsWith(
                       LegacyBrightnessCommandPrefix,
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
