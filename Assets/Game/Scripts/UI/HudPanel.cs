using System;
using UnityEngine;
using UnityEngine.UI;

namespace NightShift
{
    // The top strip during play: clock, notebook button, Noticed meter, mistake marks and the sound button.
    public class HudPanel
    {
        private const float StripHeight = GameLayout.LogicalHeight - GameLayout.HudBottomY; // 48
        private const float PulseFrom = 0.7f;      // the meter starts to pulse above this value
        private const float BarWidth = 304f;
        private const float BarHeight = 20f;
        private const float BarInnerWidth = BarWidth - 2f * UITheme.BorderSize;
        private const float BarInnerHeight = BarHeight - 2f * UITheme.BorderSize;
        private const float MarksX = 778f;
        private const float MarksWidth = 80f;

        private readonly GameObject root;
        private readonly Text clockText;
        private readonly Text noticedLabel;
        private readonly Image noticedFill;
        private readonly RectTransform marksRoot;
        private readonly Text muteLabel;

        private Image[] marks = new Image[0];
        private float noticed;
        private float pulseTime;

        public HudPanel(Transform parent, GameData data, Action onNotebook, Action onMute)
        {
            Image strip = UIBuilder.CreateImage(parent, "Hud", 0f, GameLayout.HudBottomY, GameLayout.LogicalWidth, StripHeight, UITheme.PanelFill);
            root = strip.gameObject;
            Transform t = strip.transform;
            UIBuilder.CreateImage(t, "BottomLine", 0f, 0f, GameLayout.LogicalWidth, UITheme.BorderSize, UITheme.PanelBorder);

            // Left: clock and the notebook hint (which is also a button).
            clockText = UIBuilder.CreateText(t, "Clock", 16f, 0f, 84f, StripHeight, "", 26, UITheme.Amber, TextAnchor.MiddleLeft);
            clockText.fontStyle = FontStyle.Bold;
            UIBuilder.CreateButton(t, "NotebookButton", 104f, 10f, 132f, 28f, data.Get("ui.notebookHint"), 14, onNotebook);

            // Centre: Noticed meter.
            noticedLabel = UIBuilder.CreateText(t, "NoticedLabel", 240f, 0f, 80f, StripHeight, data.Get("ui.noticed"), 16,
                UITheme.TextMain, TextAnchor.MiddleRight);
            RectTransform bar = UIBuilder.CreatePanel(t, "NoticedBar", 328f, 14f, BarWidth, BarHeight, new Color(0f, 0f, 0f, 1f), UITheme.PanelBorder);
            noticedFill = UIBuilder.CreateImage(bar, "NoticedFill", UITheme.BorderSize, UITheme.BorderSize, 0f, BarInnerHeight, UITheme.Accent);
            noticedFill.raycastTarget = false;

            // Right: mistakes and sound.
            UIBuilder.CreateText(t, "MistakesLabel", 640f, 0f, 128f, StripHeight, data.Get("ui.mistakes"), 16,
                UITheme.TextMain, TextAnchor.MiddleRight);
            marksRoot = UIBuilder.CreateRect(t, "MistakeMarks", MarksX, 0f, MarksWidth, StripHeight);
            Button muteButton = UIBuilder.CreateButton(t, "MuteButton", 864f, 10f, 88f, 28f, UITheme.SoundOnLabel, 13, onMute);
            muteLabel = muteButton.GetComponentInChildren<Text>();

            SetNoticed(0f);
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
        }

        public void SetClock(string clock)
        {
            clockText.text = clock;
        }

        public void SetNoticed(float value)
        {
            noticed = Mathf.Clamp01(value);

            // The fill grows from the left edge of the bar, inside its border.
            noticedFill.rectTransform.sizeDelta = new Vector2(BarInnerWidth * noticed, BarInnerHeight);

            UpdateNoticedColor();
        }

        // One mark per allowed mistake; marks already used turn red.
        public void SetMistakes(int mistakes, int maxMistakes)
        {
            if (maxMistakes < 0) maxMistakes = 0;
            if (marks.Length != maxMistakes)
            {
                RebuildMarks(maxMistakes);
            }
            for (int i = 0; i < marks.Length; i++)
            {
                marks[i].color = i < mistakes ? UITheme.Danger : UITheme.PanelBorder;
            }
        }

        public void SetMuted(bool muted)
        {
            muteLabel.text = muted ? UITheme.SoundOffLabel : UITheme.SoundOnLabel;
            muteLabel.color = muted ? UITheme.Danger : UITheme.TextMain;
        }

        // Advances the pulse of the meter.
        public void Tick(float deltaSeconds)
        {
            if (noticed >= PulseFrom)
            {
                pulseTime += deltaSeconds;
                UpdateNoticedColor();
            }
            else
            {
                pulseTime = 0f;
            }
        }

        // Green -> amber -> red as the meter fills; near the top it also flashes brighter.
        private void UpdateNoticedColor()
        {
            Color color = noticed < 0.5f
                ? Color.Lerp(UITheme.Accent, UITheme.Amber, noticed * 2f)
                : Color.Lerp(UITheme.Amber, UITheme.Danger, (noticed - 0.5f) * 2f);

            if (noticed >= PulseFrom)
            {
                // Faster pulse the closer the meter is to full: 2 to 5 flashes per second.
                float speed = Mathf.Lerp(2f, 5f, (noticed - PulseFrom) / (1f - PulseFrom));
                float flash = 0.5f + 0.5f * Mathf.Sin(pulseTime * speed * 2f * Mathf.PI);
                color = Color.Lerp(color, Color.white, flash * 0.45f);
            }

            noticedFill.color = color;
            noticedLabel.color = noticed >= PulseFrom ? UITheme.Danger : UITheme.TextMain;
        }

        private void RebuildMarks(int count)
        {
            for (int i = 0; i < marks.Length; i++)
            {
                UnityEngine.Object.Destroy(marks[i].gameObject);
            }

            marks = new Image[count];
            if (count == 0) return;

            // Marks get smaller when there are too many to fit.
            float step = Mathf.Min(22f, MarksWidth / count);
            float size = step - 6f;
            for (int i = 0; i < count; i++)
            {
                marks[i] = UIBuilder.CreateImage(marksRoot, "Mark" + i, i * step, (StripHeight - size) * 0.5f, size, size, UITheme.PanelBorder);
                marks[i].raycastTarget = false;
            }
        }
    }
}
