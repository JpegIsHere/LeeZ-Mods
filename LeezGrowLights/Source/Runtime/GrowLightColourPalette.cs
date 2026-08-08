using System;
using UnityEngine;

namespace LeezGrowLights
{
    internal enum GrowLightColour : byte
    {
        Blue = 0,
        Green = 1,
        Red = 2,
        Purple = 3,
        White = 4,
        Yellow = 5
    }

    internal static class GrowLightColourPalette
    {
        private static readonly GrowLightColour[] AllowedColours =
        {
            GrowLightColour.Blue,
            GrowLightColour.Green,
            GrowLightColour.Red,
            GrowLightColour.Purple,
            GrowLightColour.White,
            GrowLightColour.Yellow
        };

        public static GrowLightColour Default => GrowLightColour.White;

        public static GrowLightColour[] GetAllowedColours()
        {
            return (GrowLightColour[])AllowedColours.Clone();
        }

        public static GrowLightColour Next(GrowLightColour current)
        {
            for (int i = 0; i < AllowedColours.Length; i++)
            {
                if (AllowedColours[i] == current)
                {
                    return AllowedColours[(i + 1) % AllowedColours.Length];
                }
            }

            return Default;
        }

        public static bool TryParse(string value, out GrowLightColour colour)
        {
            colour = Default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            for (int i = 0; i < AllowedColours.Length; i++)
            {
                GrowLightColour candidate = AllowedColours[i];
                if (string.Equals(candidate.ToString(), value.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    colour = candidate;
                    return true;
                }
            }

            return false;
        }

        public static Color ToUnityColour(GrowLightColour colour)
        {
            switch (colour)
            {
                case GrowLightColour.Blue:
                    return new Color(0.15f, 0.45f, 1.00f, 1.00f);
                case GrowLightColour.Green:
                    return new Color(0.20f, 1.00f, 0.30f, 1.00f);
                case GrowLightColour.Red:
                    return new Color(1.00f, 0.18f, 0.12f, 1.00f);
                case GrowLightColour.Purple:
                    return new Color(0.70f, 0.25f, 1.00f, 1.00f);
                case GrowLightColour.Yellow:
                    return new Color(1.00f, 0.85f, 0.15f, 1.00f);
                case GrowLightColour.White:
                default:
                    return Color.white;
            }
        }
    }

    internal enum GrowLightBrightness : byte
    {
        Dim = 0,
        Normal = 1,
        Bright = 2,
        VeryBright = 3,
        Maximum = 4
    }

    internal static class GrowLightBrightnessPalette
    {
        private static readonly GrowLightBrightness[] AllowedBrightness =
        {
            GrowLightBrightness.Dim,
            GrowLightBrightness.Normal,
            GrowLightBrightness.Bright,
            GrowLightBrightness.VeryBright,
            GrowLightBrightness.Maximum
        };

        public static GrowLightBrightness Default => GrowLightBrightness.Normal;

        public static GrowLightBrightness Next(GrowLightBrightness current)
        {
            for (int i = 0; i < AllowedBrightness.Length; i++)
            {
                if (AllowedBrightness[i] == current)
                    return AllowedBrightness[(i + 1) % AllowedBrightness.Length];
            }

            return Default;
        }

        public static string ToDisplayName(GrowLightBrightness brightness)
        {
            return brightness == GrowLightBrightness.VeryBright
                ? "Very Bright"
                : brightness.ToString();
        }

        public static float ToIntensityMultiplier(GrowLightBrightness brightness)
        {
            switch (brightness)
            {
                case GrowLightBrightness.Dim:
                    return 0.35f;
                case GrowLightBrightness.Bright:
                    return 1.50f;
                case GrowLightBrightness.VeryBright:
                    return 2.00f;
                case GrowLightBrightness.Maximum:
                    return 3.00f;
                case GrowLightBrightness.Normal:
                default:
                    return 1.00f;
            }
        }
    }
}
