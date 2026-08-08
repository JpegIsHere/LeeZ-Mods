using System;
using System.Collections.Generic;
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
            public bool HasApplied;
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<Vector3i, WeakReference> LiveBlockEntities =
            new Dictionary<Vector3i, WeakReference>();
        private static readonly ConditionalWeakTable<Light, LightIntensityState> LightIntensityStates =
            new ConditionalWeakTable<Light, LightIntensityState>();

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
                " / " + GrowLightBrightnessPalette.ToDisplayName(
                    GrowLightColourState.GetBrightness(blockValue)) + ".");
            return true;
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
            GrowLightBrightness brightness = GrowLightColourState.GetBrightness(blockValue);
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
                    ApplyBrightness(light, brightness);
                }
            }
            catch (Exception ex)
            {
                LeezLog.Warning("Grow-light light-component visual update failed: " + ex.Message);
            }
        }

        private static void ApplyBrightness(
            Light light,
            GrowLightBrightness brightness)
        {
            LightIntensityState state = LightIntensityStates.GetValue(
                light,
                CreateLightIntensityState);

            float currentIntensity = light.intensity;
            if (!state.HasApplied ||
                !Mathf.Approximately(currentIntensity, state.LastAppliedIntensity))
            {
                // Vanilla may change the light intensity when power/toggle state changes.
                // Treat that post-vanilla value as the new baseline so cosmetic brightness
                // remains a multiplier rather than replacing powered-light behaviour.
                state.BaseIntensity = currentIntensity;
            }

            float multiplier = GrowLightBrightnessPalette.ToIntensityMultiplier(brightness);
            float targetIntensity = state.BaseIntensity * multiplier;
            light.intensity = targetIntensity;
            state.LastAppliedIntensity = targetIntensity;
            state.HasApplied = true;
        }

        private static LightIntensityState CreateLightIntensityState(Light light)
        {
            float intensity = light != null ? light.intensity : 0f;
            return new LightIntensityState
            {
                BaseIntensity = intensity,
                LastAppliedIntensity = intensity,
                HasApplied = false
            };
        }
    }
}
