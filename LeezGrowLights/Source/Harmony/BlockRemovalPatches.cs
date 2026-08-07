namespace LeezGrowLights
{
    /// <summary>
    /// Preserves crop progress when a LeeZ grow light is physically removed/destroyed.
    ///
    /// Prefix runs on the V3.1 block-removal callback, captures each affected crop's
    /// effective multiplier, then Postfix re-scans remaining overlapping lamps and
    /// reschedules only crops whose effective multiplier changed.
    /// </summary>
    internal static class BlockRemovalPatches
    {
        public static void Prefix(
            object __instance,
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

            if (world == null || world.IsRemote() || !foundPos)
                return;

            // Harmony supplies the concrete block instance for the removal callback.
            // Prefer that over the BlockValue argument so this remains resilient if the
            // V3 method signature changes while still passing world/position.
            Block removedBlock = __instance as Block;
            if (removedBlock == null && foundBlockValue)
                removedBlock = blockValue.Block;

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
