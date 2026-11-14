using System;
using UnityEngine;
using UnityEngine.UI;

namespace NightShift
{
    // The button area (bottom right): Sell / Refuse / optional Special, or the Continue button during feedback.
    public class ActionPanel
    {
        private readonly GameObject root;
        private readonly GameObject sellButton;
        private readonly GameObject refuseButton;
        private readonly GameObject specialButton;
        private readonly GameObject continueButton;
        private readonly Text specialLabel;

        // True while the third button is on screen (so key 3 is allowed).
        public bool HasSpecial { get; private set; }

        public ActionPanel(Transform parent, GameData data, Action<PlayerAction> onAction, Action onContinue)
        {
            // Region y 0..152, x 624..960 with an 8 px margin: three buttons of 42 px with 7 px gaps.
            RectTransform area = UIBuilder.CreateRect(parent, "Actions", 628f, 8f, 324f, 140f);
            root = area.gameObject;

            sellButton = UIBuilder.CreateButton(area, "SellButton", 0f, 98f, 324f, 42f,
                "[1] " + data.Get("ui.sell"), 18, () => onAction(PlayerAction.Sell)).gameObject;
            refuseButton = UIBuilder.CreateButton(area, "RefuseButton", 0f, 49f, 324f, 42f,
                "[2] " + data.Get("ui.refuse"), 18, () => onAction(PlayerAction.Refuse)).gameObject;

            Button special = UIBuilder.CreateButton(area, "SpecialButton", 0f, 0f, 324f, 42f,
                "", 18, () => onAction(PlayerAction.Special));
            specialButton = special.gameObject;
            specialLabel = special.GetComponentInChildren<Text>();

            continueButton = UIBuilder.CreateButton(area, "ContinueButton", 0f, 49f, 324f, 42f,
                data.Get("ui.continue") + UITheme.ConfirmKeyHint, 18, onContinue).gameObject;
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
        }

        // Shows Sell and Refuse, and the third button when the customer has one ("" = no third button).
        public void ShowChoices(string specialText)
        {
            HasSpecial = !string.IsNullOrEmpty(specialText);
            specialLabel.text = "[3] " + specialText;

            sellButton.SetActive(true);
            refuseButton.SetActive(true);
            specialButton.SetActive(HasSpecial);
            continueButton.SetActive(false);
        }

        public void ShowContinue()
        {
            HasSpecial = false;
            sellButton.SetActive(false);
            refuseButton.SetActive(false);
            specialButton.SetActive(false);
            continueButton.SetActive(true);
        }
    }
}
