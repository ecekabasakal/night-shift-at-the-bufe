using System;
using UnityEngine;
using UnityEngine.UI;

namespace NightShift
{
    // The title screen: game name, tagline, the Shift instruction, the Start button and the controls line.
    public class TitleScreen
    {
        private readonly GameObject root;

        public TitleScreen(Transform parent, GameData data, Action onStart)
        {
            Image dim = UIBuilder.CreateImage(parent, "TitleScreen", 0f, 0f, GameLayout.LogicalWidth, GameLayout.LogicalHeight, UITheme.ScreenDim);
            root = dim.gameObject;
            Transform t = dim.transform;

            Text title = UIBuilder.CreateText(t, "Title", 60f, 372f, 840f, 70f, data.Get("ui.title"), 52,
                UITheme.Amber, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            UIBuilder.UseBestFit(title, 28, 52);

            UIBuilder.CreateText(t, "Tagline", 60f, 334f, 840f, 30f, data.Get("ui.tagline"), 20,
                UITheme.TextMain, TextAnchor.MiddleCenter);

            // The one instruction the player must not miss.
            RectTransform hintBox = UIBuilder.CreatePanel(t, "ShiftHintBox", 240f, 248f, 480f, 50f, UITheme.PanelFill, UITheme.Accent);
            Text hint = UIBuilder.CreateText(hintBox, "ShiftHint", 10f, 0f, 460f, 50f, data.Get("ui.shiftHint"), 20,
                UITheme.Accent, TextAnchor.MiddleCenter);
            UIBuilder.UseBestFit(hint, 12, 20);

            UIBuilder.CreateButton(t, "StartButton", 340f, 150f, 280f, 52f,
                data.Get("ui.start") + UITheme.ConfirmKeyHint, 20, onStart);

            Text controls = UIBuilder.CreateText(t, "Controls", 30f, 60f, 900f, 24f, data.Get("ui.controls"), 15,
                UITheme.TextDim, TextAnchor.MiddleCenter);
            UIBuilder.UseBestFit(controls, 10, 15);
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
        }
    }
}
