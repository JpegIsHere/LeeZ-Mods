using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace LeezGrowLights
{
    internal static class PatchInstaller
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static MethodInfo ScheduledTickMethod { get; private set; }
        internal static MethodInfo UpdateTickMethod { get; private set; }
        internal static MethodInfo GetTickRateMethod { get; private set; }

        private static readonly string[] SunlightMethodNames =
        {
            "CanPlaceBlockAt",
            "CanPlantStay",
            "CanGrowOn",
            "CheckPlantAlive",
            "UpdateTick"
        };

        public static void Install(Harmony harmony)
        {
            Type plantType = AccessTools.TypeByName("BlockPlantGrowing");
            if (plantType == null)
            {
                LeezLog.Error("BlockPlantGrowing was not found. No runtime crop patches installed.");
                return;
            }

            GetTickRateMethod = plantType.GetMethod(
                "GetTickRate",
                InstanceFlags,
                null,
                Type.EmptyTypes,
                null);

            ScheduledTickMethod = FindPreferredMethodByName(plantType, "addScheduledTick");
            UpdateTickMethod = FindPreferredMethodByName(plantType, "UpdateTick");

            InstallGrowthSpeedPatches(harmony, plantType);
            InstallSunlightSubstitutionPatches(harmony, plantType);
            InstallPowerTransitionPatches(harmony);
        }

        private static void InstallGrowthSpeedPatches(Harmony harmony, Type plantType)
        {
            if (GetTickRateMethod == null || ScheduledTickMethod == null)
            {
                LeezLog.Error(
                    "The V3.1 crop scheduling API did not expose GetTickRate/addScheduledTick as expected. " +
                    "Grow-light acceleration was not installed.");
                DumpNamedCandidates(plantType);
                return;
            }

            MethodInfo contextPrefix =
                AccessTools.Method(typeof(PlantGrowthPatches), nameof(PlantGrowthPatches.ContextPrefix));
            MethodInfo contextPostfix =
                AccessTools.Method(typeof(PlantGrowthPatches), nameof(PlantGrowthPatches.ContextPostfix));
            MethodInfo tickRatePostfix =
                AccessTools.Method(typeof(PlantGrowthPatches), nameof(PlantGrowthPatches.TickRatePostfix));

            harmony.Patch(
                ScheduledTickMethod,
                prefix: new HarmonyMethod(contextPrefix),
                postfix: new HarmonyMethod(contextPostfix));

            harmony.Patch(
                GetTickRateMethod,
                postfix: new HarmonyMethod(tickRatePostfix));

            if (UpdateTickMethod != null)
            {
                harmony.Patch(
                    UpdateTickMethod,
                    prefix: new HarmonyMethod(contextPrefix),
                    postfix: new HarmonyMethod(contextPostfix));
            }

            LeezLog.Info("V3.1 crop scheduling hook installed: " + Describe(ScheduledTickMethod));
            LeezLog.Info("V3.1 tick-rate hook installed: " + Describe(GetTickRateMethod));
            if (UpdateTickMethod != null)
                LeezLog.Info("V3.1 crop update context hook installed: " + Describe(UpdateTickMethod));
            else
                LeezLog.Warning("BlockPlantGrowing.UpdateTick was not found; scheduling hook remains active.");
        }

        private static void InstallSunlightSubstitutionPatches(Harmony harmony, Type plantType)
        {
            FieldInfo growField = AccessTools.Field(plantType, "lightLevelGrow");
            FieldInfo stayField = AccessTools.Field(plantType, "lightLevelStay");
            if (growField == null || stayField == null)
            {
                LeezLog.Error(
                    "V3.1 crop light threshold fields were not found. Underground sunlight substitution was not installed.");
                return;
            }

            MethodInfo prefix = AccessTools.Method(
                typeof(SunlightSubstitutionPatches), nameof(SunlightSubstitutionPatches.Prefix));
            MethodInfo postfix = AccessTools.Method(
                typeof(SunlightSubstitutionPatches), nameof(SunlightSubstitutionPatches.Postfix));
            MethodInfo finalizer = AccessTools.Method(
                typeof(SunlightSubstitutionPatches), nameof(SunlightSubstitutionPatches.Finalizer));

            var patchedTokens = new HashSet<int>();
            int installed = 0;

            foreach (string methodName in SunlightMethodNames)
            {
                foreach (MethodInfo method in FindHierarchyMethodsByName(plantType, methodName))
                {
                    if (!patchedTokens.Add(method.MetadataToken))
                        continue;

                    try
                    {
                        harmony.Patch(
                            method,
                            prefix: new HarmonyMethod(prefix),
                            postfix: new HarmonyMethod(postfix),
                            finalizer: new HarmonyMethod(finalizer));

                        installed++;
                        LeezLog.Info("Grow-light sunlight hook installed: " + DescribeDetailed(method));
                    }
                    catch (Exception ex)
                    {
                        LeezLog.Warning(
                            "Could not patch sunlight check " + Describe(method) + ": " + ex.Message);
                    }
                }
            }

            if (installed == 0)
                LeezLog.Error("No crop sunlight-check methods could be patched.");
            else
                LeezLog.Info("Grow-light sunlight substitution installed on " + installed + " crop method(s).");
        }

        private static void InstallPowerTransitionPatches(Harmony harmony)
        {
            Type powerToggleType = AccessTools.TypeByName("PowerConsumerToggle");
            if (powerToggleType == null)
            {
                LeezLog.Warning(
                    "PowerConsumerToggle was not found; live mid-stage rescheduling was not installed.");
                return;
            }

            MethodInfo prefix = AccessTools.Method(
                typeof(PowerTransitionPatches), nameof(PowerTransitionPatches.Prefix));
            MethodInfo postfix = AccessTools.Method(
                typeof(PowerTransitionPatches), nameof(PowerTransitionPatches.Postfix));

            string[] transitionMethods =
            {
                "set_IsToggled",
                "HandlePowerReceived",
                "HandlePowerUpdate",
                "HandleDisconnect"
            };

            var patchedTokens = new HashSet<int>();
            int installed = 0;

            foreach (string methodName in transitionMethods)
            {
                MethodInfo method = FindPreferredMethodByName(powerToggleType, methodName);
                if (method == null || !patchedTokens.Add(method.MetadataToken))
                {
                    LeezLog.Warning(
                        "Electrical transition hook not found/duplicate: PowerConsumerToggle." +
                        methodName);
                    continue;
                }

                try
                {
                    harmony.Patch(
                        method,
                        prefix: new HarmonyMethod(prefix),
                        postfix: new HarmonyMethod(postfix));

                    installed++;
                    LeezLog.Info(
                        "Mid-stage electrical transition hook installed: " +
                        DescribeDetailed(method));
                }
                catch (Exception ex)
                {
                    LeezLog.Warning(
                        "Could not patch electrical transition " +
                        Describe(method) + ": " + ex.Message);
                }
            }

            if (installed == 0)
                LeezLog.Warning("No live electrical transition hooks were installed.");
            else
                LeezLog.Info(
                    "Progress-preserving mid-stage rescheduler armed on " +
                    installed + " electrical transition method(s).");
        }

        private static MethodInfo FindPreferredMethodByName(Type type, string name)
        {
            MethodInfo[] declared = type
                .GetMethods(InstanceFlags)
                .Where(m => string.Equals(m.Name, name, StringComparison.Ordinal))
                .Where(m => m.DeclaringType == type)
                .OrderBy(m => m.MetadataToken)
                .ToArray();

            if (declared.Length > 0)
                return declared[0];

            return type
                .GetMethods(InstanceFlags)
                .Where(m => string.Equals(m.Name, name, StringComparison.Ordinal))
                .OrderBy(m => InheritanceDistance(type, m.DeclaringType))
                .ThenBy(m => m.MetadataToken)
                .FirstOrDefault();
        }

        private static IEnumerable<MethodInfo> FindHierarchyMethodsByName(Type type, string name)
        {
            var seen = new HashSet<string>();

            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                MethodInfo[] methods;
                try
                {
                    methods = current.GetMethods(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                }
                catch
                {
                    continue;
                }

                foreach (MethodInfo method in methods
                             .Where(m => string.Equals(m.Name, name, StringComparison.Ordinal))
                             .OrderBy(m => m.MetadataToken))
                {
                    string key = method.Module.ModuleVersionId + ":" + method.MetadataToken;
                    if (seen.Add(key))
                        yield return method;
                }
            }
        }

        private static int InheritanceDistance(Type child, Type possibleBase)
        {
            int distance = 0;
            for (Type current = child; current != null; current = current.BaseType, distance++)
            {
                if (current == possibleBase)
                    return distance;
            }
            return int.MaxValue;
        }

        private static void DumpNamedCandidates(Type plantType)
        {
            try
            {
                foreach (MethodInfo method in plantType.GetMethods(InstanceFlags)
                             .Where(m =>
                                 m.Name.IndexOf("Tick", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 m.Name.IndexOf("Grow", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 m.Name.IndexOf("Plant", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 m.Name.IndexOf("Place", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    LeezLog.Warning("Crop API candidate: " + DescribeDetailed(method));
                }
            }
            catch (Exception ex)
            {
                LeezLog.Warning("Could not dump crop candidates: " + ex.Message);
            }
        }

        private static string Describe(MethodInfo method)
        {
            if (method == null) return "<null>";
            return method.ReturnType.Name + " " +
                   (method.DeclaringType != null ? method.DeclaringType.Name : "<type>") +
                   "." + method.Name +
                   " [token 0x" + method.MetadataToken.ToString("X8") + "]";
        }

        private static string DescribeDetailed(MethodInfo method)
        {
            if (method == null) return "<null>";

            try
            {
                string parameters = string.Join(", ", method.GetParameters()
                    .Select(p => p.ParameterType.Name + " " + p.Name));
                return method.ReturnType.Name + " " +
                       (method.DeclaringType != null ? method.DeclaringType.Name : "<type>") +
                       "." + method.Name + "(" + parameters + ")" +
                       " [token 0x" + method.MetadataToken.ToString("X8") + "]";
            }
            catch
            {
                return Describe(method);
            }
        }
    }
}
