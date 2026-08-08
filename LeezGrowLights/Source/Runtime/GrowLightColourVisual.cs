using System;
using System.Collections.Generic;
using UnityEngine;

namespace LeezGrowLights
{
    internal static class GrowLightColourVisual
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<Vector3i, WeakReference> LiveBlockEntities =
            new Dictionary<Vector3i, WeakReference>();

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
                    "Grow-light live colour refresh has no cached block entity at " + position + ".");
                return false;
            }

            ApplyInternal(blockEntityData, blockValue);
            LeezLog.Info(
                "Grow-light live colour refresh applied at " + position +
                " as " + GrowLightColourState.Get(blockValue) + ".");
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
                    if (light != null)
                        light.color = colour;
                }
            }
            catch (Exception ex)
            {
                LeezLog.Warning("Grow-light light-component tint failed: " + ex.Message);
            }
        }
    }
}
