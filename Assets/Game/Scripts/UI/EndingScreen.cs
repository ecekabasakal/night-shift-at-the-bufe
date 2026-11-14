using System;
using UnityEngine;
using UnityEngine.UI;

namespace NightShift
{
    // The ending screen: title, body and Play again in the lower part, so the ending picture shows above it.
    public class EndingScreen
    {
        private const float PanelHeight = 244f;

        private readonly GameObject root;
        private readonly Text titleText;
        private readonly Text bodyText;

        public EndingScreen(Transform parent, GameData data, Action onPlayAgain)
        {
            Image panel = UIBuilder.CreateImage(parent, "EndingScreen", 0f, 0f, GameLayout.LogicalWidth, PanelHeight, UITheme.PanelFill);
            root = panel.gameObject;
            Transform t = panel.transform;
            UIBuilder.CreateImage(t, "TopLine", 0f, PanelHeight - UITheme.BorderSize, GameLayout.LogicalWidth, UITheme.BorderSize, UITheme.PanelBorder);

            titleText = UIBuilder.CreateText(t, "Title", 60f, 186f, 840f, 46f, "", 34, UITheme.Amber, TextAnchor.MiddleCenter);
            titleText.fontStyle = FontStyle.Bold;
            UIBuilder.UseBestFit(titleText, 18, 34);

            bodyText = UIBuilder.CreateText(t, "Body", 120f, 72f, 720f, 108f, "", 17, UITheme.TextMain, TextAnchor.UpperCenter);
            UIBuilder.UseBestFit(bodyText, 11, 17);

            UIBuilder.CreateButton(t, "PlayAgainButton", 320f, 16f, 320f, 44f,
                data.Get("ui.playAgain") + UITheme.ConfirmKeyHint, 18, onPlayAgain);
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
        }

        public void SetEnding(EndingId ending, string title, string body)
        {
            titleText.text = title;
            titleText.color = TitleColor(ending);
            bodyText.text = body;
        }

        private static Color TitleColor(EndingId ending)
        {
            switch (ending)
            {
                case EndingId.Morning: return UITheme.Amber;
                case EndingId.Changed: return UITheme.Accent;
                default: return UITheme.Danger; // Seen, Fired
            }
        }
    }
}
