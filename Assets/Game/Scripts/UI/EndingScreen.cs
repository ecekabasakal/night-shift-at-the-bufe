using System;
using UnityEngine;
using UnityEngine.UI;

namespace NightShift
{
    // The ending screen: title, body and Play again in the lower part, so the ending picture shows above it.
    // On a good ending the panel is taller and first asks the player to pick a note for the next shift.
    public class EndingScreen
    {
        // Plain layout (bad endings): panel y 0..244.
        private const float PlainHeight = 244f;
        // Handover layout (good endings): panel y 0..270, the lower half of the screen.
        private const float HandoverHeight = 270f;

        private readonly GameObject root;
        private readonly RectTransform panelRect;
        private readonly RectTransform topLine;
        private readonly Text titleText;
        private readonly Text bodyText;
        private readonly GameObject playAgainButton;
        private readonly GameObject handoverPrompt;
        private readonly GameObject[] noteButtons = new GameObject[HandoverNotes.OptionCount];
        private readonly GameObject handoverDone;

        public EndingScreen(Transform parent, GameData data, Action onPlayAgain, Action<int> onNoteChosen)
        {
            Image panel = UIBuilder.CreateImage(parent, "EndingScreen", 0f, 0f, GameLayout.LogicalWidth, PlainHeight, UITheme.PanelFill);
            root = panel.gameObject;
            panelRect = panel.rectTransform;
            Transform t = panel.transform;
            topLine = UIBuilder.CreateImage(t, "TopLine", 0f, 0f, GameLayout.LogicalWidth, UITheme.BorderSize, UITheme.PanelBorder).rectTransform;

            // Title and body are placed by ShowPlain / UseHandoverLayout.
            titleText = UIBuilder.CreateText(t, "Title", 0f, 0f, 0f, 0f, "", 34, UITheme.Amber, TextAnchor.MiddleCenter);
            titleText.fontStyle = FontStyle.Bold;
            UIBuilder.UseBestFit(titleText, 18, 34);

            bodyText = UIBuilder.CreateText(t, "Body", 0f, 0f, 0f, 0f, "", 17, UITheme.TextMain, TextAnchor.UpperCenter);
            UIBuilder.UseBestFit(bodyText, 11, 17);

            playAgainButton = UIBuilder.CreateButton(t, "PlayAgainButton", 320f, 16f, 320f, 44f,
                data.Get("ui.playAgain") + "  " + data.Get("ui.keyEnter"), 18, onPlayAgain).gameObject;

            // Handover: a prompt and three full-width note buttons (28 px high, 4 px gaps), first note on top.
            handoverPrompt = UIBuilder.CreateText(t, "HandoverPrompt", 60f, 106f, 840f, 22f, data.Get(HandoverNotes.PromptKey), 16,
                UITheme.Amber, TextAnchor.MiddleCenter).gameObject;
            for (int i = 0; i < noteButtons.Length; i++)
            {
                int optionIndex = i; // each button needs its own copy for the click handler
                noteButtons[i] = UIBuilder.CreateButton(t, "HandoverButton" + (i + 1), 60f, 74f - i * 32f, 840f, 28f,
                    "[" + (i + 1) + "] " + data.Get(HandoverNotes.OptionKey(i)), 15, () => onNoteChosen(optionIndex)).gameObject;
            }

            // Shown in place of the prompt and the note buttons after a note was picked.
            handoverDone = UIBuilder.CreateText(t, "HandoverDone", 60f, 76f, 840f, 24f, data.Get(HandoverNotes.DoneKey), 16,
                UITheme.Accent, TextAnchor.MiddleCenter).gameObject;

            ShowPlain();
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

        // Bad ending: title, body and Play again only.
        public void ShowPlain()
        {
            SetPanelHeight(PlainHeight);
            UIBuilder.SetRect(titleText.rectTransform, 60f, 186f, 840f, 46f);
            UIBuilder.SetRect(bodyText.rectTransform, 120f, 72f, 720f, 108f);
            SetParts(false, false, true);
        }

        // Good ending, step 1: the prompt and the three notes; no Play again yet.
        public void ShowHandoverChoice()
        {
            UseHandoverLayout();
            SetParts(true, false, false);
        }

        // Good ending, step 2: the confirmation line and Play again.
        public void ShowHandoverDone()
        {
            UseHandoverLayout();
            SetParts(false, true, true);
        }

        // Title and body move up to make room for the notes underneath.
        private void UseHandoverLayout()
        {
            SetPanelHeight(HandoverHeight);
            UIBuilder.SetRect(titleText.rectTransform, 60f, 220f, 840f, 42f);
            UIBuilder.SetRect(bodyText.rectTransform, 80f, 132f, 800f, 84f);
        }

        private void SetPanelHeight(float height)
        {
            UIBuilder.SetRect(panelRect, 0f, 0f, GameLayout.LogicalWidth, height);
            UIBuilder.SetRect(topLine, 0f, height - UITheme.BorderSize, GameLayout.LogicalWidth, UITheme.BorderSize);
        }

        private void SetParts(bool noteChoice, bool doneLine, bool playAgain)
        {
            handoverPrompt.SetActive(noteChoice);
            for (int i = 0; i < noteButtons.Length; i++)
            {
                noteButtons[i].SetActive(noteChoice);
            }
            handoverDone.SetActive(doneLine);
            playAgainButton.SetActive(playAgain);
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
