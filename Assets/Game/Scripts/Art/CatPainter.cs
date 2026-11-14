using UnityEngine;

namespace NightShift
{
    // Paints a cat sitting on the counter, facing the player, with a tail that sways.
    // On the other side the "tallCat" anomaly keeps the head and the seat
    // but stretches everything in between right up to the ceiling.
    public class CatPainter
    {
        // The cat sits on the counter, a little in front of its far edge.
        public const int BaseRow = 8;

        // Tail positions per frame: left, middle, right, middle.
        public const int FrameCount = 4;
        private static readonly int[] TailSwing = { 0, 1, 2, 1 };

        private const float CentreX = 31.5f;
        private const int NormalHeadY = 36;
        private const int TallHeadY = 66;

        private readonly PixelCanvas canvas;
        private readonly CustomerLook look;
        private readonly Color32 fur, furDark, furLight;

        private CatPainter(PixelCanvas canvas, CustomerLook look)
        {
            this.canvas = canvas;
            this.look = look;
            fur = look.Hollow ? Palette.Void : look.Skin;
            furDark = look.Hollow ? Palette.Void : Palette.Darker(look.Skin);
            furLight = look.Hollow ? Palette.Void : Palette.Mix(look.Skin, Palette.White, 0.5f);
        }

        public static void Paint(PixelCanvas canvas, CustomerLook look, int frame)
        {
            CatPainter painter = new CatPainter(canvas, look);
            painter.PaintTail(TailSwing[frame % FrameCount]);
            painter.PaintSeat();
            if (look.TallCat) painter.PaintLongBody();
            painter.PaintHead(look.TallCat ? TallHeadY : NormalHeadY);
        }

        // A curved tail to the right of the body; swing moves its tip.
        private void PaintTail(int swing)
        {
            float tipX = 51 + swing * 2;
            float tipY = 25 - swing * 2;
            for (int i = 0; i <= 16; i++)
            {
                // A curve from the seat (40,10) past (54,9) to the tip.
                float t = i / 16f;
                float x = (1 - t) * (1 - t) * 40 + 2 * (1 - t) * t * 54 + t * t * tipX;
                float y = (1 - t) * (1 - t) * 10 + 2 * (1 - t) * t * 9 + t * t * tipY;
                canvas.FillEllipse(x, y, 1.6f, 1.6f, i >= 13 ? furDark : fur);
            }
        }

        // Lower body, chest patch, front paws and side stripes.
        private void PaintSeat()
        {
            canvas.FillEllipse(CentreX, 19, 10, 11, furDark);
            canvas.FillEllipse(CentreX - 1, 19, 9, 11, fur);
            canvas.FillRect(24, BaseRow, 16, 3, fur);
            canvas.FillEllipse(CentreX, 20, 4.5f, 7, furLight);
            canvas.FillEllipse(27, 9.5f, 3, 1.6f, furLight);
            canvas.FillEllipse(36, 9.5f, 3, 1.6f, furLight);

            for (int y = 14; y <= 22; y += 4)
            {
                canvas.FillRect(22, y, 3, 1, furDark);
                canvas.FillRect(39, y, 3, 1, furDark);
            }
        }

        // The stretched part of a tall cat: a striped column that sways a little on its way up.
        private void PaintLongBody()
        {
            for (int y = 24; y <= TallHeadY - 5; y++)
            {
                int sway = Mathf.RoundToInt(2f * Mathf.Sin((y - 24) * 0.22f));
                int left = 26 + sway;
                canvas.FillRect(left, y, 12, 1, fur);
                canvas.FillRect(left + 10, y, 2, 1, furDark);
                canvas.FillRect(left + 4, y, 4, 1, furLight);
                if (y % 5 == 0)
                {
                    canvas.FillRect(left, y, 3, 1, furDark);
                    canvas.FillRect(left + 8, y, 4, 1, furDark);
                }
            }
        }

        private void PaintHead(int headY)
        {
            // Ears: two triangles, mirrored.
            int[] earWidths = { 7, 6, 5, 4, 2, 1 };
            for (int i = 0; i < earWidths.Length; i++)
            {
                int y = headY + 5 + i;
                int inset = (7 - earWidths[i]) / 2;
                canvas.FillRect(22 + inset, y, earWidths[i], 1, fur);
                canvas.FillRect(35 + inset, y, earWidths[i], 1, furDark);
            }
            if (!look.Hollow)
            {
                canvas.FillRect(24, headY + 7, 2, 2, Palette.CatNose);
                canvas.FillRect(37, headY + 7, 2, 2, Palette.CatNose);
            }

            canvas.FillEllipse(CentreX, headY, 9.5f, 7.5f, furDark);
            canvas.FillEllipse(CentreX - 1, headY, 8.5f, 7.5f, fur);

            if (look.Staring) canvas.FillEllipse(CentreX, headY, 9.5f, 7.5f, Palette.Shade(fur, 0.5f));
            if (!look.Hollow)
            {
                canvas.FillEllipse(CentreX, headY - 3.5f, 4, 2.5f, furLight);  // muzzle
                canvas.FillRect(29, headY + 4, 1, 3, furDark);                 // forehead stripes
                canvas.FillRect(31, headY + 5, 2, 2, furDark);
                canvas.FillRect(34, headY + 4, 1, 3, furDark);
            }

            if (!look.Hollow || look.Staring)
            {
                FacePainter.PaintEyes(canvas, 25, 35, headY, look, true);
            }
            if (look.Hollow) return;

            if (look.TooManyTeeth)
            {
                FacePainter.PaintGrin(canvas, 25, 38, headY - 8, false);
                return;
            }
            canvas.FillRect(31, headY - 3, 2, 1, Palette.CatNose);
            canvas.SetPixel(30, headY - 5, Palette.Outline);
            canvas.FillRect(31, headY - 4, 2, 1, Palette.Outline);
            canvas.SetPixel(33, headY - 5, Palette.Outline);
        }
    }
}
