using UnityEngine;

namespace NightShift
{
    // Draws every sprite of the game from code (Texture2D, FilterMode.Point, no asset files).
    // World sprites use GameLayout.ArtPixelsPerUnit so that one art pixel = 4 screen pixels.
    // Implementations should cache: asking twice for the same thing returns the same sprites.
    // Implemented by SpriteFactory (plain C# class) in Scripts/Art/.
    public interface ISpriteFactory
    {
        // The kiosk background, 240 x 135 art pixels, pivot in the centre.
        // Two or more frames for the fluorescent flicker: frame 0 = light on, others = flicker.
        // shiftView = true gives the "other side" version.
        Sprite[] CreateBackgroundFrames(bool shiftView);

        // A customer, 64 x 80 art pixels (GameLayout.CustomerArt...), pivot bottom-centre, shadow included.
        // One or more animation frames (e.g. cat tail). shiftView = true applies customer.shiftAnomalies.
        Sprite[] CreateCustomerFrames(CustomerData customer, bool shiftView);

        // v2: the street cat outside the window, alarmed (puffed up, hissing). Two or more animation frames,
        // each a full 240 x 135 art pixel canvas, pivot in the centre, transparent everywhere except the cat,
        // so it lines up with the background when placed at the same position. The picture must completely
        // cover the calm cat that is painted into the background. shiftView = true gives the version
        // that matches the Shift background.
        Sprite[] CreateStreetCatAlarmFrames(bool shiftView);

        // Full-screen picture shown behind the ending screen, 240 x 135 art pixels, pivot in the centre.
        // EndingId.Changed shows the "you look a bit other too" variant.
        Sprite CreateEndingBackground(EndingId ending);

        // Dark-edged, transparent-centre overlay, 240 x 135 art pixels, pivot in the centre (white, tint it).
        Sprite CreateVignette();

        // A single white pixel that fills exactly one world unit (pixelsPerUnit = 1), pivot in the centre.
        // For tint overlays (scale it), UI bars and plain UI backgrounds.
        Sprite CreateWhitePixel();

        // A small bordered pixel box with 9-slice borders set, for UI panels and buttons
        // (use with Image.Type.Sliced; white/grey so the UI can tint it).
        Sprite CreatePanel();
    }
}
