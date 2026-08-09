using System;
using System.Reflection;

namespace LeezGrowLights
{
    internal static class GrowLightColourPatches
    {
        private const string ColourCommandIdPrefix = "growlightcolour_";
        private const string BrightnessCommandIdPrefix = "growlightbrightness_";
        private const string LegacyColourCommandPrefix = "Grow light colour:";
        private const string LegacyBrightnessCommandPrefix = "Grow light brightness:";

        public static void ActivationCommandsPostfix(
            object __instance,
            object[] __args,
            ref BlockActivationCommand[] __result)
        {
            Block block = __instance as Block;
            if (!IsGrowLight(block))
                return;

            BlockValue value = FindBlockValue(__args, out bool foundValue)
                ? FindLastBlockValue(__args)
                : default(BlockValue);

            GrowLightColour selected = foundValue
                ? GrowLightColourState.Get(value)
                : GrowLightColourPalette.Default;
            GrowLightColour offeredColour = GrowLightColourPalette.Next(selected);
            GrowLightBrightness brightness = foundValue
                ? GrowLightColourState.GetBrightness(value)
                : GrowLightBrightnessPalette.Default;
            GrowLightBrightness offeredBrightness = GrowLightBrightnessPalette.Next(brightness);

            bool hasColour = TryFindExistingColourCommand(__result, out int colourIndex);
            bool hasBrightness = TryFindExistingBrightnessCommand(__result, out int brightnessIndex);

            if (hasColour)
            {
                BlockActivationCommand existing = __result[colourIndex];
                existing.text = BuildColourCommandId(offeredColour);
                existing.iconColor = GrowLightColourPalette.ToUnityColour(offeredColour);
                __result[colourIndex] = EnsureEnabled(existing);
            }

            if (hasBrightness)
            {
                BlockActivationCommand existing = __result[brightnessIndex];
                existing.text = BuildBrightnessCommandId(offeredBrightness);
                existing.iconColor = GrowLightColourPalette.ToUnityColour(selected);
                __result[brightnessIndex] = EnsureEnabled(existing);
            }

            int additions = (hasColour ? 0 : 1) + (hasBrightness ? 0 : 1);
            if (additions == 0)
                return;

            int originalLength = __result != null ? __result.Length : 0;
            BlockActivationCommand[] expanded =
                new BlockActivationCommand[originalLength + additions];

            if (__result != null && __result.Length > 0)
                Array.Copy(__result, expanded, __result.Length);

            int insertIndex = originalLength;

            if (!hasColour)
            {
                // The activation handler cycles from the current colour to the next colour,
                // so advertise that next colour in the radial menu. This keeps the command
                // label/icon aligned with the colour the click will actually apply.
                BlockActivationCommand colourCommand = new BlockActivationCommand
                {
                    text = BuildColourCommandId(offeredColour),
                    iconColor = GrowLightColourPalette.ToUnityColour(offeredColour),
                    activateTime = 0f,
                    highlighted = false
                };

                colourCommand = EnsureEnabled(colourCommand);
                expanded[insertIndex] = colourCommand;

                LeezLog.Info(
                    "Grow-light colour command exposed at index " + insertIndex +
                    " as token '" + colourCommand.text + "' (" + offeredColour + ").");
                insertIndex++;
            }

            if (!hasBrightness)
            {
                // Like colour, the activation handler advances from the current brightness
                // to the next brightness. Advertise the level the click will actually apply.
                BlockActivationCommand brightnessCommand = new BlockActivationCommand
                {
                    text = BuildBrightnessCommandId(offeredBrightness),
                    iconColor = GrowLightColourPalette.ToUnityColour(selected),
                    activateTime = 0f,
                    highlighted = false
                };

                brightnessCommand = EnsureEnabled(brightnessCommand);
                expanded[insertIndex] = brightnessCommand;

                LeezLog.Info(
                    "Grow-light brightness command exposed at index " + insertIndex +
                    " as token '" + brightnessCommand.text + "' (" +
                    GrowLightBrightnessPalette.ToDisplayName(offeredBrightness) + ").");
            }

            __result = expanded;
        }

        public static bool ActivatedPrefix(
            object __instance,
            MethodBase __originalMethod,
            object[] __args,
            ref bool __result)
        {
            Block block = __instance as Block;
            if (!IsGrowLight(block))
                return true;

            if (!TryGetCommandName(__originalMethod, __args, out string commandName))
                return true;

            // V3.1 BlockPoweredLight.OnBlockActivated identifies radial commands by the
            // string _commandName. dev8 uses stable growlightcolour_<colour> and
            // growlightbrightness_<level> tokens; legacy display-text prefixes remain
            // accepted for compatibility with already-open dev7 radial menus.
            bool isColourCommand = IsColourCommandName(commandName);
            bool isBrightnessCommand = IsBrightnessCommandName(commandName);
            if (!isColourCommand && !isBrightnessCommand)
                return true;

            string action = isColourCommand ? "colour" : "brightness";
            LeezLog.Info(
                "Grow-light " + action + " activation recognized: command='" + commandName + "'.");

            WorldBase world = FindFirst<WorldBase>(__args);
            if (world == null || !TryGetFirstVector3i(__args, out Vector3i position))
            {
                LeezLog.Warning(
                    "Grow-light " + action + " command could not resolve world/position.");
                __result = false;
                return false;
            }

            BlockValue value = FindBlockValue(__args, out bool foundValue)
                ? FindLastBlockValue(__args)
                : world.GetBlock(position);

            if (!foundValue && value.Block == null)
            {
                LeezLog.Warning(
                    "Grow-light " + action + " command resolved an empty block value at " +
                    position + ".");
                __result = false;
                return false;
            }

            if (world.IsRemote())
            {
                LeezLog.Warning(
                    "Remote grow-light " + action +
                    " request ignored until server command routing is enabled.");
                __result = false;
                return false;
            }

            bool changed;
            BlockValue updatedValue;

            if (isColourCommand)
            {
                GrowLightColour current = GrowLightColourState.Get(value);
                GrowLightColour next = GrowLightColourPalette.Next(current);
                changed = GrowLightColourState.TrySet(world, position, value, next);
                updatedValue = GrowLightColourState.WithColour(value, next);

                if (changed)
                {
                    GrowLightColourVisual.TryApplyCached(position, updatedValue);
                    LeezLog.Info(
                        "Grow-light colour at " + position +
                        " changed " + current + " -> " + next + ".");
                }
                else
                {
                    LeezLog.Warning(
                        "Grow-light colour state update returned false at " + position +
                        " for " + current + " -> " + next + ".");
                }
            }
            else
            {
                GrowLightBrightness current = GrowLightColourState.GetBrightness(value);
                GrowLightBrightness next = GrowLightBrightnessPalette.Next(current);
                changed = GrowLightColourState.TrySetBrightness(world, position, value, next);
                updatedValue = GrowLightColourState.WithBrightness(value, next);

                if (changed)
                {
                    GrowLightColourVisual.TryApplyCached(position, updatedValue);
                    LeezLog.Info(
                        "Grow-light brightness at " + position +
                        " changed " + GrowLightBrightnessPalette.ToDisplayName(current) +
                        " -> " + GrowLightBrightnessPalette.ToDisplayName(next) + ".");
                }
                else
                {
                    LeezLog.Warning(
                        "Grow-light brightness state update returned false at " + position +
                        " for " + GrowLightBrightnessPalette.ToDisplayName(current) +
                        " -> " + GrowLightBrightnessPalette.ToDisplayName(next) + ".");
                }
            }

            __result = changed;
            return false;
        }

        public static void VisualPostfix(object __instance, object[] __args)
        {
            Block block = __instance as Block;
            if (!IsGrowLight(block))
                return;

            BlockEntityData blockEntityData = FindFirst<BlockEntityData>(__args);
            if (blockEntityData == null)
                return;

            BlockValue value = FindBlockValue(__args, out bool foundValue)
                ? FindLastBlockValue(__args)
                : blockEntityData.blockValue;

            if (!foundValue && value.Block == null)
                return;

            if (TryGetFirstVector3i(__args, out Vector3i position))
            {
                GrowLightColourVisual.ApplyAt(position, blockEntityData, value);
            }
            else
            {
                GrowLightColourVisual.Apply(blockEntityData, value);
                LeezLog.Warning(
                    "Grow-light visual hook applied colour/brightness but could not cache block position for live refresh.");
            }
        }

        private static string BuildColourCommandId(GrowLightColour colour)
        {
            return ColourCommandIdPrefix + colour.ToString().ToLowerInvariant();
        }

        private static string BuildBrightnessCommandId(GrowLightBrightness brightness)
        {
            return BrightnessCommandIdPrefix + brightness.ToString().ToLowerInvariant();
        }

        private static bool TryGetCommandName(
            MethodBase originalMethod,
            object[] args,
            out string commandName)
        {
            commandName = null;
            if (args == null)
                return false;

            ParameterInfo[] parameters = SafeParameters(originalMethod);
            int count = Math.Min(parameters.Length, args.Length);

            for (int i = 0; i < count; i++)
            {
                if (!(args[i] is string text))
                    continue;

                string parameterName = parameters[i].Name ?? string.Empty;
                if (parameterName.IndexOf("command", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    commandName = text;
                    return true;
                }
            }

            foreach (object arg in args)
            {
                if (arg is string text)
                {
                    commandName = text;
                    return true;
                }
            }

            return false;
        }

        private static bool IsColourCommandName(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                return false;

            return commandName.StartsWith(
                       ColourCommandIdPrefix,
                       StringComparison.OrdinalIgnoreCase) ||
                   commandName.StartsWith(
                       LegacyColourCommandPrefix,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBrightnessCommandName(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                return false;

            return commandName.StartsWith(
                       BrightnessCommandIdPrefix,
                       StringComparison.OrdinalIgnoreCase) ||
                   commandName.StartsWith(
                       LegacyBrightnessCommandPrefix,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static ParameterInfo[] SafeParameters(MethodBase method)
        {
            if (method == null)
                return new ParameterInfo[0];

            try
            {
                return method.GetParameters();
            }
            catch
            {
                return new ParameterInfo[0];
            }
        }

        private static BlockActivationCommand EnsureEnabled(BlockActivationCommand command)
        {
            object boxed = command;
            Type type = boxed.GetType();
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

            foreach (string memberName in new[] { "enabled", "isEnabled" })
            {
                try
                {
                    FieldInfo field = type.GetField(memberName, flags);
                    if (field != null && field.FieldType == typeof(bool))
                    {
                        field.SetValue(boxed, true);
                        return (BlockActivationCommand)boxed;
                    }

                    PropertyInfo property = type.GetProperty(memberName, flags);
                    if (property != null &&
                        property.PropertyType == typeof(bool) &&
                        property.CanWrite)
                    {
                        property.SetValue(boxed, true, null);
                        return (BlockActivationCommand)boxed;
                    }
                }
                catch (Exception ex)
                {
                    LeezLog.Warning(
                        "Could not enable grow-light visual command through member '" +
                        memberName + "': " + ex.Message);
                }
            }

            LeezLog.Warning(
                "BlockActivationCommand exposes no writable enabled/isEnabled member; " +
                "grow-light visual-command visibility depends on the V3.1 default.");
            return command;
        }

        private static bool TryFindExistingColourCommand(
            BlockActivationCommand[] commands,
            out int index)
        {
            index = -1;
            if (commands == null)
                return false;

            for (int i = 0; i < commands.Length; i++)
            {
                string text = commands[i].text;
                if (IsColourCommandName(text))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindExistingBrightnessCommand(
            BlockActivationCommand[] commands,
            out int index)
        {
            index = -1;
            if (commands == null)
                return false;

            for (int i = 0; i < commands.Length; i++)
            {
                string text = commands[i].text;
                if (IsBrightnessCommandName(text))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        private static bool IsGrowLight(Block block)
        {
            return block != null &&
                   GrowLightScanner.TryGetGrowLightCoverage(block, out _, out _, out _);
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

        private static bool FindBlockValue(object[] args, out bool found)
        {
            found = false;
            if (args == null)
                return false;

            foreach (object arg in args)
            {
                if (arg is BlockValue)
                {
                    found = true;
                    return true;
                }
            }

            return false;
        }

        private static BlockValue FindLastBlockValue(object[] args)
        {
            BlockValue value = default(BlockValue);
            if (args == null)
                return value;

            foreach (object arg in args)
            {
                if (arg is BlockValue blockValue)
                    value = blockValue;
            }

            return value;
        }
    }
}
