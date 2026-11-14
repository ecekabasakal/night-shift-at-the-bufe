using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NightShift
{
    // Which screen the UI is showing right now. Keys and buttons only work for the current screen.
    public enum UIScreenId
    {
        None,
        Title,
        Note,
        Customer,   // dialogue + action buttons
        Feedback,   // feedback line + Continue
        Ending
    }

    // The whole uGUI layer, built from code. A passive view: GameBootstrap calls the Show.../Set... methods,
    // and the player's clicks and keys come back out as the events below.
    public class UIController : MonoBehaviour, IUIController
    {
        public event Action StartClicked;
        public event Action ContinueClicked;
        public event Action<PlayerAction> ActionChosen;
        public event Action PlayAgainClicked;
        public event Action MuteClicked;

        private GameData data;
        private EventSystem eventSystem;

        private TitleScreen title;
        private NoteScreen note;
        private HudPanel hud;
        private DialoguePanel dialogue;
        private ActionPanel actions;
        private NotebookPanel notebook;
        private EndingScreen ending;

        private UIScreenId screen = UIScreenId.None;

        // Enter / Space are ignored until this reaches zero, so a held or double key press
        // cannot skip a feedback line or the ending. Mouse clicks are not affected.
        private const float FeedbackConfirmDelay = 0.3f;
        private const float EndingConfirmDelay = 1f;
        private float confirmDelay;

        // Frame of the last intent event, so that one frame can never raise two of them
        // (for example Space as "Start" and then again as "Continue" on the screen that just opened).
        private int lastIntentFrame = -1;

        public bool IsNotebookOpen
        {
            get { return notebook != null && notebook.IsOpen; }
        }

        // The sprite factory is part of the contract but not needed: every panel is a flat coloured Image.
        public void Init(GameData data, ISpriteFactory sprites)
        {
            this.data = data;

            CreateEventSystem();
            Transform root = CreateCanvas();

            // Creation order = drawing order (later = on top).
            dialogue = new DialoguePanel(root, data.Get("ui.shiftHint"));
            actions = new ActionPanel(root, data, PressAction, PressContinue);
            hud = new HudPanel(root, data, ToggleNotebook, PressMute);
            notebook = new NotebookPanel(root, data, ToggleNotebook);
            title = new TitleScreen(root, data, PressStart);
            note = new NoteScreen(root, data, PressContinue);
            ending = new EndingScreen(root, data, PressPlayAgain);

            SwitchTo(UIScreenId.None);
        }

        // ---------- Screens ----------

        public void ShowTitle()
        {
            SwitchTo(UIScreenId.Title);
        }

        public void ShowIntroNote()
        {
            SwitchTo(UIScreenId.Note);
        }

        public void ShowCustomer(CustomerData customer)
        {
            dialogue.ShowCustomer(
                data.Get(customer.nameKey),
                data.Get(customer.greetingKey),
                data.Get(customer.requestKey),
                data.Get(customer.shiftHintKey));
            actions.ShowChoices(data.Get(customer.specialActionKey));
            SwitchTo(UIScreenId.Customer);
        }

        public void ShowFeedback(string feedbackKey, bool correct)
        {
            dialogue.ShowFeedback(data.Get(feedbackKey), correct);
            actions.ShowContinue();
            SwitchTo(UIScreenId.Feedback);
        }

        public void ShowEnding(EndingId endingId)
        {
            ending.SetEnding(endingId,
                data.Get(DataParsing.EndingTitleKey(endingId)),
                data.Get(DataParsing.EndingBodyKey(endingId)));
            SwitchTo(UIScreenId.Ending);
        }

        // ---------- HUD values ----------

        public void SetClock(string clockText)
        {
            hud.SetClock(clockText);
        }

        public void SetNoticed(float noticed)
        {
            hud.SetNoticed(noticed);
        }

        public void SetMistakes(int mistakes, int maxMistakes)
        {
            hud.SetMistakes(mistakes, maxMistakes);
        }

        public void SetShiftView(bool active)
        {
            dialogue.SetShiftView(active);
        }

        public void SetMuted(bool muted)
        {
            hud.SetMuted(muted);
        }

        // ---------- Per frame ----------

        public void Tick(float deltaSeconds)
        {
            // A clicked button must not stay selected, or Enter / Space would click it again.
            ClearSelection();

            hud.Tick(deltaSeconds);
            if (confirmDelay > 0f) confirmDelay -= deltaSeconds;

            if (Input.GetKeyDown(KeyCode.M))
            {
                PressMute();
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleNotebook();
                return;
            }

            // While the notebook is open, only Tab and M do something.
            if (notebook.IsOpen) return;

            bool confirm = confirmDelay <= 0f &&
                (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space));

            // "screen" is read once here, so one key press can only reach one branch.
            switch (screen)
            {
                case UIScreenId.Title:
                    if (confirm) PressStart();
                    break;

                case UIScreenId.Note:
                case UIScreenId.Feedback:
                    if (confirm) PressContinue();
                    break;

                case UIScreenId.Customer:
                    if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) PressAction(PlayerAction.Sell);
                    else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) PressAction(PlayerAction.Refuse);
                    else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) PressAction(PlayerAction.Special);
                    break;

                case UIScreenId.Ending:
                    if (confirm) PressPlayAgain();
                    break;
            }
        }

        // ---------- Player intent (shared by buttons and keys; each checks that it is valid right now) ----------

        private void PressStart()
        {
            if (screen != UIScreenId.Title || !BeginIntent()) return;
            if (StartClicked != null) StartClicked();
        }

        private void PressContinue()
        {
            if (screen != UIScreenId.Note && screen != UIScreenId.Feedback) return;
            if (notebook.IsOpen || !BeginIntent()) return;
            if (ContinueClicked != null) ContinueClicked();
        }

        private void PressAction(PlayerAction action)
        {
            if (screen != UIScreenId.Customer || notebook.IsOpen) return;
            if (action == PlayerAction.Special && !actions.HasSpecial) return;
            if (!BeginIntent()) return;
            if (ActionChosen != null) ActionChosen(action);
        }

        private void PressPlayAgain()
        {
            if (screen != UIScreenId.Ending || !BeginIntent()) return;
            if (PlayAgainClicked != null) PlayAgainClicked();
        }

        private void PressMute()
        {
            if (MuteClicked != null) MuteClicked();
        }

        // The notebook can only be opened during play (HUD visible).
        private void ToggleNotebook()
        {
            if (notebook.IsOpen)
            {
                notebook.Close();
            }
            else if (screen == UIScreenId.Customer || screen == UIScreenId.Feedback)
            {
                notebook.Open();
            }
        }

        // False when an intent event was already raised in this frame.
        private bool BeginIntent()
        {
            if (Time.frameCount == lastIntentFrame) return false;
            lastIntentFrame = Time.frameCount;
            return true;
        }

        // ---------- Set-up and helpers ----------

        // Shows exactly the parts that belong to the given screen and closes the notebook.
        private void SwitchTo(UIScreenId newScreen)
        {
            screen = newScreen;
            if (newScreen == UIScreenId.Ending) confirmDelay = EndingConfirmDelay;
            else if (newScreen == UIScreenId.Feedback) confirmDelay = FeedbackConfirmDelay;
            else confirmDelay = 0f;
            bool serving = newScreen == UIScreenId.Customer || newScreen == UIScreenId.Feedback;

            notebook.Close();
            title.SetVisible(newScreen == UIScreenId.Title);
            note.SetVisible(newScreen == UIScreenId.Note);
            ending.SetVisible(newScreen == UIScreenId.Ending);
            hud.SetVisible(serving);
            dialogue.SetVisible(serving);
            actions.SetVisible(serving);

            ClearSelection();
        }

        private void ClearSelection()
        {
            if (eventSystem.currentSelectedGameObject != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        private void CreateEventSystem()
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(transform, false);
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        // Creates the canvas and returns a 960 x 540 rectangle in its centre that all screens are built in.
        private Transform CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(GameLayout.LogicalWidth, GameLayout.LogicalHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform root = UIBuilder.CreateRect(canvasObject.transform, "Root", 0f, 0f, GameLayout.LogicalWidth, GameLayout.LogicalHeight);
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            return root;
        }
    }
}
