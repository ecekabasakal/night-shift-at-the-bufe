using UnityEngine;

namespace NightShift
{
    // The only object in the scene. Creates the camera and every system, wires them together,
    // reads the Shift key, ticks the game session once per frame and announces what happened
    // (IUIController calls + EventBus events). The game flow lives here; the rules live in GameSession.
    public class GameBootstrap : MonoBehaviour
    {
        // A long frame (tab was in the background) must not fill the Noticed meter in one step.
        private const float MaxDeltaSeconds = 0.1f;

        // Where the handover note is saved. Tests assign a MemoryNoteStore here BEFORE the GameBootstrap
        // object is created (and set it back to null afterwards); null = PlayerPrefs.
        public static INoteStore NoteStoreOverride;

        private GameData data;
        private GameState state;
        private IGameSession session;
        private ISpriteFactory sprites;
        private IKioskView kiosk;
        private IAudioController audioController;
        private IUIController ui;
        private INoteStore noteStore;

        // Intro has two screens: the title (false) and the previous worker's note (true).
        private bool introNoteShowing;

        // True on a good ending until the player has picked a handover note.
        private bool handoverPending;

        // Last values sent out by PublishState, to announce only real changes.
        private bool lastShiftActive;
        private float lastNoticed = -1f;
        private int lastMistakes = -1;
        private int lastClockMinute = -1;

        private void Awake()
        {
            EventBus.Clear();

            data = GameData.LoadFromResources();
            noteStore = NoteStoreOverride != null ? NoteStoreOverride : new PlayerPrefsNoteStore();

            state = new GameState();
            state.PhaseChanged += EventBus.RaisePhaseChanged;

            CreateCamera();

            session = new GameSession(data);
            sprites = new SpriteFactory();

            KioskView kioskView = new GameObject("KioskView").AddComponent<KioskView>();
            kioskView.Init(sprites, session);
            kiosk = kioskView;

            AudioController audioView = new GameObject("AudioController").AddComponent<AudioController>();
            audioView.Init(session);
            audioController = audioView;

            UIController uiView = new GameObject("UIController").AddComponent<UIController>();
            uiView.Init(data, sprites);
            ui = uiView;

            ui.StartClicked += OnStartClicked;
            ui.ContinueClicked += OnContinueClicked;
            ui.ActionChosen += OnActionChosen;
            ui.PlayAgainClicked += OnPlayAgainClicked;
            ui.MuteClicked += OnMuteClicked;
            ui.HandoverNoteChosen += OnHandoverNoteChosen;

            // The note saved in an earlier game (if any) goes into the notebook.
            ui.SetPreviousNote(HandoverNotes.NoteText(data, noteStore.Load()));

            EnterIntro();
        }

        private void OnDestroy()
        {
            EventBus.Clear();
        }

        // Frame order: 1) UI keys  2) Shift key + session  3) art  4) audio.
        private void Update()
        {
            float deltaSeconds = Mathf.Min(Time.deltaTime, MaxDeltaSeconds);

            ui.Tick(deltaSeconds);

            if (state.Phase == GamePhase.Serving)
            {
                bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                session.Tick(deltaSeconds, shiftHeld);
                PublishState();

                if (session.IsFinished)
                {
                    EnterEnding();
                }
            }

            kiosk.Tick(deltaSeconds);
            audioController.Tick(deltaSeconds);
        }

        private void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera worldCamera = cameraObject.AddComponent<Camera>();
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = GameLayout.CameraOrthographicSize;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = Color.black;
            worldCamera.nearClipPlane = 0.1f;
            worldCamera.farClipPlane = 100f;

            // The one AudioListener of the game.
            cameraObject.AddComponent<AudioListener>();
        }

        // ---------- Flow ----------

        // Title screen. Used at start-up and after Play again.
        private void EnterIntro()
        {
            introNoteShowing = false;
            handoverPending = false;
            session.Reset();
            state.Set(GamePhase.Intro);
            PublishState();
            ui.ShowTitle();
        }

        private void OnStartClicked()
        {
            if (state.Phase != GamePhase.Intro || introNoteShowing) return;

            audioController.BeginAfterUserGesture();
            EventBus.RaiseGameStarted();

            introNoteShowing = true;
            ui.ShowIntroNote();
        }

        private void OnContinueClicked()
        {
            if (state.Phase == GamePhase.Intro)
            {
                if (introNoteShowing) BeginNight();
                return;
            }

            if (state.Phase == GamePhase.Serving && session.Step == ServingStep.ShowingFeedback)
            {
                EventBus.RaiseCustomerLeft(session.CurrentCustomer);
                session.Advance();
                PublishState();

                if (session.IsFinished)
                {
                    EnterEnding();
                }
                else
                {
                    ArriveCustomer();
                }
            }
        }

        // The note was dismissed: the first customer arrives.
        private void BeginNight()
        {
            introNoteShowing = false;
            session.StartNight();
            state.Set(GamePhase.Serving);
            PublishState();

            if (session.IsFinished)
            {
                EnterEnding();
            }
            else
            {
                ArriveCustomer();
            }
        }

        private void ArriveCustomer()
        {
            CustomerData customer = session.CurrentCustomer;
            EventBus.RaiseCustomerArrived(customer);
            ui.ShowCustomer(customer);
        }

        private void OnActionChosen(PlayerAction action)
        {
            if (state.Phase != GamePhase.Serving || !session.CanDecide(action)) return;

            DecisionResult result = session.Decide(action);
            EventBus.RaiseDecisionMade(result);
            PublishState();
            ui.ShowFeedback(result.FeedbackKey, result.Correct);
        }

        private void EnterEnding()
        {
            PublishState();
            state.Set(GamePhase.Ending);
            EventBus.RaiseEndingReached(session.Ending);

            // A good ending asks for a handover note first; a bad ending is the plain ending screen.
            handoverPending = HandoverNotes.IsGoodEnding(session.Ending);
            if (handoverPending)
            {
                ui.ShowEndingWithHandover(session.Ending);
            }
            else
            {
                ui.ShowEnding(session.Ending);
            }
        }

        // The player picked the note to leave for the next shift: save its key and show it in the notebook.
        private void OnHandoverNoteChosen(int optionIndex)
        {
            if (state.Phase != GamePhase.Ending || !handoverPending) return;
            if (optionIndex < 0 || optionIndex >= HandoverNotes.OptionCount) return;

            handoverPending = false;
            string noteKey = HandoverNotes.OptionKey(optionIndex);
            noteStore.Save(noteKey);
            ui.SetPreviousNote(HandoverNotes.NoteText(data, noteKey));
            ui.ShowHandoverDone();
        }

        private void OnPlayAgainClicked()
        {
            if (state.Phase != GamePhase.Ending || handoverPending) return;

            EventBus.RaiseRestartRequested();
            EnterIntro();
        }

        private void OnMuteClicked()
        {
            bool muted = !audioController.IsMuted;
            audioController.SetMuted(muted);
            ui.SetMuted(muted);
        }

        // Compares the session with what was last announced and sends out every change
        // (to the UI directly, to everyone else through the EventBus).
        // Called after every call that can change the session.
        private void PublishState()
        {
            bool shiftActive = session.IsShiftActive;
            if (shiftActive != lastShiftActive)
            {
                lastShiftActive = shiftActive;
                ui.SetShiftView(shiftActive);
                EventBus.RaiseShiftChanged(shiftActive);
            }

            float noticed = session.Noticed;
            if (noticed != lastNoticed)
            {
                lastNoticed = noticed;
                ui.SetNoticed(noticed);
                EventBus.RaiseNoticedChanged(noticed);
            }

            int mistakes = session.Mistakes;
            if (mistakes != lastMistakes)
            {
                lastMistakes = mistakes;
                ui.SetMistakes(mistakes, session.MaxMistakes);
                EventBus.RaiseMistakesChanged(mistakes, session.MaxMistakes);
            }

            int clockMinute = session.ClockMinute;
            if (clockMinute != lastClockMinute)
            {
                lastClockMinute = clockMinute;
                ui.SetClock(GameData.FormatClock(clockMinute));
                EventBus.RaiseClockChanged(clockMinute);
            }
        }
    }
}
