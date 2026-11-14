using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NightShift
{
    // Creates the uGUI building blocks (rectangle, flat image, bordered panel, text, button) from code.
    // Every position is in logical pixels measured from the bottom-left corner of the parent.
    public static class UIBuilder
    {
        private static Font font;

        public static Font GetFont()
        {
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return font;
        }

        // An empty rectangle at (x, y) with size (width, height).
        public static RectTransform CreateRect(Transform parent, string name, float x, float y, float width, float height)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        // A flat coloured rectangle (no sprite).
        public static Image CreateImage(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            RectTransform rect = CreateRect(parent, name, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        // A flat rectangle with a thin border. Returns the outer rectangle; put children inside it.
        public static RectTransform CreatePanel(Transform parent, string name, float x, float y, float width, float height, Color fill, Color border)
        {
            Image outer = CreateImage(parent, name, x, y, width, height, border);
            float b = UITheme.BorderSize;
            CreateImage(outer.transform, "Fill", b, b, width - 2f * b, height - 2f * b, fill);
            return outer.rectTransform;
        }

        // A text that wraps inside its rectangle. Texts never block mouse clicks.
        public static Text CreateText(Transform parent, string name, float x, float y, float width, float height,
            string content, int fontSize, Color color, TextAnchor alignment)
        {
            RectTransform rect = CreateRect(parent, name, x, y, width, height);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = GetFont();
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.text = content;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        // For texts of unknown length: shrink the font (down to minSize) until the text fits its rectangle.
        public static void UseBestFit(Text text, int minSize, int maxSize)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = maxSize;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        // A flat button: border, fill that changes colour on mouse over / press, centred label.
        public static Button CreateButton(Transform parent, string name, float x, float y, float width, float height,
            string label, int fontSize, Action onClick)
        {
            Image border = CreateImage(parent, name, x, y, width, height, UITheme.ButtonBorder);
            float b = UITheme.BorderSize;

            // The fill stays white; the Button tints it with the colours below.
            Image fill = CreateImage(border.transform, "Fill", b, b, width - 2f * b, height - 2f * b, Color.white);

            Text text = CreateText(border.transform, "Label", 6f, 0f, width - 12f, height, label, fontSize,
                UITheme.TextMain, TextAnchor.MiddleCenter);
            UseBestFit(text, 10, fontSize);

            Button button = border.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;

            ColorBlock colors = button.colors;
            colors.normalColor = UITheme.ButtonFill;
            colors.highlightedColor = UITheme.ButtonHover;
            colors.pressedColor = UITheme.ButtonPressed;
            colors.selectedColor = UITheme.ButtonFill;
            colors.disabledColor = UITheme.ButtonFill;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            // No keyboard navigation: Enter / Space must never "click" a button by themselves.
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            button.onClick.AddListener(new UnityAction(onClick));
            return button;
        }
    }
}
