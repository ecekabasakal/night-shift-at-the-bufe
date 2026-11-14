using UnityEngine;

namespace NightShift
{
    // The fixed colours of the game and the few colour sums the painters need.
    // Night = dark blue-violet, shop light = sickly green-white, street lamp = sodium orange,
    // the other side = the same picture as a cold magenta / teal negative.
    public static class Palette
    {
        public static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        public static readonly Color32 White = Rgb(255, 255, 255);
        public static readonly Color32 Outline = Rgb(18, 12, 30);
        public static readonly Color32 Void = Rgb(9, 5, 18);          // inside of a hollow body
        public static readonly Color32 ShadowInk = Rgb(10, 4, 24);    // cast shadows on the other side

        // Street behind the glass.
        public static readonly Color32 SkyTop = Rgb(10, 9, 28);
        public static readonly Color32 SkyLow = Rgb(30, 24, 62);
        public static readonly Color32 Building = Rgb(22, 19, 46);
        public static readonly Color32 BuildingFar = Rgb(34, 28, 66);
        public static readonly Color32 WindowWarm = Rgb(242, 190, 92);
        public static readonly Color32 WindowTv = Rgb(130, 200, 210);
        public static readonly Color32 Road = Rgb(26, 23, 48);
        public static readonly Color32 Pavement = Rgb(50, 45, 80);
        public static readonly Color32 Kerb = Rgb(78, 70, 112);
        public static readonly Color32 Sodium = Rgb(255, 176, 66);
        public static readonly Color32 SodiumCore = Rgb(255, 236, 170);
        public static readonly Color32 StreetCat = Rgb(70, 62, 98);

        // Morning street (ending pictures).
        public static readonly Color32 DawnTop = Rgb(142, 176, 214);
        public static readonly Color32 DawnLow = Rgb(250, 206, 160);
        public static readonly Color32 DawnBuilding = Rgb(150, 124, 142);
        public static readonly Color32 DawnBuildingFar = Rgb(196, 160, 160);
        public static readonly Color32 DawnRoad = Rgb(120, 112, 128);
        public static readonly Color32 DawnPavement = Rgb(190, 172, 160);
        public static readonly Color32 Sunlight = Rgb(255, 222, 160);

        // Shop interior (colours as seen under the fluorescent tube).
        public static readonly Color32 Ceiling = Rgb(36, 44, 58);
        public static readonly Color32 Wall = Rgb(58, 78, 76);
        public static readonly Color32 WallDark = Rgb(42, 58, 62);
        public static readonly Color32 Frame = Rgb(30, 40, 52);
        public static readonly Color32 FrameLight = Rgb(70, 90, 96);
        public static readonly Color32 Floor = Rgb(66, 72, 88);
        public static readonly Color32 FloorLine = Rgb(48, 52, 68);
        public static readonly Color32 ShelfBoard = Rgb(112, 92, 70);
        public static readonly Color32 Counter = Rgb(146, 158, 128);
        public static readonly Color32 CounterLight = Rgb(190, 202, 164);
        public static readonly Color32 CounterDark = Rgb(108, 120, 100);
        public static readonly Color32 Tube = Rgb(236, 255, 214);
        public static readonly Color32 TubeOff = Rgb(96, 108, 100);
        public static readonly Color32 TubeGlow = Rgb(190, 240, 170);
        public static readonly Color32 Metal = Rgb(150, 162, 164);
        public static readonly Color32 MetalDark = Rgb(74, 82, 92);
        public static readonly Color32 FridgeBody = Rgb(196, 210, 204);
        public static readonly Color32 FridgeGlow = Rgb(196, 255, 238);
        public static readonly Color32 FridgeBlue = Rgb(52, 110, 190);
        public static readonly Color32 Brass = Rgb(255, 214, 64);
        public static readonly Color32 Tea = Rgb(190, 60, 40);

        // Product packets on the shelves.
        public static readonly Color32[] Products =
        {
            Rgb(204, 72, 60), Rgb(232, 176, 64), Rgb(72, 140, 170), Rgb(214, 214, 196),
            Rgb(96, 160, 98), Rgb(170, 96, 150), Rgb(226, 128, 60)
        };

        // People.
        public static readonly Color32 EyeWhite = Rgb(244, 244, 236);
        public static readonly Color32 Pupil = Rgb(22, 16, 28);
        public static readonly Color32 Tooth = Rgb(250, 250, 240);
        public static readonly Color32 ToothGap = Rgb(150, 150, 160);
        public static readonly Color32 MouthDark = Rgb(48, 10, 28);
        public static readonly Color32 StareEye = Rgb(255, 252, 190);
        public static readonly Color32 StareGlow = Rgb(255, 230, 90);
        public static readonly Color32 Scarf = Rgb(222, 168, 60);
        public static readonly Color32 BagLeather = Rgb(92, 70, 48);
        public static readonly Color32 GlassesFrame = Rgb(24, 22, 32);
        public static readonly Color32 CatEye = Rgb(190, 230, 90);
        public static readonly Color32 CatNose = Rgb(230, 130, 140);

        // Colours of the other side, from "dark" to "bright".
        private static readonly Color32[] OtherRamp =
        {
            Rgb(12, 6, 26), Rgb(70, 20, 84), Rgb(150, 44, 130), Rgb(60, 150, 150), Rgb(200, 255, 236)
        };

        // The counter on the other side: pale, so that every shadow on it stands out.
        public static readonly Color32 OtherCounter = Rgb(112, 190, 184);
        public static readonly Color32 OtherCounterLight = Rgb(190, 246, 232);

        // The tint KioskView lays over the whole picture while Shift is held.
        public static readonly Color32 ShiftTint = Rgb(170, 60, 200);
        public static readonly Color32 ShiftVignette = Rgb(30, 0, 44);

        public static Color32 Rgb(int r, int g, int b)
        {
            return new Color32((byte)r, (byte)g, (byte)b, 255);
        }

        // "#RRGGBB" from the data files. A broken value comes back as magenta so it is easy to spot.
        public static Color32 Parse(string hex)
        {
            Color32 color = DataParsing.ParseColor(hex);
            color.a = 255;
            return color;
        }

        // Darker (factor below 1) or lighter (above 1) version of a colour.
        public static Color32 Shade(Color32 color, float factor)
        {
            return new Color32(ToByte(color.r * factor), ToByte(color.g * factor), ToByte(color.b * factor), color.a);
        }

        // Each channel multiplied separately: how a surface looks under coloured light.
        public static Color32 Multiply(Color32 color, float r, float g, float b)
        {
            return new Color32(ToByte(color.r * r), ToByte(color.g * g), ToByte(color.b * b), color.a);
        }

        // t = 0 gives a, t = 1 gives b.
        public static Color32 Mix(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                ToByte(a.r + (b.r - a.r) * t),
                ToByte(a.g + (b.g - a.g) * t),
                ToByte(a.b + (b.b - a.b) * t),
                ToByte(a.a + (b.a - a.a) * t));
        }

        // Highlight tone of a material.
        public static Color32 Lighter(Color32 color)
        {
            return Mix(color, White, 0.28f);
        }

        // Shadow tone of a material (slightly violet, like the night).
        public static Color32 Darker(Color32 color)
        {
            return Mix(Shade(color, 0.68f), Rgb(30, 16, 60), 0.15f);
        }

        // 0 = black, 1 = white.
        public static float Brightness(Color32 color)
        {
            return (0.299f * color.r + 0.587f * color.g + 0.114f * color.b) / 255f;
        }

        // The shop as it looks on the other side: a negative (dark becomes bright)
        // painted in magenta and teal.
        public static Color32 OtherSide(Color32 color)
        {
            Color32 result = Ramp(1f - Brightness(color));
            result.a = color.a;
            return result;
        }

        // A customer on the other side: keeps its own colours, pulled a little towards the cold ramp
        // so that the figure belongs to the picture but is still the same person.
        public static Color32 OtherSideFigure(Color32 color)
        {
            Color32 result = Mix(color, Ramp(Brightness(color)), 0.2f);
            result.a = color.a;
            return result;
        }

        private static Color32 Ramp(float t)
        {
            float scaled = Mathf.Clamp01(t) * (OtherRamp.Length - 1);
            int index = Mathf.Min((int)scaled, OtherRamp.Length - 2);
            return Mix(OtherRamp[index], OtherRamp[index + 1], scaled - index);
        }

        private static byte ToByte(float value)
        {
            if (value <= 0f) return 0;
            if (value >= 255f) return 255;
            return (byte)(value + 0.5f);
        }
    }

    // A tiny repeatable number generator: the same seed always gives the same numbers,
    // so the art is identical on every run (never seeded from the clock).
    public class SeededNumbers
    {
        private uint state;

        public SeededNumbers(uint seed)
        {
            state = seed == 0 ? 1u : seed;
        }

        // Turns a text (e.g. a customer id) into a seed.
        public static uint SeedFrom(string text)
        {
            uint hash = 2166136261u;
            if (text != null)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    hash = (hash ^ text[i]) * 16777619u;
                }
            }
            return hash;
        }

        // A whole number from min (included) to max (not included).
        public int Next(int min, int max)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            if (max <= min) return min;
            return min + (int)(state % (uint)(max - min));
        }

        // A number from 0 (included) to 1 (not included).
        public float NextFloat()
        {
            return Next(0, 10000) / 10000f;
        }
    }
}
