using System;
using UnityEngine;
using UnityEngine.UI;

namespace NightShift
{
    // The rule notebook: a sheet of paper over the world band listing every rule from rules.json,
    // and under the rules the note left by the previous shift (when there is one).
    public class NotebookPanel
    {
        private const float RulesTop = 274f;
        private const float RulesBottom = 12f;
        // Bottom of the rules while the previous note is shown underneath (note block: y 10..54).
        private const float RulesBottomWithNote = 58f;

        private readonly GameObject root;
        private readonly Text[] numberTexts;
        private readonly Text[] ruleTexts;
        private readonly GameObject noteHeading;
        private readonly Text noteText;

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
            UIBuilder.CreateButton(paper, "CloseButton", 708f, 286f, 112f, 26f, data.Get("ui.notebookClose"), 13, onClose);
            UIBuilder.CreateImage(paper, "TitleLine", 20f, 280f, 800f, UITheme.BorderSize, UITheme.PaperBorder).raycastTarget = false;

            int count = data.Rules.Length;
            numberTexts = new Text[count];
            ruleTexts = new Text[count];
            for (int i = 0; i < count; i++)
            {
                numberTexts[i] = UIBuilder.CreateText(paper, "RuleNumber" + i, 0f, 0f, 0f, 0f, (i + 1) + ".", 15,
                    UITheme.InkDim, TextAnchor.MiddleLeft);
                numberTexts[i].fontStyle = FontStyle.Bold;

                ruleTexts[i] = UIBuilder.CreateText(paper, "Rule" + i, 0f, 0f, 0f, 0f, data.Get(data.Rules[i].textKey), 15,
                    UITheme.Ink, TextAnchor.MiddleLeft);
                UIBuilder.UseBestFit(ruleTexts[i], 10, 15);
            }

            Text heading = UIBuilder.CreateText(paper, "PreviousNoteHeading", 20f, 36f, 800f, 18f, data.Get(HandoverNotes.HeadingKey), 13,
                UITheme.InkDim, TextAnchor.MiddleLeft);
            heading.fontStyle = FontStyle.Bold;
            noteHeading = heading.gameObject;

            noteText = UIBuilder.CreateText(paper, "PreviousNote", 20f, 10f, 800f, 26f, "", 14, UITheme.Ink, TextAnchor.MiddleLeft);
            noteText.fontStyle = FontStyle.Italic;
            UIBuilder.UseBestFit(noteText, 10, 14);

            SetPreviousNote("");
            Close();
        }

        // "" hides the note block and gives its space back to the rules.
        public void SetPreviousNote(string text)
        {
            bool hasNote = !string.IsNullOrEmpty(text);
            noteText.text = hasNote ? text : "";
            noteHeading.SetActive(hasNote);
            noteText.gameObject.SetActive(hasNote);
            LayoutRules(hasNote ? RulesBottomWithNote : RulesBottom);
        }

        // The rules share the space between the title line and "bottom" equally.
        private void LayoutRules(float bottom)
        {
            int count = ruleTexts.Length;
            float slot = count > 0 ? (RulesTop - bottom) / count : 0f;
            for (int i = 0; i < count; i++)
            {
                float y = RulesTop - (i + 1) * slot;
                UIBuilder.SetRect(numberTexts[i].rectTransform, 20f, y, 26f, slot);
                UIBuilder.SetRect(ruleTexts[i].rectTransform, 48f, y + 1f, 772f, slot - 2f);
            }
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
