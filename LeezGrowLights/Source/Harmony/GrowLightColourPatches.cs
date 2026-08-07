using System;
using System.Collections.Generic;

namespace LeezGrowLights
{
    internal static class GrowLightColourPatches
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<Block, int> ColourCommandIndices =
            new Dictionary<Block, int>();

        public static void ActivationCommandsPostfix(
            object __instance,
            object[] __args,
            ref BlockActivationCommand[] __result)
        {
            Block block = __instance as Block;
            if (!IsGrowLight(block))
                return;

            int originalLength = __result != null ? __result.Length : 0;
            lock (Sync)
            {
                ColourCommandIndices[block] = originalLength;
            }

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

            BlockActivationCommand[] expanded =
                new BlockActivationCommand[originalLength + 1];

            if (__result != null && __result.Length > 0)
                Array.Copy(__result, expanded, __result.Length);

            expanded[originalLength] = colourCommand;
            __result = expanded;
        }

        public static bool ActivatedPrefix(
            object __instance,
            object[] __args,
            ref bool __result)
        {
            Block block = __instance as Block;
            if (!IsGrowLight(block))
                return true;

            if (!TryGetColourCommandIndex(block, out int colourCommandIndex))
                return true;

            if (!TryGetFirstInt(__args, out int activatedIndex) ||
                activatedIndex != colourCommandIndex)
            {
                return true;
            }

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
                __result = false;
                return false;
            }

            // 0.7.0-dev1 intentionally validates the local/server path first.  A remote
            // client must not author colour state locally; multiplayer command routing is
            // the next gate after single-player persistence/visual validation.
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

        private static bool TryGetFirstInt(object[] args, out int value)
        {
            value = 0;
            if (args == null)
                return false;

            foreach (object arg in args)
            {
                if (arg is int integer)
                {
                    value = integer;
                    return true;
                }
            }

            return false;
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
