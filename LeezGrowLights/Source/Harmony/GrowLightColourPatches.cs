using System;
using System.Collections.Generic;
using System.Reflection;

namespace LeezGrowLights
{
    internal static class GrowLightColourPatches
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<Block, int> ColourCommandIndices =
            new Dictionary<Block, int>();
        private static int LastKnownColourCommandIndex = -1;

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

                RememberColourCommandIndex(block, existingIndex);

                LeezLog.Info(
                    "Grow-light colour command confirmed at index " + existingIndex + ".");
                return;
            }

            int originalLength = __result != null ? __result.Length : 0;
            RememberColourCommandIndex(block, originalLength);

            BlockValue value = FindBlockValue(__args, out bool foundValue)
                ? FindLastBlockValue(__args)
                : default(BlockValue);

            GrowLightColour selected = foundValue
                ? GrowLightColourState.Get(value)
                : GrowLightColourPalette.Default;

            BlockActivationCommand colourCommand = new BlockActivationCommand
            {
                text = "Grow light colour: " + selected,
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

            bool blockSpecificIndex = TryGetColourCommandIndex(block, out int colourCommandIndex);
            if (!blockSpecificIndex)
            {
                lock (Sync)
                {
                    colourCommandIndex = LastKnownColourCommandIndex;
                }
            }

            if (colourCommandIndex < 0)
            {
                LeezLog.Warning(
                    "Grow-light activation observed but no colour command index is known. " +
                    DescribeActivation(__originalMethod, __args));
                return true;
            }

            if (!TryGetActivatedCommandIndex(
                    __originalMethod,
                    __args,
                    colourCommandIndex,
                    out int activatedIndex,
                    out string indexSource))
            {
                LeezLog.Warning(
                    "Grow-light activation observed but command index could not be resolved; " +
                    "colourIndex=" + colourCommandIndex + ". " +
                    DescribeActivation(__originalMethod, __args));
                return true;
            }

            LeezLog.Info(
                "Grow-light activation observed: activatedIndex=" + activatedIndex +
                " via " + indexSource +
                ", colourIndex=" + colourCommandIndex +
                ", indexCache=" + (blockSpecificIndex ? "block" : "fallback") +
                ". " + DescribeActivation(__originalMethod, __args));

            if (activatedIndex != colourCommandIndex)
                return true;

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

        private static void RememberColourCommandIndex(Block block, int index)
        {
            lock (Sync)
            {
                if (block != null)
                    ColourCommandIndices[block] = index;
                LastKnownColourCommandIndex = index;
            }
        }

        private static bool TryGetActivatedCommandIndex(
            MethodBase originalMethod,
            object[] args,
            int expectedIndex,
            out int value,
            out string source)
        {
            value = 0;
            source = null;
            if (args == null)
                return false;

            ParameterInfo[] parameters = SafeParameters(originalMethod);
            int count = Math.Min(parameters.Length, args.Length);

            for (int i = 0; i < count; i++)
            {
                if (!(args[i] is int integer))
                    continue;

                string name = parameters[i].Name ?? string.Empty;
                string lower = name.ToLowerInvariant();
                bool activationIndex =
                    lower.Contains("indexinblockactivationcommands") ||
                    (lower.Contains("activation") && lower.Contains("index")) ||
                    (lower.Contains("command") && lower.Contains("index"));

                if (activationIndex)
                {
                    value = integer;
                    source = "parameter '" + name + "'";
                    return true;
                }
            }

            int matchingExpected = 0;
            int matchedValue = 0;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is int integer && integer == expectedIndex)
                {
                    matchingExpected++;
                    matchedValue = integer;
                }
            }

            if (matchingExpected == 1)
            {
                value = matchedValue;
                source = "unique int matching exposed colour index";
                return true;
            }

            int intCount = 0;
            int firstInt = 0;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is int integer)
                {
                    if (intCount == 0)
                        firstInt = integer;
                    intCount++;
                }
            }

            if (intCount == 1)
            {
                value = firstInt;
                source = "only int argument";
                return true;
            }

            if (intCount > 0)
            {
                value = firstInt;
                source = "first-int diagnostic fallback";
                return true;
            }

            return false;
        }

        private static string DescribeActivation(MethodBase method, object[] args)
        {
            string methodName = method != null
                ? (method.DeclaringType != null ? method.DeclaringType.Name + "." : string.Empty) + method.Name
                : "<unknown method>";

            ParameterInfo[] parameters = SafeParameters(method);
            var parts = new List<string>();
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string parameterName = i < parameters.Length
                        ? parameters[i].Name
                        : "arg" + i;
                    object arg = args[i];
                    string typeName = arg != null ? arg.GetType().Name : "null";
                    string displayValue;
                    try
                    {
                        displayValue = arg != null ? arg.ToString() : "null";
                    }
                    catch
                    {
                        displayValue = "<ToString failed>";
                    }

                    parts.Add(parameterName + ":" + typeName + "=" + displayValue);
                }
            }

            return methodName + " args=[" + string.Join(", ", parts.ToArray()) + "]";
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
                if (!string.IsNullOrEmpty(text) &&
                    text.StartsWith("Grow light colour:", StringComparison.Ordinal))
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

        private static bool TryGetColourCommandIndex(Block block, out int index)
        {
            lock (Sync)
            {
                return ColourCommandIndices.TryGetValue(block, out index);
            }
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
