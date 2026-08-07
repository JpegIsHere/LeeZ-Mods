namespace LeezGrowLights
{
    internal static class GrowthScheduleContext
    {
        [System.ThreadStatic]
        private static float currentMultiplier;

        public static float CurrentMultiplier
        {
            get => currentMultiplier <= 0f ? 1f : currentMultiplier;
            set => currentMultiplier = value <= 0f ? 1f : value;
        }
    }
}
