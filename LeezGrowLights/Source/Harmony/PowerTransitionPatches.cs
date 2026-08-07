using System.Reflection;
using HarmonyLib;

namespace LeezGrowLights
{
    internal static class PowerTransitionPatches
    {
        private static readonly FieldInfo PowerItemTileEntityField =
            AccessTools.Field(typeof(PowerItem), "TileEntity");

        public static void Prefix(
            object __instance,
            ref GrowLightTransitionRescheduler.TransitionState __state)
        {
            __state = Capture(__instance);
        }

        public static void Postfix(GrowLightTransitionRescheduler.TransitionState __state)
        {
            GrowLightTransitionRescheduler.Apply(__state);
        }

        private static GrowLightTransitionRescheduler.TransitionState Capture(object instance)
        {
            if (!(instance is PowerConsumerToggle))
                return null;

            WorldBase world = ResolveServerWorld();
            if (world == null || world.IsRemote())
                return null;

            TileEntityPowered tileEntity = ResolveTileEntity(instance);
            if (tileEntity == null)
                return null;

            Vector3i lampPos = tileEntity.ToWorldPos();
            Block lampBlock = world.GetBlock(lampPos).Block;

            return GrowLightTransitionRescheduler.Capture(
                world,
                lampPos,
                lampBlock);
        }

        private static TileEntityPowered ResolveTileEntity(object instance)
        {
            if (PowerItemTileEntityField == null)
                return null;

            try
            {
                return PowerItemTileEntityField.GetValue(instance) as TileEntityPowered;
            }
            catch
            {
                return null;
            }
        }

        private static WorldBase ResolveServerWorld()
        {
            try
            {
                if (GameManager.Instance == null)
                    return null;

                return GameManager.Instance.World;
            }
            catch
            {
                return null;
            }
        }
    }
}
