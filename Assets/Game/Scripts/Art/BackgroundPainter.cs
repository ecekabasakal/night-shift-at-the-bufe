using UnityEngine;

namespace NightShift
{
    // The versions of the kiosk picture that exist.
    public enum KioskScene
    {
        Night,      // the normal view during the shift
        OtherSide,  // the same shop while Shift is held
        Morning,    // ending: morning light, your reflection in the glass
        Changed,    // ending: morning light, but the reflection is not quite you
        Seen,       // ending: the other side looks back
        Fired       // ending: lights off, shutter down
    }

    // Paints the kiosk seen from behind the counter (240 x 135 art pixels).
    // From back to front: night street behind the glass, walls and glass frames, shelves (left),
    // ayran fridge (right), floor, ceiling with the fluorescent tube, and the counter along the bottom.
    // The middle of the counter is kept empty: that is where the customer and its shadow go.
    public class BackgroundPainter
    {
        private const int Width = GameLayout.BackgroundArtWidth;
        private const int Height = GameLayout.BackgroundArtHeight;

        // Heights (art pixels from the bottom).
        private const int CounterTop = 54;   // first row above the counter surface
        private const int WallBase = 62;     // the floor is the strip between the counter and the wall
        private const int CeilingBase = 112;
        private const int ItemBase = 46;     // where the things on the counter stand

        // The glass front.
        private const int GlassLeft = 50;
        private const int GlassRight = 189;
        private const int PaneLeft = 54, PaneRight = 116, PaneBottom = 66;
        private const int DoorLeft = 124, DoorRight = 185, DoorBottom = 64;
        private const int GlassTop = 107;

        private const int LampX = 160;
        private const int StreetCatX = 71;
        private const int StreetCatGround = 66;  // the cat sits on the bottom edge of the window pane
        public const int StreetCatAlarmFrameCount = 2;
        private const int FridgeLeft = 194, FridgeRight = 234, FridgeBottom = 56, FridgeTop = 110;

        private readonly PixelCanvas canvas = new PixelCanvas(Width, Height);
        private readonly KioskScene scene;
        private readonly bool tubeOn;
        private readonly bool morning;

        // Colour of the light inside the shop (1,1,1 = tube on).
        private float lightR = 1f, lightG = 1f, lightB = 1f;

        // False while the normal shop is painted; true once the picture has been turned into the other side.
        private bool otherSide;

        private BackgroundPainter(KioskScene scene, bool tubeOn)
        {
            this.scene = scene;
            morning = scene == KioskScene.Morning || scene == KioskScene.Changed;
            this.tubeOn = tubeOn && !morning && scene != KioskScene.Fired;
        }

        // tubeOn = false gives the "flicker" frame (tube dark for a moment).
        public static PixelCanvas Paint(KioskScene scene, bool tubeOn)
        {
            BackgroundPainter painter = new BackgroundPainter(scene, tubeOn);
            painter.PaintAll();
            return painter.canvas;
        }

        private void PaintAll()
        {
            ChooseLight();

            PaintStreet();
            if (morning) PaintReflection(scene == KioskScene.Changed);
            PaintGlassShine();
            if (scene == KioskScene.Fired) PaintShutter();

            PaintWallsAndFrames();
            PaintShelves();
            PaintFloor();
            PaintFridge();
            PaintCeilingAndTube();

            // The counter is painted last. On the other side it is painted after the colour swap,
            // in a pale tone of its own, so that shadows on it are impossible to miss.
            if (scene == KioskScene.OtherSide || scene == KioskScene.Seen) TurnIntoOtherSide();
            PaintCounter();
            if (otherSide) PaintCounterItemShadows();
            if (morning) PaintSunbeams();
            if (scene == KioskScene.Seen) PaintWatchers();
        }

        private void ChooseLight()
        {
            if (morning)
            {
                lightR = 1.1f; lightG = 0.98f; lightB = 0.84f;
            }
            else if (scene == KioskScene.Fired)
            {
                lightR = 0.34f; lightG = 0.38f; lightB = 0.56f;
            }
            else if (!tubeOn)
            {
                lightR = 0.52f; lightG = 0.56f; lightB = 0.68f;
            }
        }

        // An interior colour as it looks under the current shop light.
        private Color32 Lit(Color32 color)
        {
            return Shown(Palette.Multiply(color, lightR, lightG, lightB));
        }

        // A colour as it goes onto the canvas: unchanged, or swapped once we paint the other side.
        private Color32 Shown(Color32 color)
        {
            return otherSide ? Palette.OtherSide(color) : color;
        }

        // ---------- Street (seen through the glass) ----------

        private void PaintStreet()
        {
            Color32 skyTop = morning ? Palette.DawnTop : Palette.SkyTop;
            Color32 skyLow = morning ? Palette.DawnLow : Palette.SkyLow;
            Color32 road = morning ? Palette.DawnRoad : Palette.Road;
            Color32 pavement = morning ? Palette.DawnPavement : Palette.Pavement;

            for (int y = WallBase; y < CeilingBase; y++)
            {
                Color32 color;
                if (y >= 84) color = Palette.Mix(skyLow, skyTop, (y - 84) / 26f);
                else if (y >= 81) color = Palette.Shade(pavement, 0.8f);
                else if (y >= 73) color = road;
                else if (y == 72) color = morning ? Palette.White : Palette.Kerb;
                else color = pavement;
                canvas.FillRect(GlassLeft, y, GlassRight - GlassLeft + 1, 1, color);
            }

            if (morning) canvas.Glow(150, 90, 46, 26, Palette.Sunlight, 0.8f);
            else PaintStars();

            PaintBuildings();
            PaintStreetLamp();
            PaintStreetCat();
        }

        private void PaintStars()
        {
            SeededNumbers numbers = new SeededNumbers(77);
            for (int i = 0; i < 16; i++)
            {
                int x = numbers.Next(GlassLeft, GlassRight);
                int y = numbers.Next(99, 108);
                canvas.SetPixel(x, y, Palette.Mix(Palette.SkyLow, Palette.White, 0.3f + 0.4f * numbers.NextFloat()));
            }
        }

        private void PaintBuildings()
        {
            SeededNumbers numbers = new SeededNumbers(2026);
            Color32 far = morning ? Palette.DawnBuildingFar : Palette.BuildingFar;
            Color32 near = morning ? Palette.DawnBuilding : Palette.Building;

            // Far row: a plain skyline.
            for (int x = GlassLeft; x <= GlassRight;)
            {
                int width = numbers.Next(9, 18);
                int top = numbers.Next(97, 107);
                canvas.FillRect(x, 84, width, top - 84, far);
                x += width;
            }

            // Near row: lower blocks with windows.
            for (int x = GlassLeft; x <= GlassRight;)
            {
                int width = numbers.Next(12, 22);
                int top = numbers.Next(89, 98);
                canvas.FillRect(x, 84, width - 1, top - 84, near);
                canvas.FillRect(x, top - 1, width - 1, 1, Palette.Lighter(near));

                for (int wy = 86; wy < top - 3; wy += 4)
                {
                    for (int wx = x + 2; wx < x + width - 3; wx += 4)
                    {
                        float chance = numbers.NextFloat();
                        Color32 window = Palette.Shade(near, morning ? 0.7f : 1.5f);
                        if (!morning && chance < 0.14f) window = Palette.WindowWarm;
                        else if (!morning && chance < 0.19f) window = Palette.WindowTv;
                        canvas.FillRect(wx, wy, 2, 2, window);
                    }
                }
                x += width;
            }
        }

        private void PaintStreetLamp()
        {
            bool lampOn = !morning;

            if (lampOn)
            {
                // Cone of light, wider towards the ground, and a pool on the pavement.
                for (int y = 100; y >= WallBase; y--)
                {
                    int half = 2 + (int)((100 - y) * 0.45f);
                    canvas.BlendRect(LampX - half, y, half * 2 + 1, 1, Palette.Sodium, 0.2f);
                }
                canvas.BlendEllipse(LampX, 67, 21, 5, Palette.Sodium, 0.35f);
                canvas.Glow(LampX, 101, 13, 10, Palette.Sodium, 0.7f);
            }

            Color32 pole = morning ? Palette.Rgb(90, 84, 100) : Palette.Rgb(64, 56, 86);
            canvas.FillRect(LampX + 10, WallBase, 2, 42, pole);          // pole
            canvas.FillRect(LampX - 2, 103, 14, 1, pole);                // arm
            canvas.FillRect(LampX - 4, 102, 8, 1, pole);                 // lamp hood
            canvas.FillRect(LampX - 3, 100, 6, 2, lampOn ? Palette.SodiumCore : Palette.Rgb(170, 170, 176));
        }

        // A cat sitting on the pavement outside the window, lit from the lamp side.
        private void PaintStreetCat()
        {
            const int x = StreetCatX;
            const int ground = StreetCatGround;
            Color32 fur = morning ? Palette.Rgb(70, 62, 70) : Palette.StreetCat;
            Color32 rim = Palette.Mix(fur, morning ? Palette.Sunlight : Palette.Sodium, 0.55f);

            canvas.FillEllipse(x, ground + 4, 4f, 4.5f, fur);            // body
            canvas.FillEllipse(x, ground + 10, 3.2f, 2.6f, fur);         // head
            canvas.FillRect(x - 3, ground + 12, 2, 2, fur);              // ears
            canvas.FillRect(x + 2, ground + 12, 2, 2, fur);
            canvas.SetPixel(x - 3, ground + 14, fur);
            canvas.SetPixel(x + 3, ground + 14, fur);
            canvas.Line(x + 4, ground, x + 8, ground, fur);              // tail
            canvas.Line(x + 8, ground, x + 9, ground + 3, fur);

            canvas.Line(x + 4, ground + 2, x + 4, ground + 6, rim);
            canvas.SetPixel(x + 3, ground + 10, rim);
            canvas.SetPixel(x + 3, ground + 13, rim);

            if (!morning)
            {
                canvas.SetPixel(x - 1, ground + 10, Palette.CatEye);
                canvas.SetPixel(x + 1, ground + 10, Palette.CatEye);
            }
        }

        // The same cat with its back up, hissing: an empty canvas with only the cat on it, to lay over the
        // background. It is bigger than the calm cat on every side, so the calm one disappears behind it.
        // The two frames differ in where the fur stands on end.
        public static PixelCanvas PaintStreetCatAlarm(bool otherSide, int frame)
        {
            const int x = StreetCatX;
            const int ground = StreetCatGround;
            PixelCanvas canvas = new PixelCanvas(Width, Height);
            Color32 fur = Palette.StreetCat;
            Color32 rim = Palette.Mix(fur, Palette.Sodium, 0.55f);
            int bristle = frame % 2;

            canvas.FillEllipse(x, ground + 5, 6.5f, 6.5f, fur);          // arched body
            canvas.FillEllipse(x, ground + 13, 4.4f, 3.4f, fur);         // head
            canvas.FillRect(x - 7, ground + 14, 3, 2, fur);              // ears, flat to the sides
            canvas.FillRect(x + 5, ground + 14, 3, 2, fur);
            canvas.FillRect(x + 4, ground, 7, 4, fur);                   // tail root

            // Tail straight up, thick as a bottle brush.
            for (int y = ground; y <= ground + 17; y++)
            {
                int half = (y >= ground + 5 && y <= ground + 15) ? 2 : 1;
                canvas.FillRect(x + 9 - half, y, half * 2 + 1, 1, fur);
                if (y >= ground + 4 && (y + bristle) % 2 == 0)
                {
                    canvas.SetPixel(x + 8 - half, y, fur);
                    canvas.SetPixel(x + 10 + half, y, rim);
                }
            }

            // Fur standing on end along the back.
            for (int i = 0; i < 12; i++)
            {
                float angle = Mathf.PI * (i + 0.5f * bristle) / 11.5f;
                int px = Mathf.RoundToInt(x + 7.6f * Mathf.Cos(angle));
                int py = Mathf.RoundToInt(ground + 5 + 7.6f * Mathf.Sin(angle));
                canvas.SetPixel(px, py, px > x + 2 ? rim : fur);
            }
            canvas.FillRect(0, 0, Width, ground, Palette.Clear);         // nothing below the pane

            canvas.Line(x + 6, ground + 2, x + 6, ground + 7, rim);
            canvas.Line(x + 11, ground + 1, x + 11, ground + 4, rim);
            canvas.SetPixel(x + 4, ground + 13, rim);
            canvas.SetPixel(x + 7, ground + 15, rim);

            canvas.FillRect(x - 3, ground + 13, 2, 2, Palette.CatEye);   // eyes wide open
            canvas.FillRect(x + 2, ground + 13, 2, 2, Palette.CatEye);
            canvas.FillRect(x - 1, ground + 10 - bristle, 3, 2 + bristle, Palette.MouthDark);
            canvas.SetPixel(x - 1, ground + 11, Palette.Tooth);
            canvas.SetPixel(x + 1, ground + 11, Palette.Tooth);

            if (otherSide)
            {
                for (int i = 0; i < canvas.Pixels.Length; i++)
                {
                    canvas.Pixels[i] = Palette.OtherSide(canvas.Pixels[i]);
                }
            }
            return canvas;
        }

        // The player's own reflection in the window (ending pictures only).
        // On the "changed" ending it is a dark shape with two small lights for eyes.
        private void PaintReflection(bool other)
        {
            const float x = 88.5f;
            Color32 color = other ? Palette.Void : Palette.Rgb(244, 232, 220);
            float alpha = other ? 0.8f : 0.34f;
            float headY = other ? 93f : 91f; // the neck is a little too long

            canvas.BlendEllipse(x, 66, 15, 13, color, alpha);        // shoulders
            canvas.BlendRect(86, 78, 6, (int)headY - 84, color, alpha); // neck
            canvas.BlendEllipse(x, headY, 6.5f, 7.5f, color, alpha); // head

            if (other)
            {
                canvas.Glow(86.5f, headY + 1, 4, 4, Palette.StareGlow, 0.7f);
                canvas.Glow(91.5f, headY + 1, 4, 4, Palette.StareGlow, 0.7f);
                canvas.FillRect(86, (int)headY + 1, 2, 1, Palette.StareEye);
                canvas.FillRect(91, (int)headY + 1, 2, 1, Palette.StareEye);
            }
        }

        // Faint diagonal streaks so the panes read as glass.
        private void PaintGlassShine()
        {
            float alpha = morning ? 0.16f : 0.07f;
            for (int y = WallBase; y < CeilingBase; y++)
            {
                for (int x = GlassLeft; x <= GlassRight; x++)
                {
                    int diagonal = (x + y) % 46;
                    if (diagonal < 5 || diagonal == 9) canvas.Blend(x, y, Palette.White, alpha);
                }
            }
        }

        // Roller shutter pulled down over the glass (the "fired" ending).
        private void PaintShutter()
        {
            for (int y = 70; y < CeilingBase; y++)
            {
                Color32 slat = (y % 3 == 0) ? Lit(Palette.MetalDark) : Lit(Palette.Metal);
                if (y == 70 || y == 71) slat = Lit(Palette.Frame);
                canvas.FillRect(GlassLeft, y, GlassRight - GlassLeft + 1, 1, slat);
            }
        }

        // ---------- Shop ----------

        private void PaintWallsAndFrames()
        {
            // Solid wall left and right of the glass front.
            canvas.FillRect(0, WallBase, GlassLeft, CeilingBase - WallBase, Lit(Palette.Wall));
            canvas.FillRect(GlassRight + 1, WallBase, Width - GlassRight - 1, CeilingBase - WallBase, Lit(Palette.Wall));
            canvas.FillRect(0, WallBase, GlassLeft, 1, Lit(Palette.WallDark));
            canvas.FillRect(GlassRight + 1, WallBase, Width - GlassRight - 1, 1, Lit(Palette.WallDark));

            // Frame bars around the window pane and the door glass.
            Color32 frame = Lit(Palette.Frame);
            Color32 edge = Lit(Palette.FrameLight);
            canvas.FillRect(GlassLeft, WallBase, PaneLeft - GlassLeft, CeilingBase - WallBase, frame);          // left post
            canvas.FillRect(DoorRight + 1, WallBase, GlassRight - DoorRight, CeilingBase - WallBase, frame);    // right post
            canvas.FillRect(PaneRight + 1, WallBase, DoorLeft - PaneRight - 1, CeilingBase - WallBase, frame);  // middle post
            canvas.FillRect(GlassLeft, GlassTop + 1, GlassRight - GlassLeft + 1, CeilingBase - GlassTop - 1, frame); // top beam
            canvas.FillRect(GlassLeft, WallBase, PaneRight - GlassLeft + 1, PaneBottom - WallBase, frame);      // window sill
            canvas.FillRect(DoorLeft, WallBase, DoorRight - DoorLeft + 1, DoorBottom - WallBase, frame);        // door threshold

            canvas.FillRect(PaneLeft, PaneBottom - 1, PaneRight - PaneLeft + 1, 1, edge);
            canvas.FillRect(PaneLeft - 1, PaneBottom, 1, GlassTop - PaneBottom + 1, edge);
            canvas.FillRect(DoorLeft - 1, DoorBottom, 1, GlassTop - DoorBottom + 1, edge);
            canvas.FillRect(DoorLeft, DoorBottom - 1, DoorRight - DoorLeft + 1, 1, edge);

            // Door handle.
            canvas.FillRect(DoorLeft + 3, 78, 2, 15, Lit(Palette.Metal));
            canvas.FillRect(DoorLeft + 3, 92, 2, 1, Lit(Palette.White));
        }

        // Shelf unit on the left wall with rows of small packets.
        private void PaintShelves()
        {
            SeededNumbers numbers = new SeededNumbers(1923);
            canvas.FillRect(2, WallBase, 45, 48, Lit(Palette.WallDark));

            for (int board = 64; board <= 106; board += 14)
            {
                canvas.FillRect(1, board, 47, 2, Lit(Palette.ShelfBoard));
                canvas.FillRect(1, board + 1, 47, 1, Lit(Palette.Lighter(Palette.ShelfBoard)));
                if (board == 106) break; // the top board carries nothing

                for (int x = 3; x < 43;)
                {
                    int width = numbers.Next(3, 6);
                    int height = numbers.Next(5, 11);
                    Color32 color = Palette.Products[numbers.Next(0, Palette.Products.Length)];
                    PaintPacket(x, board + 2, width, height, color, numbers.Next(0, 3) == 0);
                    x += width + 1;
                }
            }
        }

        // One product: a block with a light left edge, a dark right edge and maybe a label.
        private void PaintPacket(int x, int y, int width, int height, Color32 color, bool label)
        {
            canvas.FillRect(x, y, width, height, Lit(color));
            canvas.FillRect(x, y, 1, height, Lit(Palette.Lighter(color)));
            canvas.FillRect(x + width - 1, y, 1, height, Lit(Palette.Darker(color)));
            if (label && height > 5) canvas.FillRect(x + 1, y + height / 2, width - 2, 2, Lit(Palette.Rgb(236, 232, 214)));
        }

        // Checkerboard tiles between the counter and the wall.
        private void PaintFloor()
        {
            for (int y = CounterTop; y < WallBase; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    bool dark = ((x / 8) + ((y - CounterTop) / 4)) % 2 == 0;
                    canvas.SetPixel(x, y, Lit(dark ? Palette.FloorLine : Palette.Floor));
                }
            }
        }

        // The ayran fridge: it has its own light, so it glows even when the tube flickers.
        private void PaintFridge()
        {
            bool glowing = scene != KioskScene.Fired;
            int width = FridgeRight - FridgeLeft + 1;

            if (glowing) canvas.Glow(FridgeLeft + width / 2, 82, 46, 50, Palette.FridgeGlow, morning ? 0.15f : 0.4f);

            canvas.FillRect(FridgeLeft, FridgeBottom, width, FridgeTop - FridgeBottom + 1, Lit(Palette.FridgeBody));
            canvas.FillRect(FridgeRight - 1, FridgeBottom, 2, FridgeTop - FridgeBottom + 1, Lit(Palette.Darker(Palette.FridgeBody)));
            canvas.FillRect(FridgeLeft, FridgeBottom, width, 2, Lit(Palette.MetalDark));

            // Header with the word AYRAN.
            canvas.FillRect(FridgeLeft + 2, 103, width - 4, 6, Lit(Palette.FridgeBlue));
            PaintTinyWord("AYRAN", FridgeLeft + 11, 104, glowing ? Palette.White : Lit(Palette.White));

            // Glass door with the lit inside.
            Color32 inside = glowing ? Palette.FridgeGlow : Lit(Palette.Metal);
            canvas.FillRect(FridgeLeft + 3, 59, width - 7, 43, Lit(Palette.MetalDark));
            canvas.FillRect(FridgeLeft + 4, 60, width - 9, 41, inside);
            canvas.FillRect(FridgeLeft + 4, 60, width - 9, 3, Palette.Mix(inside, Palette.FridgeBlue, 0.25f));

            for (int shelf = 60; shelf <= 88; shelf += 14)
            {
                if (shelf > 60) canvas.FillRect(FridgeLeft + 4, shelf - 1, width - 9, 1, Palette.Mix(inside, Palette.MetalDark, 0.6f));
                for (int x = FridgeLeft + 6; x + 4 <= FridgeRight - 5; x += 6)
                {
                    PaintAyranCup(x, shelf, glowing);
                }
            }

            canvas.FillRect(FridgeLeft + 1, 74, 2, 16, Lit(Palette.Frame)); // handle
        }

        private void PaintAyranCup(int x, int y, bool glowing)
        {
            Color32 white = glowing ? Palette.White : Lit(Palette.White);
            Color32 blue = glowing ? Palette.FridgeBlue : Lit(Palette.FridgeBlue);
            canvas.FillRect(x, y, 4, 8, white);
            canvas.FillRect(x, y + 3, 4, 2, blue);
            canvas.FillRect(x, y + 8, 4, 1, Palette.Mix(white, blue, 0.4f));
            canvas.FillRect(x + 3, y, 1, 8, Palette.Mix(white, blue, 0.3f));
        }

        // 3 x 5 pixel capital letters; only the letters the game needs.
        private void PaintTinyWord(string word, int x, int y, Color32 color)
        {
            for (int i = 0; i < word.Length; i++)
            {
                string rows = TinyLetter(word[i]); // 5 rows of 3, top row first
                for (int row = 0; row < 5; row++)
                {
                    for (int column = 0; column < 3; column++)
                    {
                        if (rows[row * 3 + column] == '#') canvas.SetPixel(x + i * 4 + column, y + 4 - row, color);
                    }
                }
            }
        }

        private static string TinyLetter(char letter)
        {
            switch (letter)
            {
                case 'A': return ".#." + "#.#" + "###" + "#.#" + "#.#";
                case 'Y': return "#.#" + "#.#" + ".#." + ".#." + ".#.";
                case 'R': return "##." + "#.#" + "##." + "#.#" + "#.#";
                case 'N': return "#.#" + "###" + "###" + "#.#" + "#.#";
                default: return "###" + "###" + "###" + "###" + "###";
            }
        }

        private void PaintCeilingAndTube()
        {
            canvas.FillRect(0, CeilingBase, Width, Height - CeilingBase, Lit(Palette.Ceiling));
            canvas.FillRect(0, CeilingBase, Width, 1, Lit(Palette.Frame));

            if (tubeOn)
            {
                canvas.Glow(120, 117, 96, 30, Palette.TubeGlow, 0.5f);
                canvas.Glow(120, 117, 50, 9, Palette.Tube, 0.5f);
            }

            canvas.FillRect(76, 119, 89, 3, Lit(Palette.Metal));           // housing
            canvas.FillRect(78, 116, 2, 3, Lit(Palette.MetalDark));        // end caps
            canvas.FillRect(161, 116, 2, 3, Lit(Palette.MetalDark));
            canvas.FillRect(80, 116, 81, 3, tubeOn ? Palette.Tube : Lit(Palette.TubeOff));
            if (tubeOn) canvas.FillRect(80, 117, 81, 1, Palette.White);
        }

        private void PaintCounter()
        {
            Color32 surface = otherSide ? Palette.OtherCounter : Lit(Palette.Counter);
            Color32 surfaceLight = otherSide ? Palette.OtherCounterLight : Lit(Palette.CounterLight);
            Color32 surfaceDark = otherSide ? Palette.Shade(Palette.OtherCounter, 0.85f) : Lit(Palette.CounterDark);

            canvas.FillRect(0, 0, Width, CounterTop, surface);
            canvas.FillRect(0, CounterTop - 1, Width, 1, surfaceLight);
            canvas.FillRect(0, CounterTop - 2, Width, 1, Palette.Mix(surface, surfaceLight, 0.5f));

            // A few worn streaks.
            SeededNumbers numbers = new SeededNumbers(404);
            for (int i = 0; i < 14; i++)
            {
                int x = numbers.Next(0, Width - 30);
                int y = numbers.Next(2, CounterTop - 4);
                int length = numbers.Next(8, 30);
                canvas.BlendRect(x, y, length, 1, i % 3 == 0 ? surfaceDark : surfaceLight, 0.35f);
            }

            PaintPacketRack();
            PaintTeaGlass();
            PaintSimitTray();
            PaintCashRegister();
        }

        // Display stand with gum and sunflower-seed packets (left end of the counter).
        private void PaintPacketRack()
        {
            SeededNumbers numbers = new SeededNumbers(31);
            canvas.FillRect(6, ItemBase, 35, 2, Lit(Palette.Metal));
            canvas.FillRect(8, ItemBase + 2, 31, 27, Lit(Palette.MetalDark));
            canvas.FillRect(8, ItemBase + 28, 31, 1, Lit(Palette.Metal));

            for (int row = 0; row < 3; row++)
            {
                int y = ItemBase + 3 + row * 9;
                canvas.FillRect(8, y - 1, 31, 1, Lit(Palette.Metal));
                for (int x = 10; x < 36; x += 6)
                {
                    Color32 color = Palette.Products[numbers.Next(0, Palette.Products.Length)];
                    PaintPacket(x, y, 5, 6, color, true);
                }
            }
        }

        // A thin-waisted glass of tea on a saucer.
        private void PaintTeaGlass()
        {
            const int x = 58;
            Color32 glass = Lit(Palette.Rgb(220, 232, 226));
            Color32 tea = Lit(Palette.Tea);
            canvas.FillRect(x - 2, ItemBase, 9, 1, Lit(Palette.White));
            canvas.FillRect(x - 1, ItemBase + 1, 7, 1, glass);
            canvas.FillRect(x, ItemBase + 2, 5, 3, tea);
            canvas.FillRect(x + 1, ItemBase + 5, 3, 2, tea);
            canvas.FillRect(x, ItemBase + 7, 5, 2, tea);
            canvas.FillRect(x, ItemBase + 9, 5, 1, glass);
            canvas.SetPixel(x, ItemBase + 3, Lit(Palette.Lighter(Palette.Tea)));
            canvas.SetPixel(x, ItemBase + 8, Lit(Palette.Lighter(Palette.Tea)));
        }

        // A tray with a small stack of simit rings.
        private void PaintSimitTray()
        {
            Color32 crust = Lit(Palette.Rgb(196, 122, 52));
            Color32 hole = Lit(Palette.Rgb(110, 62, 30));
            canvas.FillRect(160, ItemBase, 27, 2, Lit(Palette.Metal));
            canvas.FillRect(160, ItemBase + 1, 27, 1, Lit(Palette.White));

            for (int i = 0; i < 3; i++)
            {
                float x = 173.5f + (i == 1 ? 2 : 0);
                float y = ItemBase + 4 + i * 3;
                canvas.FillEllipse(x, y, 8, 2.6f, Lit(Palette.Darker(Palette.Rgb(196, 122, 52))));
                canvas.FillEllipse(x, y + 0.6f, 8, 2.2f, crust);
                canvas.FillRect((int)x - 2, (int)y + 1, 5, 1, hole);
                canvas.SetPixel((int)x - 5, (int)y + 1, Lit(Palette.Rgb(240, 214, 150)));
                canvas.SetPixel((int)x + 5, (int)y, Lit(Palette.Rgb(240, 214, 150)));
            }
        }

        private void PaintCashRegister()
        {
            canvas.FillRect(198, ItemBase, 36, 15, Lit(Palette.MetalDark));
            canvas.FillRect(198, ItemBase + 14, 36, 1, Lit(Palette.Metal));
            canvas.FillRect(198, ItemBase, 36, 1, Lit(Palette.Frame));
            for (int y = ItemBase + 3; y < ItemBase + 13; y += 4)
            {
                for (int x = 202; x < 230; x += 4)
                {
                    canvas.FillRect(x, y, 2, 2, Lit(Palette.Metal));
                }
            }

            // Display: its own green light.
            canvas.FillRect(206, ItemBase + 15, 22, 10, Lit(Palette.Frame));
            canvas.FillRect(208, ItemBase + 17, 18, 6, Shown(Palette.Rgb(18, 52, 40)));
            for (int x = 214; x < 225; x += 4)
            {
                canvas.FillRect(x, ItemBase + 18, 2, 4, Shown(Palette.Rgb(120, 255, 170)));
            }
        }

        // Morning light falling in through the glass onto the floor and the counter.
        private void PaintSunbeams()
        {
            for (int y = WallBase - 1; y >= 0; y--)
            {
                int slant = (int)((WallBase - y) * 0.7f);
                canvas.BlendRect(PaneLeft - slant, y, PaneRight - PaneLeft + 1, 1, Palette.Sunlight, 0.2f);
                canvas.BlendRect(DoorLeft - slant, y, DoorRight - DoorLeft + 1, 1, Palette.Sunlight, 0.2f);
            }
        }

        // ---------- The other side ----------

        // Swaps every pixel painted so far; from now on Lit() and Shown() swap new colours too.
        private void TurnIntoOtherSide()
        {
            otherSide = true;
            for (int i = 0; i < canvas.Pixels.Length; i++)
            {
                canvas.Pixels[i] = Palette.OtherSide(canvas.Pixels[i]);
            }
        }

        // On the other side everything real has a shadow that leans away from the street lamp.
        private void PaintCounterItemShadows()
        {
            PaintCounterShadow(6, 40);     // packet rack
            PaintCounterShadow(56, 64);    // tea glass
            PaintCounterShadow(160, 186);  // simit tray
            PaintCounterShadow(198, 233);  // cash register
        }

        private void PaintCounterShadow(int left, int right)
        {
            for (int row = 1; row <= ShadowPainter.CounterShadowRows; row++)
            {
                int shift = (int)(row * ShadowPainter.LeanPerRow);
                float alpha = ShadowPainter.AlphaAt(row);
                canvas.BlendRect(left - shift, ItemBase - row, right - left + 1, 1, Palette.ShadowInk, alpha);
            }
        }

        // The "seen" ending: lights in the dark outside, and something tall right at the counter.
        private void PaintWatchers()
        {
            SeededNumbers numbers = new SeededNumbers(606);
            for (int i = 0; i < 12; i++)
            {
                int x = numbers.Next(PaneLeft + 2, DoorRight - 6);
                int y = numbers.Next(70, 104);
                if (x > 92 && x < 146) continue; // hidden behind the tall one
                PaintEyePair(x, y, 1);
            }

            PixelCanvas figure = new PixelCanvas(Width, Height - CounterTop);
            figure.FillEllipse(119.5f, 4, 34, 30, Palette.Void);    // shoulders
            figure.FillRect(113, 30, 14, 12, Palette.Void);          // neck
            figure.FillEllipse(119.5f, 50, 12, 15, Palette.Void);    // head
            figure.Outline(Palette.Rgb(120, 30, 110));
            canvas.Blit(figure, 0, CounterTop);
            PaintEyePair(112, CounterTop + 52, 2);
        }

        private void PaintEyePair(int x, int y, int size)
        {
            int gap = size == 1 ? 3 : 9;
            for (int eye = 0; eye < 2; eye++)
            {
                int ex = x + eye * gap;
                canvas.Glow(ex + size, y + size * 0.5f, 3 + size * 2, 3 + size * 2, Palette.StareGlow, 0.7f);
                canvas.FillRect(ex, y, size * 2, size, Palette.StareEye);
            }
        }
    }
}
