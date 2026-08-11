using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LeezGrowLights
{
    internal static class GrowLightColourVisual
    {
        private sealed class LightIntensityState
        {
            public float BaseIntensity;
            public float LastAppliedIntensity;
            public float Multiplier;
            public bool HasApplied;
        }

        private sealed class LightReference
        {
            public Light Light;
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<Vector3i, WeakReference> LiveBlockEntities =
            new Dictionary<Vector3i, WeakReference>();
        private static readonly ConditionalWeakTable<Light, LightIntensityState> LightIntensityStates =
            new ConditionalWeakTable<Light, LightIntensityState>();
        private static readonly ConditionalWeakTable<object, LightReference> LightLodLights =
            new ConditionalWeakTable<object, LightReference>();

        public static void Apply(BlockEntityData blockEntityData, BlockValue blockValue)
        {
            ApplyInternal(blockEntityData, blockValue);
        }

        public static void ApplyAt(
            Vector3i position,
            BlockEntityData blockEntityData,
            BlockValue blockValue)
        {
            if (blockEntityData == null)
                return;

            lock (Sync)
            {
                LiveBlockEntities[position] = new WeakReference(blockEntityData);
            }

            ApplyInternal(blockEntityData, blockValue);
        }

        public static bool TryApplyCached(Vector3i position, BlockValue blockValue)
        {
            BlockEntityData blockEntityData = null;
            lock (Sync)
            {
                if (LiveBlockEntities.TryGetValue(position, out WeakReference reference))
                {
                    blockEntityData = reference.Target as BlockEntityData;
                    if (blockEntityData == null)
                        LiveBlockEntities.Remove(position);
                }
            }

            if (blockEntityData == null)
            {
                LeezLog.Warning(
                    "Grow-light live visual refresh has no cached block entity at " + position + ".");
                return false;
            }

            ApplyInternal(blockEntityData, blockValue);
            LeezLog.Info(
                "Grow-light live visual refresh applied at " + position +
                " as " + GrowLightColourState.Get(blockValue) +
                " / T" + GetGrowLightTier(blockValue.Block) + " fixed brightness.");
            return true;
        }

        /// <summary>
        /// V3.1 LightLOD.FrameUpdate recalculates and writes Unity Light.intensity every
        /// frame. The normal grow-light visual callback therefore cannot own intensity
        /// with a one-shot write: vanilla immediately restores its LOD-controlled value.
        ///
        /// This Harmony postfix runs after that vanilla writer. Only Light components
        /// previously seen by the LeeZ visual path have a LightIntensityState, so all
        /// unrelated vanilla/mod lights pass through untouched. If vanilla did not write
        /// a new value this frame, current intensity still equals LastAppliedIntensity and
        /// we skip it, preventing multiplier compounding.
        /// </summary>
        public static void LightLodFramePostfix(object __instance)
        {
            if (__instance == null)
                return;

            Light light = ResolveLightLodLight(__instance);
            if (light == null ||
                !LightIntensityStates.TryGetValue(light, out LightIntensityState state) ||
                !state.HasApplied)
            {
                return;
            }

            float currentIntensity = light.intensity;
            if (Mathf.Approximately(currentIntensity, state.LastAppliedIntensity))
                return;

            // FrameUpdate has just supplied the current vanilla LOD/power value. Keep it
            // as the fresh baseline and layer the fixed tier multiplier on top.
            state.BaseIntensity = currentIntensity;
            float targetIntensity = currentIntensity * state.Multiplier;
            light.intensity = targetIntensity;
            state.LastAppliedIntensity = targetIntensity;
        }

        private static void ApplyInternal(
            BlockEntityData blockEntityData,
            BlockValue blockValue)
        {
            if (blockEntityData == null)
                return;

            Block block = blockValue.Block;
            if (block == null ||
                !GrowLightScanner.TryGetGrowLightCoverage(block, out _, out _, out _))
            {
                return;
            }

            GrowLightColour selected = GrowLightColourState.Get(blockValue);
            float tierBrightnessMultiplier = GetTierIntensityMultiplier(block);
            Color colour = GrowLightColourPalette.ToUnityColour(selected);

            try
            {
                blockEntityData.SetMaterialColor(colour);
            }
            catch (Exception ex)
            {
                LeezLog.Warning("Grow-light material tint failed: " + ex.Message);
            }

            try
            {
                Transform transform = blockEntityData.transform;
                if (transform == null)
                    return;

                Light[] lights = transform.GetComponentsInChildren<Light>(true);
                foreach (Light light in lights)
                {
                    if (light == null)
                        continue;

                    light.color = colour;
                    ApplyBrightness(light, tierBrightnessMultiplier);
                }
            }
            catch (Exception ex)
            {
                LeezLog.Warning("Grow-light light-component visual update failed: " + ex.Message);
            }
        }

        private static void ApplyBrightness(
            Light light,
            float tierBrightnessMultiplier)
        {
            LightIntensityState state = LightIntensityStates.GetValue(
                light,
                CreateLightIntensityState);

            float currentIntensity = light.intensity;
            if (!state.HasApplied ||
                !Mathf.Approximately(currentIntensity, state.LastAppliedIntensity))
            {
                // Vanilla may change the light intensity when power/toggle/LOD state changes.
                // Treat that post-vanilla value as the new baseline so tier brightness remains
                // a multiplier rather than replacing powered-light behaviour.
                state.BaseIntensity = currentIntensity;
            }

            state.Multiplier = tierBrightnessMultiplier;
            float targetIntensity = state.BaseIntensity * state.Multiplier;
            light.intensity = targetIntensity;
            state.LastAppliedIntensity = targetIntensity;
            state.HasApplied = true;
        }

        private static int GetGrowLightTier(Block block)
        {
            if (block?.Properties?.Values != null &&
                block.Properties.Values.TryGetValue("LeezGrowTier", out string tierText) &&
                int.TryParse(tierText, out int tier))
            {
                return tier;
            }

            // All LeeZ grow lights define LeezGrowTier. Fall back to Normal/T2 intensity
            // if a malformed third-party block reaches this path.
            return 2;
        }

        private static float GetTierIntensityMultiplier(Block block)
        {
            switch (GetGrowLightTier(block))
            {
                case 1:
                    return 1.00f; // Temporary test: T1
                case 2:
                    return 2.00f; // Temporary test: T2
                case 3:
                    return 4.00f; // Temporary test: T3
                case 4:
                    return 6.00f; // Temporary test: T4
                case 5:
                    return 8.00f; // Temporary test: T5
                case 6:
                    return 10.00f; // Temporary test: T6
                default:
                    return 1.00f;
            }
        }

        private static Light ResolveLightLodLight(object lightLodInstance)
        {
            LightReference reference = LightLodLights.GetValue(
                lightLodInstance,
                CreateLightReference);
            return reference.Light;
        }

        private static LightReference CreateLightReference(object lightLodInstance)
        {
            Light light = null;

            try
            {
                Type type = lightLodInstance?.GetType();
                FieldInfo field = type?.GetField(
                    "myLight",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                    light = field.GetValue(lightLodInstance) as Light;
            }
            catch
            {
                // Fall through to the component lookup below.
            }

            if (light == null && lightLodInstance is Component component)
            {
                try
                {
                    light = component.GetComponent<Light>();
                }
                catch
                {
                    // Leave the cached reference empty; this LightLOD is not one we can own.
                }
            }

            return new LightReference { Light = light };
        }

        private static LightIntensityState CreateLightIntensityState(Light light)
        {
            float intensity = light != null ? light.intensity : 0f;
            return new LightIntensityState
            {
                BaseIntensity = intensity,
                LastAppliedIntensity = intensity,
                Multiplier = 1f,
                HasApplied = false
            };
        }
    }
}
