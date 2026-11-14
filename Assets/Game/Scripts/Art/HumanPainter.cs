using UnityEngine;

namespace NightShift
{
    // Paints a person standing behind the counter, from the waist up, facing the player.
    // The figure is 34 pixels wide in the middle of the 64 x 80 canvas and is cut off straight
    // at BaseRow, where it disappears behind the counter. Each material has three tones:
    // base, light (from the tube above, left) and dark (right side).
    public class HumanPainter
    {
        // The figure stands on this row; the rows below belong to the counter (and the shadow).
        public const int BaseRow = 12;

        private const int ShoulderRow = 41;   // top row of the torso
        private const float CentreX = 31.5f;
        private const float HeadY = 55f;      // centre of the head (it spans rows 44..66)
        private const int EyeRow = 56;
        private const int LeftEyeX = 25;
        private const int RightEyeX = 35;

        private readonly PixelCanvas canvas;
        private readonly CustomerLook look;
        private readonly Color32 skin, skinDark;
        private readonly Color32 hair, hairDark, hairLight;
        private readonly Color32 outfit, outfitDark, outfitLight;

        private HumanPainter(PixelCanvas canvas, CustomerLook look)
        {
            this.canvas = canvas;
            this.look = look;

            // A hollow customer has no body: wherever skin would show there is only darkness.
            skin = look.Hollow ? Palette.Void : look.Skin;
            skinDark = look.Hollow ? Palette.Void : Palette.Darker(look.Skin);
            hair = look.Hair;
            hairDark = Palette.Darker(hair);
            hairLight = Palette.Lighter(hair);
            outfit = look.Outfit;
            outfitDark = Palette.Darker(outfit);
            outfitLight = Palette.Lighter(outfit);
        }

        public static void Paint(PixelCanvas canvas, CustomerLook look)
        {
            new HumanPainter(canvas, look).PaintAll();
        }

        private void PaintAll()
        {
            if (look.OutfitStyle == 2) PaintHoodBehindNeck();
            PaintTorso();
            PaintNeck();
            PaintOutfitDetails();

            if (look.Accessory == "bag") PaintBag();
            if (look.Accessory == "whistle") PaintWhistle();
            if (look.Accessory == "scarf") PaintScarf();
            if (look.HairStyle == 2) PaintLongHairSides();

            PaintHead();
            if (!look.Hollow || look.Staring) PaintFace();
            PaintHair();
            if (look.Accessory == "glasses" && !look.Hollow) PaintGlasses();

            if (look.ExtraFingers) PaintRaisedHand();
        }

        // ---------- Body ----------

        // Half the width of the torso at a row: straight sides, sloping shoulders.
        private static int TorsoHalfWidth(int y)
        {
            switch (y)
            {
                case 41: return 7;
                case 40: return 11;
                case 39: return 14;
                case 38: return 15;
                case 37: return 16;
                default: return 17;
            }
        }

        private void PaintTorso()
        {
            for (int y = BaseRow; y <= ShoulderRow; y++)
            {
                int half = TorsoHalfWidth(y);
                int left = 32 - half;
                int right = 31 + half;
                canvas.FillRect(left, y, right - left + 1, 1, outfit);

                // Light catches the top of the left shoulder; the right flank is in shadow.
                if (y >= 37) canvas.FillRect(left, y, Mathf.Min(4, 30 - left), 1, outfitLight);
                canvas.FillRect(right - 1, y, 2, 1, outfitDark);
            }

            // Arms hang at the sides: a dark line separates them from the chest.
            canvas.FillRect(20, BaseRow, 1, 21, outfitDark);
            canvas.FillRect(43, BaseRow, 1, 21, outfitDark);

            if (look.OutfitStyle == 0)
            {
                // T-shirt: short sleeves, bare forearms.
                canvas.FillRect(15, BaseRow, 5, 15, skin);
                canvas.FillRect(44, BaseRow, 5, 15, skin);
                canvas.FillRect(47, BaseRow, 2, 15, skinDark);
                canvas.FillRect(15, 27, 5, 1, outfitDark);
                canvas.FillRect(44, 27, 5, 1, outfitDark);
            }
        }

        private void PaintNeck()
        {
            canvas.FillRect(28, 40, 8, 6, skin);
            canvas.FillRect(28, 43, 8, 2, skinDark); // shadow under the chin
        }

        private void PaintOutfitDetails()
        {
            switch (look.OutfitStyle)
            {
                case 1: PaintJacket(); break;
                case 2: PaintHoodie(); break;
                case 3: PaintUniform(); break;
                default: PaintTShirtNeck(); break;
            }
        }

        // Round neckline showing a little skin.
        private void PaintTShirtNeck()
        {
            int[] halfWidths = { 5, 4, 3, 1 };
            for (int i = 0; i < halfWidths.Length; i++)
            {
                int y = 40 - i;
                int half = halfWidths[i];
                canvas.FillRect(32 - half - 1, y, half * 2 + 2, 1, outfitDark);
                canvas.FillRect(32 - half, y, half * 2, 1, skin);
            }
            canvas.FillRect(31, 36, 2, 1, outfitDark);
        }

        // Open jacket: a strip of shirt down the middle between two lapels.
        private void PaintJacket()
        {
            Color32 shirt = Palette.Brightness(outfit) > 0.55f ? Palette.Rgb(64, 62, 78) : Palette.Rgb(226, 222, 204);
            for (int y = BaseRow; y <= 40; y++)
            {
                int half = y >= 32 ? 2 + (y - 32) : 2;
                half = Mathf.Min(half, TorsoHalfWidth(y) - 2);
                canvas.FillRect(32 - half, y, half * 2, 1, shirt);
                canvas.SetPixel(32 - half - 1, y, outfitDark);
                canvas.SetPixel(31 + half + 1, y, outfitDark);
                if (y >= 32)
                {
                    canvas.SetPixel(32 - half - 2, y, outfitLight); // lapel edge
                    canvas.SetPixel(31 + half + 2, y, outfitLight);
                }
            }
            canvas.FillRect(28, 39, 8, 2, Palette.Darker(shirt));
            canvas.FillRect(36, 18, 2, 2, outfitDark); // buttons
            canvas.FillRect(36, 25, 2, 2, outfitDark);
        }

        private void PaintHoodBehindNeck()
        {
            canvas.FillEllipse(CentreX, 42, 12.5f, 6, outfitDark);
        }

        // Thick collar, two drawstrings and a front pocket.
        private void PaintHoodie()
        {
            canvas.FillRect(24, 37, 16, 3, outfitLight);
            canvas.FillRect(25, 36, 14, 1, outfitDark);
            canvas.FillRect(29, 39, 6, 2, skin);
            canvas.FillRect(29, 39, 6, 1, skinDark);

            Color32 cord = Palette.Rgb(236, 232, 214);
            canvas.FillRect(29, 27, 1, 10, cord);
            canvas.FillRect(34, 29, 1, 8, cord);

            canvas.FillRect(23, 21, 18, 1, outfitDark);
            canvas.Line(23, 21, 21, BaseRow, outfitDark);
            canvas.Line(40, 21, 42, BaseRow, outfitDark);
        }

        // Night-watchman (bekci) uniform: collar, tie, brass buttons, shoulder straps, chest pocket.
        private void PaintUniform()
        {
            for (int i = 0; i < 4; i++)
            {
                canvas.FillRect(26 + i, 40 - i, 5 - i, 1, outfitLight); // collar, left wing
                canvas.FillRect(33, 40 - i, 5 - i, 1, outfitLight);     // collar, right wing
            }
            canvas.FillRect(31, 31, 2, 10, Palette.Rgb(20, 24, 40));    // tie
            canvas.FillRect(32, BaseRow, 1, 19, outfitDark);            // button strip

            for (int y = 15; y <= 27; y += 6)
            {
                canvas.FillRect(31, y, 2, 2, Palette.Brass);
            }

            canvas.FillRect(16, 36, 7, 2, outfitLight);                 // shoulder straps
            canvas.FillRect(41, 36, 7, 2, outfitLight);
            canvas.SetPixel(17, 36, Palette.Brass);
            canvas.SetPixel(46, 36, Palette.Brass);

            canvas.FillRect(36, 23, 6, 1, outfitDark);                  // chest pocket
            canvas.FillRect(36, 30, 6, 1, outfitDark);
            canvas.FillRect(36, 23, 1, 8, outfitDark);
            canvas.FillRect(41, 23, 1, 8, outfitDark);
            canvas.FillRect(36, 28, 6, 1, outfitDark);
            canvas.SetPixel(38, 27, Palette.Brass);
        }

        // ---------- Accessories ----------

        // The whistle hangs on a chain from the left shoulder strap. It is the brightest thing on
        // the figure on purpose: rule 3 asks the player to check that it is still there on the other side.
        // A false bekci keeps the chain, but nothing hangs on it.
        private void PaintWhistle()
        {
            Color32 chainLight = Palette.Rgb(226, 226, 214);
            Color32 chainDark = Palette.Rgb(140, 140, 150);
            int[] chainX = { 19, 19, 20, 20, 21, 22, 23 };
            for (int i = 0; i < chainX.Length; i++)
            {
                canvas.SetPixel(chainX[i], 35 - i, i % 2 == 0 ? chainLight : chainDark);
            }
            if (look.NoWhistle) return;

            canvas.FillRect(20, 22, 10, 7, Palette.Outline);
            canvas.FillRect(29, 24, 4, 4, Palette.Outline);
            canvas.FillRect(21, 23, 8, 5, Palette.Brass);                   // body
            canvas.FillRect(29, 25, 3, 2, Palette.Brass);                   // mouthpiece
            canvas.FillRect(22, 26, 5, 1, Palette.Rgb(255, 250, 200));      // shine
            canvas.FillRect(21, 23, 8, 1, Palette.Rgb(214, 160, 30));
            canvas.FillRect(24, 24, 2, 1, Palette.Outline);                 // sound hole
        }

        private void PaintScarf()
        {
            // Mustard, unless the coat is already yellowish.
            bool yellowCoat = outfit.r > 180 && outfit.g > 130 && outfit.b < 110;
            Color32 scarf = yellowCoat ? Palette.Rgb(70, 130, 140) : Palette.Scarf;
            Color32 scarfDark = Palette.Darker(scarf);

            canvas.FillRect(24, 38, 16, 5, scarf);
            canvas.FillRect(23, 39, 18, 3, scarf);
            canvas.FillRect(24, 38, 16, 1, scarfDark);
            for (int x = 25; x < 39; x += 3)
            {
                canvas.FillRect(x, 40, 2, 1, scarfDark); // folds
            }
            canvas.FillRect(23, 42, 18, 1, Palette.Lighter(scarf));

            canvas.FillRect(34, 27, 6, 11, scarf);       // hanging end
            canvas.FillRect(34, 27, 1, 11, scarfDark);
            canvas.FillRect(35, 30, 5, 1, scarfDark);
            canvas.SetPixel(35, 26, scarf);              // fringe
            canvas.SetPixel(37, 26, scarf);
            canvas.SetPixel(39, 26, scarf);
        }

        // Shoulder bag: strap across the chest, bag at the right hip.
        private void PaintBag()
        {
            Color32 leather = Palette.BagLeather;
            canvas.Line(18, 39, 45, 17, leather);
            canvas.Line(18, 38, 45, 16, leather);
            canvas.Line(19, 38, 46, 16, Palette.Darker(leather));

            canvas.FillRect(42, BaseRow, 12, 12, leather);
            canvas.FillRect(42, 20, 12, 4, Palette.Lighter(leather)); // flap
            canvas.FillRect(42, 19, 12, 1, Palette.Darker(leather));
            canvas.FillRect(52, BaseRow, 2, 12, Palette.Darker(leather));
            canvas.FillRect(47, 17, 2, 3, Palette.Brass);             // buckle
        }

        private void PaintGlasses()
        {
            Color32 frame = Palette.GlassesFrame;
            for (int eye = 0; eye < 2; eye++)
            {
                int x = (eye == 0 ? LeftEyeX : RightEyeX) - 1;
                canvas.FillRect(x, EyeRow - 2, 6, 1, frame);
                canvas.FillRect(x, EyeRow + 3, 6, 1, frame);
                canvas.FillRect(x, EyeRow - 2, 1, 6, frame);
                canvas.FillRect(x + 5, EyeRow - 2, 1, 6, frame);
                canvas.Blend(x + 1, EyeRow + 2, Palette.White, 0.5f); // glint on the lens
            }
            canvas.FillRect(30, EyeRow + 2, 4, 1, frame);  // bridge
            canvas.FillRect(22, EyeRow + 2, 2, 1, frame);  // arms
            canvas.FillRect(40, EyeRow + 2, 2, 1, frame);
        }

        // ---------- Head ----------

        private void PaintHead()
        {
            canvas.FillRect(20, 53, 2, 4, skin);       // ears
            canvas.FillRect(42, 53, 2, 4, skinDark);
            canvas.FillEllipse(CentreX, HeadY, 10, 11, skinDark);
            canvas.FillEllipse(CentreX - 1, HeadY + 0.5f, 9, 10.5f, skin);
        }

        private void PaintFace()
        {
            if (look.Staring)
            {
                // The face goes dark so that the eyes are all you see.
                canvas.FillEllipse(CentreX, HeadY, 10, 11, look.Hollow ? Palette.Void : Palette.Shade(skin, 0.5f));
            }

            FacePainter.PaintEyes(canvas, LeftEyeX, RightEyeX, EyeRow, look, false);
            if (look.Hollow) return;

            if (!look.NoEyes && !look.Staring && !look.WrongEyes)
            {
                Color32 brow = Palette.Shade(hair, Palette.Brightness(hair) > 0.5f ? 0.55f : 0.8f);
                canvas.FillRect(LeftEyeX, EyeRow + 4, 4, 1, brow);
                canvas.FillRect(RightEyeX, EyeRow + 4, 4, 1, brow);
            }

            if (look.TooManyTeeth)
            {
                FacePainter.PaintGrin(canvas, 24, 39, 45, true);
                return;
            }

            Color32 shade = look.Staring ? Palette.Shade(skin, 0.35f) : skinDark;
            canvas.FillRect(32, 52, 1, 3, shade);  // nose
            canvas.FillRect(31, 52, 1, 1, shade);
            canvas.FillRect(29, 48, 6, 1, Palette.Mix(Palette.Shade(skin, 0.5f), Palette.Rgb(150, 40, 50), 0.4f)); // mouth
            canvas.FillRect(30, 47, 4, 1, shade);
        }

        // Everything inside an ellipse around the head, from row fromY upwards, becomes hair (or a cap).
        private void PaintCap(Color32 color, int fromY, float radiusX, float radiusY)
        {
            Color32 dark = Palette.Darker(color);
            for (int y = fromY; y <= HeadY + radiusY + 2; y++)
            {
                for (int x = 16; x < 48; x++)
                {
                    float dx = (x - CentreX) / radiusX;
                    float dy = (y - (HeadY + 1)) / radiusY;
                    if (dx * dx + dy * dy <= 1f) canvas.SetPixel(x, y, x >= 38 ? dark : color);
                }
            }
        }

        private void PaintHair()
        {
            switch (look.HairStyle)
            {
                case 1: // short
                    PaintCap(hair, 62, 11, 11.5f);
                    PaintSideburns();
                    canvas.FillRect(26, 65, 6, 1, hairLight);
                    break;

                case 2: // long
                    PaintCap(hair, 62, 12, 12.5f);
                    canvas.FillRect(21, 60, 3, 2, hair);
                    canvas.FillRect(40, 60, 3, 2, hairDark);
                    canvas.FillRect(25, 66, 7, 1, hairLight);
                    break;

                case 3: // bun
                    PaintCap(hair, 62, 11, 11.5f);
                    PaintSideburns();
                    canvas.FillEllipse(CentreX, 70.5f, 5, 4, hairDark);
                    canvas.FillEllipse(CentreX - 1, 71, 4, 3.5f, hair);
                    canvas.FillRect(29, 72, 3, 1, hairLight);
                    canvas.FillRect(26, 65, 6, 1, hairLight);
                    break;

                case 4: // cap or hat
                    PaintSideburns();
                    if (look.OutfitStyle == 3) PaintUniformCap();
                    else PaintSoftCap();
                    break;

                default: // bald: just a shine on the scalp
                    if (!look.Hollow) canvas.FillRect(26, 63, 4, 2, Palette.Lighter(skin));
                    break;
            }
        }

        private void PaintSideburns()
        {
            canvas.FillRect(22, 58, 1, 4, hair);
            canvas.FillRect(41, 58, 1, 4, hairDark);
        }

        // Long hair falls in front of the shoulders on both sides of the face.
        private void PaintLongHairSides()
        {
            canvas.FillRect(19, 37, 5, 24, hair);
            canvas.FillRect(40, 37, 5, 24, hairDark);
            canvas.FillRect(20, 36, 3, 1, hair);
            canvas.FillRect(41, 36, 3, 1, hairDark);
            canvas.FillRect(20, 40, 1, 18, hairLight);
        }

        // An everyday cap in a darker tone of the jacket, peak towards the player.
        private void PaintSoftCap()
        {
            Color32 cap = Palette.Shade(outfit, 0.72f);
            PaintCap(cap, 63, 11, 11.5f);
            canvas.FillRect(20, 62, 24, 2, Palette.Darker(cap));  // peak
            canvas.FillRect(21, 63, 22, 1, Palette.Lighter(cap));
            canvas.FillRect(31, 68, 2, 1, Palette.Lighter(cap));  // button on top
        }

        // The bekci's peaked cap with a brass badge.
        private void PaintUniformCap()
        {
            canvas.FillRect(20, 61, 24, 2, Palette.Rgb(22, 22, 34));    // shiny peak
            canvas.FillRect(22, 62, 8, 1, Palette.Rgb(90, 90, 110));
            canvas.FillRect(21, 63, 22, 2, outfitDark);                 // band
            canvas.FillRect(20, 65, 24, 1, outfit);                     // crown, wider at the top
            canvas.FillRect(19, 66, 26, 3, outfit);
            canvas.FillRect(20, 69, 24, 1, outfit);
            canvas.FillRect(22, 70, 20, 1, outfitLight);
            canvas.FillRect(41, 65, 3, 4, outfitDark);
            canvas.FillRect(30, 63, 4, 4, Palette.Brass);               // badge
            canvas.FillRect(31, 65, 2, 1, Palette.White);
        }

        // ---------- Anomaly: extra fingers ----------

        // A forearm raised beside the head, palm towards the player, with seven long fingers.
        private void PaintRaisedHand()
        {
            Color32 sleeve = look.OutfitStyle == 0 ? skin : outfit;
            Color32 sleeveDark = look.OutfitStyle == 0 ? skinDark : outfitDark;
            canvas.FillRect(49, 22, 7, 17, sleeve);
            canvas.FillRect(54, 22, 2, 17, sleeveDark);
            canvas.FillRect(49, 38, 7, 1, sleeveDark);

            canvas.FillRect(47, 39, 13, 7, skin);    // palm
            canvas.FillRect(47, 39, 13, 1, skinDark);
            canvas.FillRect(50, 42, 6, 1, skinDark); // crease

            int[] lengths = { 6, 10, 12, 13, 12, 10, 6 };
            for (int finger = 0; finger < lengths.Length; finger++)
            {
                int x = 47 + finger * 2;
                canvas.FillRect(x, 46, 1, lengths[finger], skin);
                canvas.SetPixel(x, 46 + lengths[finger] - 1, Palette.Lighter(skin)); // nail
            }
        }
    }
}
