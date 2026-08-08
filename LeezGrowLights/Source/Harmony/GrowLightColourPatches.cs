using System;
using System.Reflection;

namespace LeezGrowLights
{
    internal static class GrowLightColourPatches
    {
        private const string ColourCommandPrefix = "Grow light colour:";

        public static void ActivationCommandsPostfix(
            object __instance,
            object[] __args,
            ref BlockActivationCommand[] __result)
        {
            Block block = __instance as Block;
            if (!IsGrowLight(block))
                return;

            if (TryFindExistingColourCommand(__result, out int existingIndex))
            {
                BlockActivationCommand existing = __result[existingIndex];
                existing = EnsureEnabled(existing);
                __result[existingIndex] = existing;
                return;
            }

            int originalLength = __result != null ? __result.Length : 0;

            BlockValue value = FindBlockValue(__args, out bool foundValue)
                ? FindLastBlockValue(__args)
                : default(BlockValue);

            GrowLightColour selected = foundValue
                ? GrowLightColourState.Get(value)
                : GrowLightColourPalette.Default;

            BlockActivationCommand colourCommand = new BlockActivationCommand
            {
                text = ColourCommandPrefix + " " + selected,
                iconColor = GrowLightColourPalette.ToUnityColour(selected),
                activateTime = 0f,
                highlighted = false
            };

            colourCommand = EnsureEnabled(colourCommand);

            BlockActivationCommand[] expanded =
                new BlockActivationCommand[originalLength + 1];

            if (__result != null && __result.Length > 0)
                Array.Copy(__result, expanded, __result.Length);

            expanded[originalLength] = colourCommand;
            __result = expanded;

            LeezLog.Info(
                "Grow-light colour command exposed at index " + originalLength +
                " as '" + colourCommand.text + "'.");
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
            // string _commandName, not by an integer index. Vanilla light toggling uses
            // the command name "light", so only our dynamic colour label is intercepted.
            if (!IsColourCommandName(commandName))
                return true;

            LeezLog.Info(
                "Grow-light colour activation recognized: command='" + commandName + "'.");

            WorldBase world = FindFirst<WorldBase>(__args);
            if (world == null || !TryGetFirstVector3i(__args, out Vector3i position))
            {
                LeezLog.Warning("Grow-light colour command could not resolve world/position.");
                __result = false;
                return false;
            }

            BlockValue value = FindBlockValue(__args, out bool foundValue)
                ? FindLastBlockValue(__args)
                : world.GetBlock(position);

            if (!foundValue && value.Block == null)
            {
                LeezLog.Warning(
                    "Grow-light colour command resolved an empty block value at " + position + ".");
                __result = false;
                return false;
            }

            if (world.IsRemote())
            {
                LeezLog.Warning(
                    "Remote grow-light colour request ignored until server command routing is enabled.");
                __result = false;
                return false;
            }

            GrowLightColour current = GrowLightColourState.Get(value);
            GrowLightColour next = GrowLightColourPalette.Next(current);
            bool changed = GrowLightColourState.TrySet(world, position, value, next);

            if (changed)
            {
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

            GrowLightColourVisual.Apply(blockEntityData, value);
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
                ColourCommandPrefix,
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
                        "Could not enable grow-light colour command through member '" +
                        memberName + "': " + ex.Message);
                }
            }

            LeezLog.Warning(
                "BlockActivationCommand exposes no writable enabled/isEnabled member; " +
                "colour command visibility depends on the V3.1 default.");
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
