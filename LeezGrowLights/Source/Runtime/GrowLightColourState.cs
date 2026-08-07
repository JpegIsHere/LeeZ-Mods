using System.Collections.Generic;

namespace LeezGrowLights
{
    internal static class GrowLightColourState
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<Vector3i, GrowLightColour> ColoursByPosition = new Dictionary<Vector3i, GrowLightColour>();

        public static GrowLightColour Get(Vector3i position)
        {
            lock (Sync)
            {
                GrowLightColour colour;
                return ColoursByPosition.TryGetValue(position, out colour)
                    ? colour
                    : GrowLightColourPalette.Default;
            }
        }

        public static void Set(Vector3i position, GrowLightColour colour)
        {
            lock (Sync)
            {
                ColoursByPosition[position] = colour;
            }
        }

        public static bool Remove(Vector3i position)
        {
            lock (Sync)
            {
                return ColoursByPosition.Remove(position);
            }
        }

        public static void Clear()
        {
            lock (Sync)
            {
                ColoursByPosition.Clear();
            }
        }
    }
}
