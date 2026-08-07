using System;
using UnityEngine;

namespace LeezGrowLights
{
    internal static class GrowLightColourVisual
    {
        public static void Apply(BlockEntityData blockEntityData, BlockValue blockValue)
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
