using UnityEngine;
using UnityEngine.UI;

namespace NightShift
{
    // The dialogue box (bottom left): customer name, what they say or the feedback line, and the Shift hint line.
    public class DialoguePanel
    {
        private readonly GameObject root;
        private readonly Text nameText;
        private readonly Text bodyText;
        private readonly Text hintText;

        public DialoguePanel(Transform parent)
        {
            // Region y 0..152, x 0..624 with an 8 px margin.
            RectTransform panel = UIBuilder.CreatePanel(parent, "DialogueBox", 8f, 8f, 612f, 140f, UITheme.PanelFill, UITheme.PanelBorder);
            root = panel.gameObject;

            nameText = UIBuilder.CreateText(panel, "Name", 14f, 108f, 584f, 24f, "", 18, UITheme.Amber, TextAnchor.MiddleLeft);
            nameText.fontStyle = FontStyle.Bold;

            bodyText = UIBuilder.CreateText(panel, "Body", 14f, 32f, 584f, 76f, "", 16, UITheme.TextMain, TextAnchor.UpperLeft);
            UIBuilder.UseBestFit(bodyText, 11, 16);

            hintText = UIBuilder.CreateText(panel, "ShiftHint", 14f, 8f, 584f, 22f, "", 14, UITheme.Accent, TextAnchor.MiddleLeft);
            hintText.fontStyle = FontStyle.Italic;
            UIBuilder.UseBestFit(hintText, 9, 14);
            hintText.gameObject.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
        }

        public void ShowCustomer(string customerName, string greeting, string request, string shiftHint)
        {
            nameText.text = customerName;
            bodyText.text = greeting + "\n" + request;
            bodyText.color = UITheme.TextMain;
            hintText.text = shiftHint;
        }

        // The feedback line takes the place of the customer's lines; the name stays.
        public void ShowFeedback(string feedback, bool correct)
        {
            bodyText.text = feedback;
            bodyText.color = correct ? UITheme.Accent : UITheme.Danger;
        }

        // The hint line is only on screen while the Shift view is active.
        public void SetHintVisible(bool visible)
        {
            hintText.gameObject.SetActive(visible);
        }
    }
}
