using UnityEngine;

namespace NightShift
{
    // The shared layout numbers, so that Art (world space) and UI (canvas space) agree.
    //
    // The game is designed on a 960 x 540 "logical pixel" screen, origin bottom-left.
    //  - World: 100 logical pixels = 1 world unit, camera centred on (0,0), so the world is 9.6 x 5.4 units.
    //  - Pixel art: 1 art pixel = 4 logical pixels, so world sprites use pixelsPerUnit = 25.
    //  - UI: Canvas (Screen Space - Overlay) + CanvasScaler "Scale With Screen Size", reference 960 x 540.
    public static class GameLayout
    {
        public const int LogicalWidth = 960;
        public const int LogicalHeight = 540;
        public const float LogicalPixelsPerUnit = 100f;
        public const float ArtPixelsPerUnit = 25f;
        public const float CameraOrthographicSize = 2.7f;

        // Full-screen art (background, vignette, ending picture) in art pixels.
        public const int BackgroundArtWidth = 240;
        public const int BackgroundArtHeight = 135;

        // Customer sprite canvas in art pixels (256 x 320 logical pixels), pivot bottom-centre.
        public const int CustomerArtWidth = 64;
        public const int CustomerArtHeight = 80;

        // Where the customer's feet (sprite pivot) are, in logical pixels.
        public const float CustomerFeetX = 480f;
        public const float CustomerFeetY = 168f;

        // Screen regions in logical pixels (y measured from the bottom).
        public const float HudBottomY = 492f;       // top HUD strip is y 492..540 (clock, noticed meter, mistakes)
        public const float BottomPanelTopY = 152f;  // dialogue box + action buttons live in y 0..152
        // The band y 152..492 belongs to the world: the UI keeps it free during Serving
        // (the notebook overlay is the only exception).

        // SpriteRenderer.sortingOrder values (default sorting layer).
        public const int SortBackground = 0;
        public const int SortStreetCat = 5;   // v2: the alarmed street cat overlay (behind the customer)
        public const int SortCustomer = 10;
        public const int SortShiftTint = 30;
        public const int SortVignette = 40;
        public const int SortEnding = 50;

        // Logical pixel position (origin bottom-left) -> world position (z = 0).
        public static Vector3 LogicalToWorld(float x, float y)
        {
            return new Vector3(
                (x - LogicalWidth * 0.5f) / LogicalPixelsPerUnit,
                (y - LogicalHeight * 0.5f) / LogicalPixelsPerUnit,
                0f);
        }

        // World position of the customer's feet.
        public static Vector3 CustomerFeetWorld()
        {
            return LogicalToWorld(CustomerFeetX, CustomerFeetY);
        }
    }
}
