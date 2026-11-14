using UnityEngine;

namespace NightShift
{
    // Everything the painters need to know about one customer picture:
    // the appearance from the data file, plus which anomalies to show (only ever set in the Shift view).
    public class CustomerLook
    {
        public bool IsCat;
        public Color32 Skin;
        public Color32 Hair;
        public Color32 Outfit;
        public int HairStyle;     // 0 bald, 1 short, 2 long, 3 bun, 4 cap/hat
        public int OutfitStyle;   // 0 t-shirt, 1 jacket, 2 hoodie, 3 uniform
        public string Accessory;  // "none" | "whistle" | "glasses" | "bag" | "scarf"

        public bool ShiftView;
        public bool NoShadow;
        public bool ReversedShadow;
        public bool NoEyes;
        public bool WrongEyes;
        public bool ExtraFingers;
        public bool TooManyTeeth;
        public bool NoWhistle;
        public bool Hollow;
        public bool Staring;
        public bool TallCat;

        public static CustomerLook From(CustomerData customer, bool shiftView)
        {
            CustomerLook look = new CustomerLook();
            Appearance appearance = (customer != null && customer.appearance != null) ? customer.appearance : new Appearance();

            look.IsCat = appearance.kind == "cat";
            look.Skin = Palette.Parse(appearance.skinColor);
            look.Hair = Palette.Parse(appearance.hairColor);
            look.Outfit = Palette.Parse(appearance.outfitColor);
            look.HairStyle = appearance.hairStyle;
            look.OutfitStyle = appearance.outfit;
            look.Accessory = string.IsNullOrEmpty(appearance.accessory) ? "none" : appearance.accessory;
            look.ShiftView = shiftView;

            // Anomalies exist only on the other side; in the normal view everybody looks ordinary.
            if (shiftView && customer != null)
            {
                look.NoShadow = customer.HasAnomaly(Anomalies.NoShadow);
                look.ReversedShadow = customer.HasAnomaly(Anomalies.ReversedShadow);
                look.NoEyes = customer.HasAnomaly(Anomalies.NoEyes);
                look.WrongEyes = customer.HasAnomaly(Anomalies.WrongEyes);
                look.ExtraFingers = customer.HasAnomaly(Anomalies.ExtraFingers);
                look.TooManyTeeth = customer.HasAnomaly(Anomalies.TooManyTeeth);
                look.NoWhistle = customer.HasAnomaly(Anomalies.NoWhistle);
                look.Hollow = customer.HasAnomaly(Anomalies.Hollow);
                look.Staring = customer.HasAnomaly(Anomalies.Staring);
                look.TallCat = customer.HasAnomaly(Anomalies.TallCat);
            }
            return look;
        }

        // Two customers with the same key get exactly the same picture (used for caching).
        public string CacheKey()
        {
            return (IsCat ? "cat" : "human")
                + "|" + Skin.r + "," + Skin.g + "," + Skin.b
                + "|" + Hair.r + "," + Hair.g + "," + Hair.b
                + "|" + Outfit.r + "," + Outfit.g + "," + Outfit.b
                + "|" + HairStyle + "|" + OutfitStyle + "|" + Accessory
                + "|" + (ShiftView ? "S" : "N")
                + (NoShadow ? "a" : "") + (ReversedShadow ? "b" : "") + (NoEyes ? "c" : "") + (WrongEyes ? "d" : "")
                + (ExtraFingers ? "e" : "") + (TooManyTeeth ? "f" : "") + (NoWhistle ? "g" : "") + (Hollow ? "h" : "")
                + (Staring ? "i" : "") + (TallCat ? "j" : "");
        }
    }

    // Puts one customer picture together (64 x 80 art pixels):
    // the figure (human or cat), its outline, the Shift colours and the shadow.
    public static class CustomerPainter
    {
        public const int Width = GameLayout.CustomerArtWidth;
        public const int Height = GameLayout.CustomerArtHeight;

        // How many animation frames this customer has (cats move their tail).
        public static int FrameCount(CustomerLook look)
        {
            return look.IsCat ? CatPainter.FrameCount : 1;
        }

        public static PixelCanvas Paint(CustomerLook look, int frame)
        {
            PixelCanvas figure = new PixelCanvas(Width, Height);
            int baseRow;
            if (look.IsCat)
            {
                CatPainter.Paint(figure, look, frame);
                baseRow = CatPainter.BaseRow;
            }
            else
            {
                HumanPainter.Paint(figure, look);
                baseRow = HumanPainter.BaseRow;
            }

            if (look.ShiftView)
            {
                for (int i = 0; i < figure.Pixels.Length; i++)
                {
                    if (figure.Pixels[i].a > 0) figure.Pixels[i] = Palette.OtherSideFigure(figure.Pixels[i]);
                }
            }
            figure.Outline(Palette.Outline);

            // Shadows only show on the other side (rule 1 of the notebook).
            PixelCanvas picture = new PixelCanvas(Width, Height);
            if (look.ShiftView && look.ReversedShadow) ShadowPainter.PaintTowardsLight(picture, figure, baseRow);
            else if (look.ShiftView && !look.NoShadow) ShadowPainter.PaintOnCounter(picture, figure, baseRow);

            picture.Blit(figure, 0, 0);
            return picture;
        }
    }

    // Cast shadows. The street lamp is behind the customer and to the right,
    // so an honest shadow falls on the counter, towards the player and to the left.
    public static class ShadowPainter
    {
        public const int CounterShadowRows = 12;
        public const float LeanPerRow = 1.2f;

        // Shadow strength for the given row (1 = touching the figure); it fades with distance.
        public static float AlphaAt(int row)
        {
            return 0.72f - 0.022f * row;
        }

        // The normal shadow: the figure's outline flattened onto the counter below baseRow.
        public static void PaintOnCounter(PixelCanvas target, PixelCanvas figure, int baseRow)
        {
            int top = TopRow(figure);
            int figureHeight = top - baseRow + 1;
            if (figureHeight <= 0 || baseRow <= 0) return;

            for (int row = 1; row <= baseRow; row++)
            {
                int sourceY = baseRow + (int)((row - 0.5f) * figureHeight / baseRow);
                int shift = (int)(row * LeanPerRow);
                float alpha = AlphaAt(row);

                for (int x = 0; x < figure.Width; x++)
                {
                    if (figure.IsOpaque(x, sourceY))
                    {
                        target.Blend(x - shift, baseRow - row, Palette.ShadowInk, alpha);
                    }
                    else if (figure.IsOpaque(x - 1, sourceY) || figure.IsOpaque(x + 1, sourceY))
                    {
                        target.Blend(x - shift, baseRow - row, Palette.ShadowInk, alpha * 0.45f); // soft edge
                    }
                }
            }
        }

        // The wrong shadow: nothing on the counter; instead a dark copy of the figure
        // stands up behind it and leans towards the street lamp.
        public static void PaintTowardsLight(PixelCanvas target, PixelCanvas figure, int baseRow)
        {
            for (int y = baseRow; y < figure.Height; y++)
            {
                int shift = 5 + (int)((y - baseRow) * 0.28f);
                for (int x = 0; x < figure.Width; x++)
                {
                    if (figure.IsOpaque(x, y)) target.Blend(x + shift, y + 3, Palette.ShadowInk, 0.68f);
                }
            }
        }

        private static int TopRow(PixelCanvas figure)
        {
            for (int y = figure.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < figure.Width; x++)
                {
                    if (figure.IsOpaque(x, y)) return y;
                }
            }
            return -1;
        }
    }

    // Eyes and mouths that are shared by humans and cats, including the wrong ones.
    public static class FacePainter
    {
        // Draws both eyes. (leftX, y) and (rightX, y) are the bottom-left pixels of two 4 x 2 eyes.
        // catEyes = true gives green eyes with a slit pupil.
        public static void PaintEyes(PixelCanvas canvas, int leftX, int rightX, int y, CustomerLook look, bool catEyes)
        {
            if (look.Staring)
            {
                PaintStaringEye(canvas, leftX, y);
                PaintStaringEye(canvas, rightX, y);
                return;
            }
            if (look.NoEyes) return; // smooth skin where the eyes should be

            Color32 white = catEyes ? Palette.CatEye : Palette.EyeWhite;
            if (look.WrongEyes)
            {
                // Too big, not level, and looking away from each other.
                canvas.FillRect(leftX - 1, y - 1, 5, 4, white);
                canvas.FillRect(leftX - 1, y + 1, 2, 2, Palette.Pupil);
                canvas.FillRect(rightX, y + 1, 5, 4, white);
                canvas.FillRect(rightX + 3, y + 1, 2, 2, Palette.Pupil);
                return;
            }

            for (int eye = 0; eye < 2; eye++)
            {
                int x = eye == 0 ? leftX : rightX;
                canvas.FillRect(x, y, 4, 2, white);
                if (catEyes)
                {
                    canvas.FillRect(x, y + 2, 4, 1, white);
                    canvas.FillRect(x + 1, y, 1, 3, Palette.Pupil);
                }
                else
                {
                    canvas.FillRect(x, y + 2, 4, 1, Palette.Outline); // upper lid
                    canvas.FillRect(x + 1, y, 2, 2, Palette.Pupil);
                }
            }
        }

        // A large glowing eye with a pin-point pupil aimed straight at the player.
        private static void PaintStaringEye(PixelCanvas canvas, int x, int y)
        {
            canvas.FillRect(x - 2, y - 2, 8, 6, Palette.StareGlow);
            canvas.FillRect(x - 1, y - 1, 6, 4, Palette.StareEye);
            canvas.FillRect(x, y, 4, 2, Palette.White);
            canvas.FillRect(x + 1, y, 2, 2, Palette.Pupil);
        }

        // A grin with far too many teeth, from x = left to x = right. bottomY is its lowest row in the
        // middle; with curved = true the corners rise by two pixels.
        public static void PaintGrin(PixelCanvas canvas, int left, int right, int bottomY, bool curved)
        {
            float centre = (left + right) * 0.5f;
            float half = (right - left) * 0.5f;

            for (int x = left; x <= right; x++)
            {
                float fromCentre = Mathf.Abs(x - centre) / half;
                int lift = !curved ? 0 : (fromCentre > 0.7f ? 2 : (fromCentre > 0.35f ? 1 : 0));
                int y = bottomY + lift;

                canvas.FillRect(x, y, 1, 7, Palette.MouthDark);
                if (x == left || x == right) continue;

                Color32 tooth = ((x - left) % 2 == 1) ? Palette.Tooth : Palette.ToothGap;
                canvas.FillRect(x, y + 1, 1, 2, tooth); // lower row
                canvas.FillRect(x, y + 4, 1, 2, tooth); // upper row
            }
        }
    }
}
