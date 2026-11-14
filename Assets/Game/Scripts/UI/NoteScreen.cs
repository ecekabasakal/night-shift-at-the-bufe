using System;
using UnityEngine;
using UnityEngine.UI;

namespace NightShift
{
    // The previous worker's note (intro.note) on a sheet of paper, with a Continue button.
    public class NoteScreen
    {
        private readonly GameObject root;

        public NoteScreen(Transform parent, GameData data, Action onContinue)
        {
            Image dim = UIBuilder.CreateImage(parent, "NoteScreen", 0f, 0f, GameLayout.LogicalWidth, GameLayout.LogicalHeight, UITheme.ScreenDim);
            root = dim.gameObject;

            RectTransform paper = UIBuilder.CreatePanel(dim.transform, "Paper", 160f, 78f, 640f, 384f, UITheme.Paper, UITheme.PaperBorder);

            Text note = UIBuilder.CreateText(paper, "Note", 28f, 84f, 584f, 276f, data.Get("intro.note"), 20,
                UITheme.Ink, TextAnchor.UpperLeft);
            UIBuilder.UseBestFit(note, 12, 20);

            UIBuilder.CreateButton(paper, "ContinueButton", 180f, 22f, 280f, 44f,
                data.Get("ui.continue") + UITheme.ConfirmKeyHint, 18, onContinue);
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
        }
    }
}
