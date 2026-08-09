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

        public static void ResultPostfix(
            Block __instance,
            ref BlockActivationCommand[] __result)
        {
            if (!GrowLightPowerPatches.IsGrowLight(__instance)) return;
            RepairArray(__result);
        }

        /// <summary>
        /// V3.x API probes can expose GetBlockActivationCommands as a void method with
        /// its command collection carried through an argument. This postfix supports that
        /// shape as well as arrays returned directly by older/current modding APIs.
        /// </summary>
        public static void ArgumentsPostfix(Block __instance, object[] __args)
        {
            if (!GrowLightPowerPatches.IsGrowLight(__instance) || __args == null) return;

            foreach (object argument in __args)
            {
                if (argument is BlockActivationCommand[] array)
                {
                    RepairArray(array);
                    continue;
                }

                if (argument is IList<BlockActivationCommand> list)
                    RepairList(list);
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
