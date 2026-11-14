using UnityEngine;

namespace NightShift
{
    // The small colour palette shared by every screen.
    public static class UITheme
    {
        // Dark panels (HUD, dialogue box, ending).
        public static readonly Color PanelFill = new Color(0.05f, 0.06f, 0.08f, 0.92f);
        public static readonly Color PanelBorder = new Color(0.30f, 0.36f, 0.31f, 1f);
        public static readonly Color ScreenDim = new Color(0.02f, 0.03f, 0.04f, 0.78f);

        // Text.
        public static readonly Color TextMain = new Color(0.91f, 0.90f, 0.83f, 1f);   // off-white
        public static readonly Color TextDim = new Color(0.60f, 0.63f, 0.58f, 1f);
        public static readonly Color Accent = new Color(0.66f, 0.93f, 0.55f, 1f);     // fluorescent green
        public static readonly Color Amber = new Color(0.96f, 0.76f, 0.30f, 1f);
        public static readonly Color Danger = new Color(0.92f, 0.29f, 0.24f, 1f);     // mistakes, full meter

        // Buttons (flat colours: normal / mouse over / pressed).
        public static readonly Color ButtonFill = new Color(0.12f, 0.15f, 0.14f, 1f);
        public static readonly Color ButtonHover = new Color(0.20f, 0.28f, 0.22f, 1f);
        public static readonly Color ButtonPressed = new Color(0.32f, 0.46f, 0.31f, 1f);
        public static readonly Color ButtonBorder = new Color(0.42f, 0.60f, 0.38f, 1f);

        // Paper (the intro note and the notebook).
        public static readonly Color Paper = new Color(0.86f, 0.81f, 0.67f, 1f);
        public static readonly Color PaperBorder = new Color(0.45f, 0.38f, 0.26f, 1f);
        public static readonly Color Ink = new Color(0.16f, 0.14f, 0.11f, 1f);
        public static readonly Color InkDim = new Color(0.40f, 0.34f, 0.25f, 1f);

        public const float BorderSize = 2f;
    }
}
