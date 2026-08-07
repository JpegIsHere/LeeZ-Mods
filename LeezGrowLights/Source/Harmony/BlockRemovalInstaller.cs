using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace LeezGrowLights
{
    /// <summary>
    /// Installs the physical grow-light removal transition hook separately from the
    /// electrical transition installer. Keeping this hook isolated makes the V3.1
    /// removal path easy to diagnose without changing the already live-validated
    /// electrical hook set.
    /// </summary>
    internal static class BlockRemovalInstaller
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static void Install(Harmony harmony)
        {
            if (harmony == null)
                return;

            Type poweredBlockType = AccessTools.TypeByName("BlockPowered");
            if (poweredBlockType == null)
            {
                LeezLog.Warning(
                    "BlockPowered was not found; physical grow-light removal rescheduling was not installed.");
                return;
            }

            MethodInfo removalMethod = poweredBlockType
                .GetMethods(InstanceFlags)
                .Where(m => m.Name == "OnBlockRemoved")
                .OrderBy(m => m.DeclaringType == poweredBlockType ? 0 : 1)
                .ThenBy(m => m.MetadataToken)
                .FirstOrDefault();

            if (removalMethod == null)
            {
                LeezLog.Warning(
                    "BlockPowered.OnBlockRemoved was not found; physical grow-light removal rescheduling was not installed.");
                return;
            }

            MethodInfo prefix = AccessTools.Method(
                typeof(BlockRemovalPatches), nameof(BlockRemovalPatches.Prefix));
            MethodInfo postfix = AccessTools.Method(
                typeof(BlockRemovalPatches), nameof(BlockRemovalPatches.Postfix));

            try
            {
                harmony.Patch(
                    removalMethod,
                    prefix: new HarmonyMethod(prefix),
                    postfix: new HarmonyMethod(postfix));

                LeezLog.Info(
                    "Grow-light removal transition hook installed: " +
                    Describe(removalMethod));
            }
            catch (Exception ex)
            {
                LeezLog.Warning(
                    "Could not patch physical grow-light removal transition " +
                    Describe(removalMethod) + ": " + ex.Message);
            }
        }

        private static string Describe(MethodInfo method)
        {
            if (method == null)
                return "<null>";

            return (method.DeclaringType != null ? method.DeclaringType.Name : "<type>") +
                   "." + method.Name +
                   " [token 0x" + method.MetadataToken.ToString("X8") + "]";
        }
    }
}
