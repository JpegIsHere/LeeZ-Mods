using System;
using System.Reflection;
using HarmonyLib;

namespace LeezGrowLights
{
    /// <summary>
    /// Keeps the electrical consumer load aligned with the grow-light toggle state.
    ///
    /// V3.1 keeps a PowerConsumerToggle in the electrical graph even while its visual
    /// light is switched off. The vanilla consumer retains the block's RequiredPower,
    /// so a directly-wired light can continue reserving/drawing its configured watts.
    ///
    /// LeeZ grow lights keep their configured block/tile wattage unchanged. Only the
    /// PowerConsumerToggle.RequiredPower value used by the live power graph is changed:
    /// 0 W while toggled off, configured watts while toggled on.
    /// </summary>
    internal static class GrowLightPowerPatches
    {
        private static readonly FieldInfo ConsumerRequiredPowerField =
            AccessTools.Field(typeof(PowerConsumerToggle), "RequiredPower");

        private static readonly FieldInfo ConsumerTileEntityField =
            AccessTools.Field(typeof(PowerConsumerToggle), "TileEntity");

        private static readonly MethodInfo SendChangesToRootMethod =
            AccessTools.Method(typeof(PowerConsumerToggle), "SendHasLocalChangesToRoot");

        private static readonly MethodInfo MarkChangedMethod =
            AccessTools.Method(typeof(TileEntityPowered), "MarkChanged");

        private static bool reportedMissingApi;

        public static void Install(Harmony harmony)
        {
            int installed = 0;

            MethodInfo tileToggleSetter =
                AccessTools.PropertySetter(typeof(TileEntityPoweredBlock), "IsToggled");
            if (tileToggleSetter != null)
            {
                harmony.Patch(
                    tileToggleSetter,
                    postfix: new HarmonyMethod(
                        typeof(GrowLightPowerPatches), nameof(TileTogglePostfix)));
                installed++;
            }

            MethodInfo consumerToggleSetter =
                AccessTools.PropertySetter(typeof(PowerConsumerToggle), "IsToggled");
            if (consumerToggleSetter != null)
            {
                harmony.Patch(
                    consumerToggleSetter,
                    postfix: new HarmonyMethod(
                        typeof(GrowLightPowerPatches), nameof(ConsumerTogglePostfix)));
                installed++;
            }

            MethodInfo initializePowerData =
                AccessTools.Method(typeof(TileEntityPowered), "InitializePowerData");
            if (initializePowerData != null)
            {
                harmony.Patch(
                    initializePowerData,
                    postfix: new HarmonyMethod(
                        typeof(GrowLightPowerPatches), nameof(InitializePowerDataPostfix)));
                installed++;
            }

            MethodInfo onReadComplete =
                AccessTools.Method(typeof(TileEntity), "OnReadComplete");
            if (onReadComplete != null)
            {
                harmony.Patch(
                    onReadComplete,
                    postfix: new HarmonyMethod(
                        typeof(GrowLightPowerPatches), nameof(OnReadCompletePostfix)));
                installed++;
            }

            if (installed == 0)
                LeezLog.Warning("Grow-light power-draw hooks were not found in the V3.1 runtime.");
            else
                LeezLog.Info("Grow-light power-draw fix installed on " + installed + " hook(s).");
        }

        public static void TileTogglePostfix(TileEntityPoweredBlock __instance)
        {
            if (__instance == null) return;
            Synchronize(__instance, __instance.IsToggled);
        }

        public static void ConsumerTogglePostfix(PowerConsumerToggle __instance)
        {
            if (__instance == null || ConsumerTileEntityField == null) return;

            TileEntityPoweredBlock tile =
                ConsumerTileEntityField.GetValue(__instance) as TileEntityPoweredBlock;

            if (tile == null) return;
            Synchronize(tile, __instance.IsToggled, __instance);
        }

        public static void InitializePowerDataPostfix(TileEntityPowered __instance)
        {
            if (__instance is TileEntityPoweredBlock tile)
                Synchronize(tile, tile.IsToggled);
        }

        public static void OnReadCompletePostfix(TileEntity __instance)
        {
            if (__instance is TileEntityPoweredBlock tile)
                Synchronize(tile, tile.IsToggled);
        }

        internal static bool IsGrowLight(Block block)
        {
            return block?.Properties?.Values != null &&
                   block.Properties.Values.ContainsKey("LeezGrowTier");
        }

        private static void Synchronize(
            TileEntityPoweredBlock tile,
            bool toggled,
            PowerConsumerToggle knownConsumer = null)
        {
            if (!IsGrowLight(tile.block)) return;

            PowerConsumerToggle consumer = knownConsumer ?? tile.GetPowerItem() as PowerConsumerToggle;
            if (consumer == null) return;

            if (ConsumerRequiredPowerField == null)
            {
                ReportMissingApiOnce("PowerConsumerToggle.RequiredPower was not found.");
                return;
            }

            int configuredPower = tile.GetRequiredPower();
            if (configuredPower < 0) configuredPower = 0;
            if (configuredPower > ushort.MaxValue) configuredPower = ushort.MaxValue;

            ushort desiredPower = toggled ? (ushort)configuredPower : (ushort)0;
            object currentValue = ConsumerRequiredPowerField.GetValue(consumer);
            ushort currentPower = currentValue == null
                ? (ushort)0
                : Convert.ToUInt16(currentValue);

            if (currentPower == desiredPower) return;

            ConsumerRequiredPowerField.SetValue(consumer, desiredPower);

            try
            {
                SendChangesToRootMethod?.Invoke(consumer, null);
                MarkChangedMethod?.Invoke(tile, null);
            }
            catch (Exception ex)
            {
                LeezLog.Warning(
                    "Grow-light power state changed but the electrical graph refresh reported: " +
                    ex.GetBaseException().Message);
            }

            LeezLog.Info(
                "Grow-light electrical load synchronized at " + tile.ToWorldPos() +
                ": toggled=" + (toggled ? "ON" : "OFF") +
                ", draw=" + desiredPower + "W");
        }

        private static void ReportMissingApiOnce(string message)
        {
            if (reportedMissingApi) return;
            reportedMissingApi = true;
            LeezLog.Warning("Grow-light power-draw fix unavailable: " + message);
        }
    }
}
