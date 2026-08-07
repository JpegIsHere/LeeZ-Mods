namespace LeezGrowLights
{
    internal static class LeezLog
    {
        private const string Prefix = "[LeezGrowLights] ";

        public static void Info(string message) => Log.Out(Prefix + message);
        public static void Warning(string message) => Log.Warning(Prefix + message);
        public static void Error(string message) => Log.Error(Prefix + message);
    }
}
