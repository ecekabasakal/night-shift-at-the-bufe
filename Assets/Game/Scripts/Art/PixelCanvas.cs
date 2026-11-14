using UnityEngine;

namespace NightShift
{
    // A small grid of pixels to draw on, with the handful of drawing tools the painters need.
    // Like Texture2D, (0,0) is the BOTTOM-left pixel and y grows upwards.
    // Drawing outside the canvas is silently ignored, so painters never need bounds checks.
    public class PixelCanvas
    {
        public readonly int Width;
        public readonly int Height;

        // Row by row from the bottom; ready for Texture2D.SetPixels32.
        public readonly Color32[] Pixels;

        public PixelCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new Color32[width * height]; // all transparent
        }

        public bool Inside(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        // Transparent for positions outside the canvas.
        public Color32 Get(int x, int y)
        {
            if (!Inside(x, y)) return new Color32(0, 0, 0, 0);
            return Pixels[y * Width + x];
        }

        public bool IsOpaque(int x, int y)
        {
            return Get(x, y).a > 0;
        }

        // Replaces the pixel.
        public void SetPixel(int x, int y, Color32 color)
        {
            if (Inside(x, y)) Pixels[y * Width + x] = color;
        }

        // Paints the colour over the pixel with the given strength (0 = nothing, 1 = replace).
        // Works on transparent pixels too (the result is then partly transparent).
        public void Blend(int x, int y, Color32 color, float alpha)
        {
            if (!Inside(x, y) || alpha <= 0f) return;
            if (alpha > 1f) alpha = 1f;

            Color32 old = Pixels[y * Width + x];
            float oldAlpha = old.a / 255f;
            float outAlpha = alpha + oldAlpha * (1f - alpha);
            float keep = oldAlpha * (1f - alpha);

            Pixels[y * Width + x] = new Color32(
                (byte)((color.r * alpha + old.r * keep) / outAlpha + 0.5f),
                (byte)((color.g * alpha + old.g * keep) / outAlpha + 0.5f),
                (byte)((color.b * alpha + old.b * keep) / outAlpha + 0.5f),
                (byte)(outAlpha * 255f + 0.5f));
        }

        // (x, y) is the bottom-left corner of the rectangle.
        public void FillRect(int x, int y, int width, int height, Color32 color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    SetPixel(px, py, color);
                }
            }
        }

        public void BlendRect(int x, int y, int width, int height, Color32 color, float alpha)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    Blend(px, py, color, alpha);
                }
            }
        }

        // A one pixel wide line between two points (both ends included).
        public void Line(int x0, int y0, int x1, int y1, Color32 color)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            if (steps == 0)
            {
                SetPixel(x0, y0, color);
                return;
            }
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                SetPixel(Mathf.RoundToInt(x0 + (x1 - x0) * t), Mathf.RoundToInt(y0 + (y1 - y0) * t), color);
            }
        }

        // A filled ellipse. The centre may lie between pixels (e.g. 31.5) for an even width.
        public void FillEllipse(float centreX, float centreY, float radiusX, float radiusY, Color32 color)
        {
            BlendEllipse(centreX, centreY, radiusX, radiusY, color, 1f);
        }

        public void BlendEllipse(float centreX, float centreY, float radiusX, float radiusY, Color32 color, float alpha)
        {
            if (radiusX <= 0f || radiusY <= 0f) return;
            int minX = Mathf.FloorToInt(centreX - radiusX);
            int maxX = Mathf.CeilToInt(centreX + radiusX);
            int minY = Mathf.FloorToInt(centreY - radiusY);
            int maxY = Mathf.CeilToInt(centreY + radiusY);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x - centreX) / radiusX;
                    float dy = (y - centreY) / radiusY;
                    if (dx * dx + dy * dy <= 1f) Blend(x, y, color, alpha);
                }
            }
        }

        // A soft round light: strongest in the centre, fading to nothing at the rim.
        public void Glow(float centreX, float centreY, float radiusX, float radiusY, Color32 color, float alpha)
        {
            if (radiusX <= 0f || radiusY <= 0f) return;
            int minX = Mathf.FloorToInt(centreX - radiusX);
            int maxX = Mathf.CeilToInt(centreX + radiusX);
            int minY = Mathf.FloorToInt(centreY - radiusY);
            int maxY = Mathf.CeilToInt(centreY + radiusY);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x - centreX) / radiusX;
                    float dy = (y - centreY) / radiusY;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance < 1f) Blend(x, y, color, alpha * (1f - distance));
                }
            }
        }

        // Draws a one pixel border around everything painted so far
        // (every transparent pixel that touches a painted one gets the colour).
        public void Outline(Color32 color)
        {
            bool[] border = new bool[Pixels.Length];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (IsOpaque(x, y)) continue;
                    border[y * Width + x] =
                        IsOpaque(x - 1, y) || IsOpaque(x + 1, y) || IsOpaque(x, y - 1) || IsOpaque(x, y + 1);
                }
            }
            for (int i = 0; i < border.Length; i++)
            {
                if (border[i]) Pixels[i] = color;
            }
        }

        // Pastes another canvas on top of this one; (x, y) is where its bottom-left corner goes.
        public void Blit(PixelCanvas source, int x, int y)
        {
            for (int sy = 0; sy < source.Height; sy++)
            {
                for (int sx = 0; sx < source.Width; sx++)
                {
                    Color32 color = source.Pixels[sy * source.Width + sx];
                    if (color.a == 0) continue;
                    Color32 solid = new Color32(color.r, color.g, color.b, 255);
                    Blend(x + sx, y + sy, solid, color.a / 255f);
                }
            }
        }
    }
}
