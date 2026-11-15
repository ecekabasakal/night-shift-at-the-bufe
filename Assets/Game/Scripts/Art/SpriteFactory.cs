using System.Collections.Generic;
using UnityEngine;

namespace NightShift
{
    // Turns the painters' pixel canvases into Unity sprites and remembers them:
    // asking twice for the same picture returns the same sprites, so "Play again" creates nothing new.
    public class SpriteFactory : ISpriteFactory
    {
        private static readonly Vector2 PivotCentre = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 PivotBottomCentre = new Vector2(0.5f, 0f);

        private readonly Dictionary<string, Sprite[]> frameCache = new Dictionary<string, Sprite[]>();
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        // Frame 0 = tube on, frame 1 = tube dark (flicker).
        public Sprite[] CreateBackgroundFrames(bool shiftView)
        {
            string key = shiftView ? "background-shift" : "background-normal";
            Sprite[] frames;
            if (frameCache.TryGetValue(key, out frames)) return frames;

            KioskScene scene = shiftView ? KioskScene.OtherSide : KioskScene.Night;
            frames = new Sprite[2];
            frames[0] = ToWorldSprite(BackgroundPainter.Paint(scene, true), PivotCentre, key + "-on");
            frames[1] = ToWorldSprite(BackgroundPainter.Paint(scene, false), PivotCentre, key + "-flicker");
            frameCache[key] = frames;
            return frames;
        }

        public Sprite[] CreateCustomerFrames(CustomerData customer, bool shiftView)
        {
            CustomerLook look = CustomerLook.From(customer, shiftView);
            string key = "customer-" + look.CacheKey();
            Sprite[] frames;
            if (frameCache.TryGetValue(key, out frames)) return frames;

            frames = new Sprite[CustomerPainter.FrameCount(look)];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = ToWorldSprite(CustomerPainter.Paint(look, i), PivotBottomCentre, key + "-" + i);
            }
            frameCache[key] = frames;
            return frames;
        }

        public Sprite[] CreateStreetCatAlarmFrames(bool shiftView)
        {
            string key = shiftView ? "streetcat-alarm-shift" : "streetcat-alarm-normal";
            Sprite[] frames;
            if (frameCache.TryGetValue(key, out frames)) return frames;

            frames = new Sprite[BackgroundPainter.StreetCatAlarmFrameCount];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = ToWorldSprite(BackgroundPainter.PaintStreetCatAlarm(shiftView, i), PivotCentre, key + "-" + i);
            }
            frameCache[key] = frames;
            return frames;
        }

        public Sprite CreateEndingBackground(EndingId ending)
        {
            string key = "ending-" + ending;
            Sprite sprite;
            if (spriteCache.TryGetValue(key, out sprite)) return sprite;

            sprite = ToWorldSprite(BackgroundPainter.Paint(SceneFor(ending), false), PivotCentre, key);
            spriteCache[key] = sprite;
            return sprite;
        }

        public Sprite CreateVignette()
        {
            Sprite sprite;
            if (spriteCache.TryGetValue("vignette", out sprite)) return sprite;

            sprite = ToWorldSprite(OverlayPainter.PaintVignette(), PivotCentre, "vignette");
            spriteCache["vignette"] = sprite;
            return sprite;
        }

        public Sprite CreateWhitePixel()
        {
            Sprite sprite;
            if (spriteCache.TryGetValue("white", out sprite)) return sprite;

            PixelCanvas canvas = new PixelCanvas(1, 1);
            canvas.SetPixel(0, 0, Palette.White);
            sprite = ToSprite(canvas, PivotCentre, 1f, Vector4.zero, "white");
            spriteCache["white"] = sprite;
            return sprite;
        }

        public Sprite CreatePanel()
        {
            Sprite sprite;
            if (spriteCache.TryGetValue("panel", out sprite)) return sprite;

            float border = OverlayPainter.PanelBorder;
            sprite = ToSprite(OverlayPainter.PaintPanel(), PivotCentre, GameLayout.ArtPixelsPerUnit,
                new Vector4(border, border, border, border), "panel");
            spriteCache["panel"] = sprite;
            return sprite;
        }

        private static KioskScene SceneFor(EndingId ending)
        {
            switch (ending)
            {
                case EndingId.Morning: return KioskScene.Morning;
                case EndingId.Changed: return KioskScene.Changed;
                case EndingId.Seen: return KioskScene.Seen;
                default: return KioskScene.Fired;
            }
        }

        private static Sprite ToWorldSprite(PixelCanvas canvas, Vector2 pivot, string name)
        {
            return ToSprite(canvas, pivot, GameLayout.ArtPixelsPerUnit, Vector4.zero, name);
        }

        // Copies the canvas into a crisp (point-filtered) texture and wraps it in a sprite.
        private static Sprite ToSprite(PixelCanvas canvas, Vector2 pivot, float pixelsPerUnit, Vector4 border, string name)
        {
            Texture2D texture = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(canvas.Pixels);
            texture.Apply(false);

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, canvas.Width, canvas.Height), pivot,
                pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            return sprite;
        }
    }
}
