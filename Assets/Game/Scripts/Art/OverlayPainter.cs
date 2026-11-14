using UnityEngine;

namespace NightShift
{
    // Paints the small helper pictures: the screen-edge vignette and the UI panel box.
    // Both are white / grey so that whoever shows them can tint them.
    public static class OverlayPainter
    {
        public const int PanelSize = 12;
        public const int PanelBorder = 4;

        // White pixels that are transparent in the middle of the screen and solid at the edges.
        // The transparency changes in a few dithered steps, to match the pixel art.
        public static PixelCanvas PaintVignette()
        {
            const int steps = 8;
            int width = GameLayout.BackgroundArtWidth;
            int height = GameLayout.BackgroundArtHeight;
            PixelCanvas canvas = new PixelCanvas(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x + 0.5f - width * 0.5f) / (width * 0.5f);
                    float dy = (y + 0.5f - height * 0.5f) / (height * 0.5f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);         // 0 centre, 1 edge, 1.41 corner
                    float strength = Mathf.Clamp01((distance - 0.55f) / 0.75f);
                    strength *= strength;

                    // Checkerboard dither: every other pixel rounds up half a step earlier.
                    float dither = ((x + y) % 2 == 0) ? 0.5f : 0f;
                    int level = Mathf.Min(steps, (int)(strength * steps + dither));
                    canvas.SetPixel(x, y, new Color32(255, 255, 255, (byte)(255 * level / steps)));
                }
            }
            return canvas;
        }

        // A 12 x 12 box: grey rim, white bevel line, light fill, corners cut off.
        // Meant for 9-slicing with a 4 pixel border.
        public static PixelCanvas PaintPanel()
        {
            PixelCanvas canvas = new PixelCanvas(PanelSize, PanelSize);
            int last = PanelSize - 1;

            canvas.FillRect(0, 0, PanelSize, PanelSize, Palette.Rgb(120, 120, 132));        // rim
            canvas.FillRect(1, 1, PanelSize - 2, PanelSize - 2, Palette.White);             // bevel
            canvas.FillRect(2, 2, PanelSize - 4, PanelSize - 4, Palette.Rgb(214, 214, 222)); // fill
            canvas.FillRect(2, 2, PanelSize - 4, 1, Palette.Rgb(176, 176, 190));            // inner shadow at the bottom

            canvas.SetPixel(0, 0, Palette.Clear);
            canvas.SetPixel(last, 0, Palette.Clear);
            canvas.SetPixel(0, last, Palette.Clear);
            canvas.SetPixel(last, last, Palette.Clear);
            return canvas;
        }
    }
}
