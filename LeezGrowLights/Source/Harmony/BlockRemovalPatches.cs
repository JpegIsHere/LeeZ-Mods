namespace LeezGrowLights
{
    /// <summary>
    /// Preserves crop progress when a LeeZ grow light is physically removed/destroyed.
    ///
    /// Prefix runs while the lamp still exists, capturing each affected crop's effective
    /// multiplier. Postfix runs after vanilla removal, re-scans remaining overlapping lamps,
    /// and reschedules only crops whose effective multiplier changed.
    /// </summary>
    internal static class BlockRemovalPatches
    {
        public static void Prefix(
            object[] __args,
            ref GrowLightTransitionRescheduler.TransitionState __state)
        {
            __state = null;

            if (__args == null)
                return;

            WorldBase world = null;
            Vector3i blockPos = default(Vector3i);
            BlockValue blockValue = default(BlockValue);
            bool foundPos = false;
            bool foundBlockValue = false;

            foreach (object arg in __args)
            {
                if (world == null && arg is WorldBase foundWorld)
                {
                    world = foundWorld;
                    continue;
                }

                if (!foundPos && arg is Vector3i foundPosValue)
                {
                    blockPos = foundPosValue;
                    foundPos = true;
                    continue;
                }

                if (!foundBlockValue && arg is BlockValue foundBlock)
                {
                    blockValue = foundBlock;
                    foundBlockValue = true;
                }
            }

            if (world == null || world.IsRemote() || !foundPos || !foundBlockValue)
                return;

            Block removedBlock = blockValue.Block;
            if (removedBlock == null)
                return;

            if (!GrowLightScanner.TryGetGrowLightCoverage(
                    removedBlock,
                    out _,
                    out _,
                    out _))
            {
                return;
            }

            __state = GrowLightTransitionRescheduler.Capture(
                world,
                blockPos,
                removedBlock);
        }

        public static void Postfix(GrowLightTransitionRescheduler.TransitionState __state)
        {
            GrowLightTransitionRescheduler.Apply(__state);
        }
    }
}
