using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace LeezGrowLights
{
    /// <summary>
    /// Makes an active LeeZ grow light act as the sunlight source for vanilla crops without
    /// globally disabling vanilla light rules.
    ///
    /// The V3.1 API exposes BlockPlantGrowing.lightLevelGrow and lightLevelStay. While vanilla is
    /// validating a covered crop we temporarily set only that BlockPlantGrowing instance's light
    /// thresholds to zero, allow vanilla to perform all of its normal substrate/space/plant rules,
    /// then restore the original values immediately.
    /// </summary>
    internal static class SunlightSubstitutionPatches
    {
        private static readonly FieldInfo LightLevelGrowField =
            AccessTools.Field(typeof(BlockPlantGrowing), "lightLevelGrow");

        private static readonly FieldInfo LightLevelStayField =
            AccessTools.Field(typeof(BlockPlantGrowing), "lightLevelStay");

        private static readonly HashSet<string> LoggedMethods = new HashSet<string>();

        internal sealed class SunlightState
        {
            public BlockPlantGrowing PlantBlock;
            public int OriginalGrow;
            public int OriginalStay;
            public bool LockTaken;
            public bool Applied;
            public bool Restored;
        }

        public static void Prefix(object __instance, MethodBase __originalMethod, object[] __args, out SunlightState __state)
        {
            __state = null;

            BlockPlantGrowing plantBlock = __instance as BlockPlantGrowing;
            if (plantBlock == null || LightLevelGrowField == null || LightLevelStayField == null)
                return;

            if (!TryFindCoveredPosition(__args, out WorldBase world, out Vector3i coveredPos))
                return;

            var state = new SunlightState { PlantBlock = plantBlock };
            __state = state;

            try
            {
                Monitor.Enter(plantBlock, ref state.LockTaken);

                state.OriginalGrow = Convert.ToInt32(LightLevelGrowField.GetValue(plantBlock));
                state.OriginalStay = Convert.ToInt32(LightLevelStayField.GetValue(plantBlock));

                LightLevelGrowField.SetValue(plantBlock, 0);
                LightLevelStayField.SetValue(plantBlock, 0);
                state.Applied = true;

                LogAppliedOnce(__originalMethod, coveredPos, world.IsRemote());
            }
            catch (Exception ex)
            {
                Restore(state);
                LeezLog.Warning("Unable to apply grow-light sunlight substitution: " + ex.Message);
            }
        }

        public static void Postfix(SunlightState __state)
        {
            Restore(__state);
        }

        public static Exception Finalizer(Exception __exception, SunlightState __state)
        {
            Restore(__state);
            return __exception;
        }

        private static void Restore(SunlightState state)
        {
            if (state == null || state.Restored)
                return;

            try
            {
                if (state.Applied && state.PlantBlock != null)
                {
                    LightLevelGrowField.SetValue(state.PlantBlock, state.OriginalGrow);
                    LightLevelStayField.SetValue(state.PlantBlock, state.OriginalStay);
                }
            }
            catch (Exception ex)
            {
                LeezLog.Error("Failed restoring vanilla crop light thresholds: " + ex.Message);
            }
            finally
            {
                state.Restored = true;
                if (state.LockTaken && state.PlantBlock != null)
                    Monitor.Exit(state.PlantBlock);
            }
        }

        private static bool TryFindCoveredPosition(object[] args, out WorldBase world, out Vector3i coveredPos)
        {
            world = null;
            coveredPos = Vector3i.zero;

            if (args == null)
                return false;

            foreach (object arg in args)
            {
                if (world == null && arg is WorldBase worldArg)
                    world = worldArg;
            }

            if (world == null)
                return false;

            foreach (object arg in args)
            {
                if (arg is Vector3i posArg &&
                    GrowLightScanner.HasActiveSunlightReplacement(world, posArg))
                {
                    coveredPos = posArg;
                    return true;
                }
            }

            return false;
        }

        private static void LogAppliedOnce(MethodBase method, Vector3i pos, bool isRemote)
        {
            if (method == null)
                return;

            string side = isRemote ? "client" : "server";
            string key = method.DeclaringType?.FullName + "." + method.Name + ":" + side;

            lock (LoggedMethods)
            {
                if (!LoggedMethods.Add(key))
                    return;
            }

            LeezLog.Info(
                "Grow-light sunlight substitution active in " +
                (method.DeclaringType?.Name ?? "<type>") + "." + method.Name +
                " (" + side + ", covered position " + pos + ").");
        }
    }
}
