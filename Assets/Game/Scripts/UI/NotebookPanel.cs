using System;
using UnityEngine;
using UnityEngine.UI;

namespace NightShift
{
    // The rule notebook: a sheet of paper over the world band listing every rule from rules.json.
    public class NotebookPanel
    {
        private readonly GameObject root;

        public bool IsOpen { get; private set; }

        public NotebookPanel(Transform parent, GameData data, Action onClose)
        {
            // Dims everything below the HUD and swallows mouse clicks, so the buttons underneath cannot be pressed.
            Image blocker = UIBuilder.CreateImage(parent, "Notebook", 0f, 0f, GameLayout.LogicalWidth, GameLayout.HudBottomY, new Color(0f, 0f, 0f, 0.45f));
            root = blocker.gameObject;

            // The paper stays inside the world band (y 152..492) so the dialogue box can still be read.
            RectTransform paper = UIBuilder.CreatePanel(blocker.transform, "Paper", 60f, 160f, 840f, 324f, UITheme.Paper, UITheme.PaperBorder);

            Text title = UIBuilder.CreateText(paper, "Title", 20f, 286f, 600f, 28f, data.Get("ui.notebookTitle"), 22,
                UITheme.Ink, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            UIBuilder.CreateButton(paper, "CloseButton", 708f, 286f, 112f, 26f, UITheme.NotebookCloseHint, 13, onClose);
            UIBuilder.CreateImage(paper, "TitleLine", 20f, 280f, 800f, UITheme.BorderSize, UITheme.PaperBorder).raycastTarget = false;

            // The rules share the space under the title equally.
            const float rulesTop = 274f;
            const float rulesBottom = 12f;
            int count = data.Rules.Length;
            float slot = count > 0 ? (rulesTop - rulesBottom) / count : 0f;

            for (int i = 0; i < count; i++)
            {
                float y = rulesTop - (i + 1) * slot;
                Text number = UIBuilder.CreateText(paper, "RuleNumber" + i, 20f, y, 26f, slot, (i + 1) + ".", 15,
                    UITheme.InkDim, TextAnchor.MiddleLeft);
                number.fontStyle = FontStyle.Bold;

                Text rule = UIBuilder.CreateText(paper, "Rule" + i, 48f, y + 1f, 772f, slot - 2f, data.Get(data.Rules[i].textKey), 15,
                    UITheme.Ink, TextAnchor.MiddleLeft);
                UIBuilder.UseBestFit(rule, 10, 15);
            }

            Close();
        }

        public void Open()
        {
            IsOpen = true;
            root.SetActive(true);
        }

        public void Close()
        {
            IsOpen = false;
            root.SetActive(false);
        }
    }
}
