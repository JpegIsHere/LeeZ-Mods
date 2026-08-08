using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace LeezGrowLights
{
    internal static class GrowLightColourInstaller
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public static void Install(Harmony harmony)
        {
            Type poweredLightType = AccessTools.TypeByName("BlockPoweredLight");
            if (poweredLightType == null)
            {
                LeezLog.Warning("BlockPoweredLight not found; colour interaction was not installed.");
                return;
            }

            MethodInfo commandPostfix = AccessTools.Method(
                typeof(GrowLightColourPatches),
                nameof(GrowLightColourPatches.ActivationCommandsPostfix));
            MethodInfo activatedPrefix = AccessTools.Method(
                typeof(GrowLightColourPatches),
                nameof(GrowLightColourPatches.ActivatedPrefix));
            MethodInfo visualPostfix = AccessTools.Method(
                typeof(GrowLightColourPatches),
                nameof(GrowLightColourPatches.VisualPostfix));

            int interactionHooks = 0;
            int visualHooks = 0;

            // Patch the powered-light overrides plus any compatible base implementation.
            // V3.1's radial-menu call path can resolve through a base block method depending
            // on the concrete player light, so dev2 no longer assumes the override is the only
            // entry point.
            interactionHooks += PatchNamedHierarchy(
                harmony,
                poweredLightType,
                "GetBlockActivationCommands",
                typeof(BlockActivationCommand[]),
                null,
                commandPostfix);

            interactionHooks += PatchNamedHierarchy(
                harmony,
                poweredLightType,
                "OnBlockActivated",
                typeof(bool),
                activatedPrefix,
                null);

            visualHooks += PatchNamed(
                harmony,
                poweredLightType,
                "OnBlockEntityTransformAfterActivated",
                null,
                visualPostfix);

            visualHooks += PatchNamed(
                harmony,
                poweredLightType,
                "updateLightState",
                null,
                visualPostfix);

            if (interactionHooks > 0)
            {
                LeezLog.Info(
                    "Grow-light colour interaction armed on " + interactionHooks + " method(s).");
            }
            else
            {
                LeezLog.Warning("No grow-light colour interaction methods could be patched.");
            }

            if (visualHooks > 0)
            {
                LeezLog.Info(
                    "Grow-light colour visual refresh armed on " + visualHooks + " method(s).");
            }
            else
            {
                LeezLog.Warning("No grow-light colour visual refresh methods could be patched.");
            }
        }

        private static int PatchNamedHierarchy(
            Harmony harmony,
            Type startType,
            string methodName,
            Type expectedReturnType,
            MethodInfo prefix,
            MethodInfo postfix)
        {
            int installed = 0;
            var seen = new HashSet<string>();

            for (Type type = startType;
                 type != null && type != typeof(object);
                 type = type.BaseType)
            {
                MethodInfo[] methods;
                try
                {
                    methods = type
                        .GetMethods(InstanceFlags)
                        .Where(m =>
                            string.Equals(m.Name, methodName, StringComparison.Ordinal) &&
                            (expectedReturnType == null || m.ReturnType == expectedReturnType))
                        .OrderBy(m => m.MetadataToken)
                        .ToArray();
                }
                catch (Exception ex)
                {
                    LeezLog.Warning(
                        "Could not enumerate " + type.Name + "." + methodName + ": " + ex.Message);
                    continue;
                }

                foreach (MethodInfo method in methods)
                {
                    string key = method.Module.ModuleVersionId + ":" + method.MetadataToken;
                    if (!seen.Add(key))
                        continue;

                    if (TryPatch(harmony, method, prefix, postfix))
                        installed++;
                }
            }

            return installed;
        }

        private static int PatchNamed(
            Harmony harmony,
            Type type,
            string methodName,
            MethodInfo prefix,
            MethodInfo postfix)
        {
            MethodInfo[] methods;
            try
            {
                methods = type
                    .GetMethods(InstanceFlags)
                    .Where(m => string.Equals(m.Name, methodName, StringComparison.Ordinal))
                    .OrderBy(m => m.MetadataToken)
                    .ToArray();
            }
            catch (Exception ex)
            {
                LeezLog.Warning(
                    "Could not enumerate BlockPoweredLight." + methodName + ": " + ex.Message);
                return 0;
            }

            int installed = 0;
            var seen = new HashSet<int>();
            foreach (MethodInfo method in methods)
            {
                if (!seen.Add(method.MetadataToken))
                    continue;

                if (TryPatch(harmony, method, prefix, postfix))
                    installed++;
            }

            return installed;
        }

        private static bool TryPatch(
            Harmony harmony,
            MethodInfo method,
            MethodInfo prefix,
            MethodInfo postfix)
        {
            try
            {
                harmony.Patch(
                    method,
                    prefix: prefix != null ? new HarmonyMethod(prefix) : null,
                    postfix: postfix != null ? new HarmonyMethod(postfix) : null);

                LeezLog.Info(
                    "Grow-light colour hook installed: " +
                    method.DeclaringType?.Name + "." + method.Name +
                    " [token 0x" + method.MetadataToken.ToString("X8") + "]");
                return true;
            }
            catch (Exception ex)
            {
                LeezLog.Warning(
                    "Could not patch " + method.DeclaringType?.Name + "." + method.Name +
                    ": " + ex.Message);
                return false;
            }
        }
    }
}
