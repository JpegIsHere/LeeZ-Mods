using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LeezGrowLights
{
    /// <summary>
    /// V3.1 BlockPoweredLight only toggles LightLOD components found below direct
    /// children named "MainLight" or "Point light". A custom LeeZ prefab can contain
    /// a valid Unity Light outside those vanilla branches, which leaves the Light
    /// visually enabled even though the electrical tile entity is unpowered/off.
    ///
    /// This late postfix mirrors the already-validated powered+toggled state onto only
    /// those grow-light Light components that vanilla is not managing. Vanilla LightLOD
    /// branches are deliberately left untouched.
    /// </summary>
    internal static class GrowLightPowerVisualSync
    {
        public static void Install(Harmony harmony)
        {
            Type poweredLightType = AccessTools.TypeByName("BlockPoweredLight");
            if (poweredLightType == null)
            {
                LeezLog.Warning(
                    "BlockPoweredLight not found; custom grow-light visual power sync was not installed.");
                return;
            }

            MethodInfo updateLightState = AccessTools.Method(
                poweredLightType,
                "updateLightState",
                new[]
                {
                    typeof(WorldBase),
                    typeof(Vector3i),
                    typeof(BlockValue),
                    typeof(bool)
                });

            MethodInfo postfixMethod = AccessTools.Method(
                typeof(GrowLightPowerVisualSync),
                nameof(UpdateLightStatePostfix));

            if (updateLightState == null || postfixMethod == null)
            {
                LeezLog.Warning(
                    "BlockPoweredLight.updateLightState could not be resolved; custom grow-light visual power sync is unavailable.");
                return;
            }

            try
            {
                HarmonyMethod postfix = new HarmonyMethod(postfixMethod)
                {
                    priority = Priority.Last
                };

                harmony.Patch(updateLightState, postfix: postfix);
                LeezLog.Info(
                    "Grow-light custom visual power sync armed on BlockPoweredLight.updateLightState.");
            }
            catch (Exception ex)
            {
                LeezLog.Warning(
                    "Could not install custom grow-light visual power sync: " + ex.Message);
            }
        }

        public static void UpdateLightStatePostfix(object __instance, object[] __args)
        {
            Block block = __instance as Block;
            if (block == null ||
                !GrowLightScanner.TryGetGrowLightCoverage(block, out _, out _, out _))
            {
                return;
            }

            WorldBase world = FindFirst<WorldBase>(__args);
            if (world == null || !TryGetFirstVector3i(__args, out Vector3i position))
                return;

            BlockEntityData blockEntityData = ResolveBlockEntityData(world, position);
            Transform root = blockEntityData?.transform;
            if (root == null)
                return;

            bool active = PowerStateResolver.IsActive(world, position);

            try
            {
                Light[] lights = root.GetComponentsInChildren<Light>(true);
                foreach (Light light in lights)
                {
                    if (light == null || IsVanillaManagedLight(root, light))
                        continue;

                    light.enabled = active;
                }
            }
            catch (Exception ex)
            {
                LeezLog.Warning(
                    "Grow-light custom visual power refresh failed at " + position + ": " + ex.Message);
            }
        }

        private static BlockEntityData ResolveBlockEntityData(
            WorldBase world,
            Vector3i position)
        {
            try
            {
                if (world?.ChunkCache == null)
                    return null;

                IChunk chunk = world.ChunkCache.GetChunkFromWorldPos(position);
                return chunk?.GetBlockEntity(position);
            }
            catch (Exception ex)
            {
                LeezLog.Warning(
                    "Grow-light custom visual power sync could not resolve block entity at " +
                    position + ": " + ex.Message);
                return null;
            }
        }

        private static bool IsVanillaManagedLight(Transform root, Light light)
        {
            return IsInsideVanillaManagedBranch(root?.Find("MainLight"), light) ||
                   IsInsideVanillaManagedBranch(root?.Find("Point light"), light);
        }

        private static bool IsInsideVanillaManagedBranch(Transform branch, Light light)
        {
            if (branch == null || light == null || branch.GetComponent<LightLOD>() == null)
                return false;

            Transform lightTransform = light.transform;
            return lightTransform == branch || lightTransform.IsChildOf(branch);
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
