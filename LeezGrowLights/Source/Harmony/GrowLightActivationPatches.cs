using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace LeezGrowLights
{
    /// <summary>
    /// Supplies visible icons for the V3.1 powered-light color and brightness commands.
    ///
    /// The inherited powered-light radial menu already exposes the two working commands,
    /// but the current game data can return them with empty icon names. We preserve all
    /// command text, enabled/highlighted state and handlers and only fill the blank icons.
    /// </summary>
    internal static class GrowLightActivationPatches
    {
        private static readonly BindingFlags CommandMemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo IconField = ResolveStringField("icon");
        private static bool loggedIconRepair;
        private static bool reportedMissingIconField;
        private static bool reportedUnsupportedArguments;

        public static void Install(Harmony harmony)
        {
            Type lightType = AccessTools.TypeByName("BlockPoweredLight");
            if (lightType == null)
            {
                LeezLog.Warning("BlockPoweredLight was not found; grow-light radial icons were not patched.");
                return;
            }

            MethodInfo[] candidates;
            try
            {
                candidates = lightType
                    .GetMethods(CommandMemberFlags)
                    .Where(method =>
                        string.Equals(
                            method.Name,
                            "GetBlockActivationCommands",
                            StringComparison.Ordinal))
                    .Where(method => method.DeclaringType == lightType)
                    .OrderBy(method => method.MetadataToken)
                    .ToArray();

                if (candidates.Length == 0)
                {
                    candidates = lightType
                        .GetMethods(CommandMemberFlags)
                        .Where(method =>
                            string.Equals(
                                method.Name,
                                "GetBlockActivationCommands",
                                StringComparison.Ordinal))
                        .OrderBy(method => method.MetadataToken)
                        .ToArray();
                }
            }
            catch (Exception ex)
            {
                LeezLog.Warning(
                    "Could not inspect BlockPoweredLight activation commands: " + ex.Message);
                return;
            }

            int installed = 0;
            foreach (MethodInfo method in candidates)
            {
                try
                {
                    MethodInfo postfix;
                    if (method.ReturnType == typeof(BlockActivationCommand[]))
                    {
                        postfix = AccessTools.Method(
                            typeof(GrowLightActivationPatches), nameof(ResultPostfix));
                    }
                    else if (method.ReturnType == typeof(void))
                    {
                        postfix = AccessTools.Method(
                            typeof(GrowLightActivationPatches), nameof(ArgumentsPostfix));
                    }
                    else
                    {
                        continue;
                    }

                    harmony.Patch(method, postfix: new HarmonyMethod(postfix));
                    installed++;
                }
                catch (Exception ex)
                {
                    LeezLog.Warning(
                        "Could not patch powered-light radial commands: " + ex.Message);
                }
            }

            if (installed == 0)
                LeezLog.Warning("No compatible BlockPoweredLight activation-command method was found.");
            else
                LeezLog.Info("Grow-light radial icon fix installed on " + installed + " hook(s).");
        }

        public static void ResultPostfix(
            Block __instance,
            ref BlockActivationCommand[] __result)
        {
            if (!GrowLightPowerPatches.IsGrowLight(__instance)) return;
            RepairArray(__result);
        }

        /// <summary>
        /// V3.x API probes expose GetBlockActivationCommands as a void method with
        /// its command collection carried through an argument. This postfix supports that
        /// shape as well as arrays returned directly by older/current modding APIs.
        /// </summary>
        public static void ArgumentsPostfix(Block __instance, object[] __args)
        {
            if (!GrowLightPowerPatches.IsGrowLight(__instance) || __args == null) return;

            bool foundCollection = false;
            foreach (object argument in __args)
            {
                if (argument is BlockActivationCommand[] array)
                {
                    foundCollection = true;
                    RepairArray(array);
                    continue;
                }

                if (argument is IList<BlockActivationCommand> list)
                {
                    foundCollection = true;
                    RepairList(list);
                }
            }

            if (!foundCollection && !reportedUnsupportedArguments)
            {
                reportedUnsupportedArguments = true;
                string argumentTypes = string.Join(
                    ", ",
                    __args.Select(argument =>
                        argument == null ? "<null>" : argument.GetType().FullName));
                LeezLog.Warning(
                    "Grow-light radial command hook did not expose a supported command collection. " +
                    "Argument types: " + argumentTypes);
            }
        }

        private static void RepairArray(BlockActivationCommand[] commands)
        {
            if (commands == null || commands.Length < 4) return;

            bool changed = false;
            changed |= SetIconIfBlank(ref commands[2], "tool");
            changed |= SetIconIfBlank(ref commands[3], "wrench");
            LogRepairOnce(changed);
        }

        private static void RepairList(IList<BlockActivationCommand> commands)
        {
            if (commands == null || commands.Count < 4) return;

            bool changed = false;

            BlockActivationCommand color = commands[2];
            if (SetIconIfBlank(ref color, "tool"))
            {
                commands[2] = color;
                changed = true;
            }

            BlockActivationCommand brightness = commands[3];
            if (SetIconIfBlank(ref brightness, "wrench"))
            {
                commands[3] = brightness;
                changed = true;
            }

            LogRepairOnce(changed);
        }

        private static bool SetIconIfBlank(
            ref BlockActivationCommand command,
            string replacementIcon)
        {
            if (IconField == null)
            {
                ReportMissingIconFieldOnce();
                return false;
            }

            object boxed = command;
            string currentIcon = IconField.GetValue(boxed) as string;
            if (!string.IsNullOrWhiteSpace(currentIcon)) return false;

            IconField.SetValue(boxed, replacementIcon);
            command = (BlockActivationCommand)boxed;
            return true;
        }

        private static FieldInfo ResolveStringField(string preferredName)
        {
            FieldInfo exact = AccessTools.Field(typeof(BlockActivationCommand), preferredName);
            if (exact != null && exact.FieldType == typeof(string))
                return exact;

            return typeof(BlockActivationCommand)
                .GetFields(CommandMemberFlags)
                .Where(field => field.FieldType == typeof(string))
                .FirstOrDefault(field =>
                    field.Name.IndexOf(preferredName, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void LogRepairOnce(bool changed)
        {
            if (!changed || loggedIconRepair) return;
            loggedIconRepair = true;
            LeezLog.Info(
                "Grow-light radial icons repaired: color=tool, brightness=wrench.");
        }

        private static void ReportMissingIconFieldOnce()
        {
            if (reportedMissingIconField) return;
            reportedMissingIconField = true;
            LeezLog.Warning(
                "Grow-light radial icon fix could not find the BlockActivationCommand icon field.");
        }
    }
}
