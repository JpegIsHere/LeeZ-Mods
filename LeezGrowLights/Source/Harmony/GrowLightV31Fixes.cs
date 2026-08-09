using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace LeezGrowLights
{
    /// <summary>
    /// V3.1 regression fixes layered on the validated v0.7.0-dev10 runtime.
    ///
    /// 1. PowerConsumerToggle.HandlePowerReceived subtracts RequiredPower even while
    ///    IsToggled is false. LeeZ grow lights therefore set the live consumer load to
    ///    0 W while switched off and restore the configured block wattage when switched on.
    ///
    /// 2. The colour/brightness radial commands added by the LeeZ runtime intentionally
    ///    tint iconColor but dev10 never supplied an icon name. This post-processes those
    ///    commands after the existing colour postfix and supplies built-in game icons.
    /// </summary>
    internal static class GrowLightV31Fixes
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const string ColourCommandIdPrefix = "growlightcolour_";
        private const string BrightnessCommandIdPrefix = "growlightbrightness_";
        private const string LegacyColourCommandPrefix = "Grow light colour:";
        private const string LegacyBrightnessCommandPrefix = "Grow light brightness:";

        private static readonly FieldInfo PowerItemTileEntityField =
            AccessTools.Field(typeof(PowerItem), "TileEntity");

        private static readonly FieldInfo PowerItemRequiredPowerField =
            AccessTools.Field(typeof(PowerItem), "RequiredPower");

        private static readonly MethodInfo SendChangesToRootMethod =
            AccessTools.Method(typeof(PowerItem), "SendHasLocalChangesToRoot");

        private static readonly MethodInfo MarkChangedMethod =
            AccessTools.Method(typeof(TileEntityPowered), "MarkChanged");

        private static readonly FieldInfo CommandIconField =
            AccessTools.Field(typeof(BlockActivationCommand), "icon");

        private static readonly PropertyInfo CommandIconProperty =
            AccessTools.Property(typeof(BlockActivationCommand), "icon");

        private static bool reportedPowerApiMissing;
        private static bool reportedIconApiMissing;
        private static bool loggedIconRepair;

        public static void Install(Harmony harmony)
        {
            InstallPowerDrawFix(harmony);
            InstallRadialIconFix(harmony);
        }

        private static void InstallPowerDrawFix(Harmony harmony)
        {
            MethodInfo postfix = AccessTools.Method(
                typeof(GrowLightV31Fixes), nameof(PowerConsumerPostfix));
            MethodInfo tilePostfix = AccessTools.Method(
                typeof(GrowLightV31Fixes), nameof(PowerTilePostfix));

            int installed = 0;

            MethodInfo toggleSetter =
                AccessTools.PropertySetter(typeof(PowerConsumerToggle), "IsToggled");
            installed += TryPatchPostfix(harmony, toggleSetter, postfix, Priority.Last);

            MethodInfo consumerRead = FindDeclaredMethod(
                typeof(PowerConsumerToggle), "read");
            installed += TryPatchPostfix(harmony, consumerRead, postfix, Priority.Last);

            MethodInfo valuesFromBlock = FindDeclaredOrInheritedMethod(
                typeof(PowerConsumerToggle), "SetValuesFromBlock");
            installed += TryPatchPostfix(harmony, valuesFromBlock, postfix, Priority.Last);

            MethodInfo initializePowerData = AccessTools.Method(
                typeof(TileEntityPowered), "InitializePowerData");
            installed += TryPatchPostfix(
                harmony, initializePowerData, tilePostfix, Priority.Last);

            if (installed == 0)
            {
                LeezLog.Warning(
                    "Grow-light zero-watt OFF-state fix could not install any V3.1 hooks.");
            }
            else
            {
                LeezLog.Info(
                    "Grow-light zero-watt OFF-state fix armed on " +
                    installed + " hook(s).");
            }
        }

        private static void InstallRadialIconFix(Harmony harmony)
        {
            Type poweredLightType = AccessTools.TypeByName("BlockPoweredLight");
            if (poweredLightType == null)
            {
                LeezLog.Warning(
                    "BlockPoweredLight was not found; LeeZ radial icon repair was not installed.");
                return;
            }

            MethodInfo postfix = AccessTools.Method(
                typeof(GrowLightV31Fixes), nameof(RadialCommandsPostfix));
            if (postfix == null)
                return;

            var seen = new HashSet<string>();
            int installed = 0;

            for (Type type = poweredLightType;
                 type != null && type != typeof(object);
                 type = type.BaseType)
            {
                MethodInfo[] methods;
                try
                {
                    methods = type
                        .GetMethods(InstanceFlags | BindingFlags.DeclaredOnly)
                        .Where(method =>
                            string.Equals(
                                method.Name,
                                "GetBlockActivationCommands",
                                StringComparison.Ordinal) &&
                            method.ReturnType == typeof(BlockActivationCommand[]))
                        .OrderBy(method => method.MetadataToken)
                        .ToArray();
                }
                catch (Exception ex)
                {
                    LeezLog.Warning(
                        "Could not inspect " + type.Name +
                        ".GetBlockActivationCommands for icon repair: " + ex.Message);
                    continue;
                }

                foreach (MethodInfo method in methods)
                {
                    string key = method.Module.ModuleVersionId + ":" + method.MetadataToken;
                    if (!seen.Add(key))
                        continue;

                    installed += TryPatchPostfix(
                        harmony, method, postfix, Priority.Last);
                }
            }

            if (installed == 0)
            {
                LeezLog.Warning(
                    "No powered-light radial command method was available for LeeZ icon repair.");
            }
            else
            {
                LeezLog.Info(
                    "Grow-light radial icon repair armed on " +
                    installed + " method(s).");
            }
        }

        public static void PowerConsumerPostfix(object __instance)
        {
            if (__instance is PowerConsumerToggle consumer)
                SynchronizeConsumerPower(consumer);
        }

        public static void PowerTilePostfix(TileEntityPowered __instance)
        {
            if (__instance == null)
                return;

            if (__instance.GetPowerItem() is PowerConsumerToggle consumer)
                SynchronizeConsumerPower(consumer);
        }

        private static void SynchronizeConsumerPower(PowerConsumerToggle consumer)
        {
            if (consumer == null)
                return;

            if (PowerItemTileEntityField == null || PowerItemRequiredPowerField == null)
            {
                ReportPowerApiMissingOnce();
                return;
            }

            TileEntityPowered tile;
            try
            {
                tile = PowerItemTileEntityField.GetValue(consumer) as TileEntityPowered;
            }
            catch
            {
                return;
            }

            if (tile == null)
                return;

            WorldBase world = ResolveServerWorld();
            if (world == null || world.IsRemote())
                return;

            Vector3i position;
            Block block;
            try
            {
                position = tile.ToWorldPos();
                block = world.GetBlock(position).Block;
            }
            catch
            {
                return;
            }

            if (!IsGrowLight(block))
                return;

            int configuredPower;
            try
            {
                configuredPower = tile.GetRequiredPower();
            }
            catch
            {
                configuredPower = 0;
            }

            configuredPower = Math.Max(0, Math.Min(ushort.MaxValue, configuredPower));
            int desiredPower = consumer.IsToggled ? configuredPower : 0;

            int currentPower;
            try
            {
                object current = PowerItemRequiredPowerField.GetValue(consumer);
                currentPower = current == null ? 0 : Convert.ToInt32(current);
            }
            catch
            {
                currentPower = -1;
            }

            if (currentPower == desiredPower)
                return;

            try
            {
                object converted = Convert.ChangeType(
                    desiredPower,
                    PowerItemRequiredPowerField.FieldType);
                PowerItemRequiredPowerField.SetValue(consumer, converted);

                SendChangesToRootMethod?.Invoke(consumer, null);
                MarkChangedMethod?.Invoke(tile, null);

                LeezLog.Info(
                    "Grow-light live electrical load at " + position +
                    " synchronized to " + desiredPower + " W (toggle=" +
                    (consumer.IsToggled ? "ON" : "OFF") + ").");
            }
            catch (Exception ex)
            {
                LeezLog.Warning(
                    "Could not synchronize LeeZ grow-light live power draw: " +
                    ex.GetBaseException().Message);
            }
        }

        public static void RadialCommandsPostfix(
            object __instance,
            ref BlockActivationCommand[] __result)
        {
            Block block = __instance as Block;
            if (!IsGrowLight(block) || __result == null)
                return;

            bool changed = false;

            for (int i = 0; i < __result.Length; i++)
            {
                string text = __result[i].text;
                if (IsColourCommand(text))
                {
                    changed |= TryEnsureIcon(
                        ref __result[i], "tool");
                }
                else if (IsBrightnessCommand(text))
                {
                    changed |= TryEnsureIcon(
                        ref __result[i], "wrench");
                }
            }

            if (changed && !loggedIconRepair)
            {
                loggedIconRepair = true;
                LeezLog.Info(
                    "Grow-light radial icons restored: colour=tool, brightness=wrench.");
            }
        }

        private static bool TryEnsureIcon(
            ref BlockActivationCommand command,
            string replacement)
        {
            if (CommandIconField == null && CommandIconProperty == null)
            {
                ReportIconApiMissingOnce();
                return false;
            }

            object boxed = command;
            string current = null;

            try
            {
                if (CommandIconField != null)
                    current = CommandIconField.GetValue(boxed) as string;
                else if (CommandIconProperty != null && CommandIconProperty.CanRead)
                    current = CommandIconProperty.GetValue(boxed, null) as string;

                if (!string.IsNullOrWhiteSpace(current))
                    return false;

                if (CommandIconField != null)
                {
                    CommandIconField.SetValue(boxed, replacement);
                }
                else if (CommandIconProperty != null && CommandIconProperty.CanWrite)
                {
                    CommandIconProperty.SetValue(boxed, replacement, null);
                }
                else
                {
                    ReportIconApiMissingOnce();
                    return false;
                }

                command = (BlockActivationCommand)boxed;
                return true;
            }
            catch (Exception ex)
            {
                if (!reportedIconApiMissing)
                {
                    reportedIconApiMissing = true;
                    LeezLog.Warning(
                        "Could not set LeeZ radial command icon: " + ex.Message);
                }
                return false;
            }
        }

        private static int TryPatchPostfix(
            Harmony harmony,
            MethodInfo target,
            MethodInfo postfix,
            int priority)
        {
            if (target == null || postfix == null)
                return 0;

            try
            {
                HarmonyMethod harmonyPostfix = new HarmonyMethod(postfix)
                {
                    priority = priority
                };

                harmony.Patch(target, postfix: harmonyPostfix);
                return 1;
            }
            catch (Exception ex)
            {
                LeezLog.Warning(
                    "Could not install LeeZ V3.1 fix on " +
                    target.DeclaringType?.Name + "." + target.Name +
                    ": " + ex.Message);
                return 0;
            }
        }

        private static MethodInfo FindDeclaredMethod(Type type, string name)
        {
            if (type == null)
                return null;

            try
            {
                return type
                    .GetMethods(InstanceFlags | BindingFlags.DeclaredOnly)
                    .Where(method =>
                        string.Equals(method.Name, name, StringComparison.Ordinal))
                    .OrderBy(method => method.MetadataToken)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static MethodInfo FindDeclaredOrInheritedMethod(Type type, string name)
        {
            if (type == null)
                return null;

            try
            {
                return type
                    .GetMethods(InstanceFlags)
                    .Where(method =>
                        string.Equals(method.Name, name, StringComparison.Ordinal))
                    .OrderBy(method => InheritanceDistance(type, method.DeclaringType))
                    .ThenBy(method => method.MetadataToken)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static int InheritanceDistance(Type child, Type possibleBase)
        {
            int distance = 0;
            for (Type current = child;
                 current != null;
                 current = current.BaseType, distance++)
            {
                if (current == possibleBase)
                    return distance;
            }

            return int.MaxValue;
        }

        private static bool IsGrowLight(Block block)
        {
            return block != null &&
                   GrowLightScanner.TryGetGrowLightCoverage(
                       block, out _, out _, out _);
        }

        private static bool IsColourCommand(string text)
        {
            return !string.IsNullOrWhiteSpace(text) &&
                   (text.StartsWith(
                        ColourCommandIdPrefix,
                        StringComparison.OrdinalIgnoreCase) ||
                    text.StartsWith(
                        LegacyColourCommandPrefix,
                        StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsBrightnessCommand(string text)
        {
            return !string.IsNullOrWhiteSpace(text) &&
                   (text.StartsWith(
                        BrightnessCommandIdPrefix,
                        StringComparison.OrdinalIgnoreCase) ||
                    text.StartsWith(
                        LegacyBrightnessCommandPrefix,
                        StringComparison.OrdinalIgnoreCase));
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

        private static void ReportPowerApiMissingOnce()
        {
            if (reportedPowerApiMissing)
                return;

            reportedPowerApiMissing = true;
            LeezLog.Warning(
                "LeeZ grow-light power fix could not resolve PowerItem.TileEntity/RequiredPower.");
        }

        private static void ReportIconApiMissingOnce()
        {
            if (reportedIconApiMissing)
                return;

            reportedIconApiMissing = true;
            LeezLog.Warning(
                "LeeZ grow-light icon fix could not resolve BlockActivationCommand.icon.");
        }
    }
}
