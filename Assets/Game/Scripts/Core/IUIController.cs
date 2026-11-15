using System;

namespace NightShift
{
    // Everything the uGUI layer can show, and how the player's intent comes back out.
    // The UI is a passive view: it never touches the game session and does not subscribe to EventBus.
    // GameBootstrap calls the methods below and listens to the events.
    // Implemented by UIController (MonoBehaviour) in Scripts/UI/.
    public interface IUIController
    {
        // ---- Player intent (raised from button clicks AND from keys read in Tick) ----

        // Start button on the title screen (also Enter / Space).
        event Action StartClicked;

        // Continue button on the intro note and on the feedback line (also Enter / Space).
        event Action ContinueClicked;

        // Sell / Refuse / Special button (also keys 1 / 2 / 3). Only raised while the action
        // buttons are visible and the notebook is closed; Special only when its button is visible.
        event Action<PlayerAction> ActionChosen;

        // Play again button on the ending screen (also Enter / Space).
        event Action PlayAgainClicked;

        // Sound on/off button in the HUD (also key M).
        event Action MuteClicked;

        // v2: one of the three handover notes was picked (button click or keys 1 / 2 / 3).
        // The value is the option index 0..2 (see HandoverNotes.OptionKey). Only raised while the
        // note buttons put up by ShowEndingWithHandover are visible.
        event Action<int> HandoverNoteChosen;

        // ---- Screens. Each Show... call hides whatever the other Show... calls put up. ----

        // Title screen: ui.title, ui.tagline, ui.controls, ui.shiftHint and the Start button. HUD hidden.
        void ShowTitle();

        // The previous worker's note (intro.note) with a Continue button. HUD hidden.
        void ShowIntroNote();

        // Serving screen: HUD visible, dialogue box with the customer's name (nameKey), greetingKey and
        // requestKey lines, and the action buttons: Sell, Refuse, and Special when the customer
        // has a specialActionKey (that key is the button label).
        void ShowCustomer(CustomerData customer);

        // Replaces the action buttons with the feedback line and a Continue button.
        // The customer's name stays; "correct" may be used to colour the line.
        void ShowFeedback(string feedbackKey, bool correct);

        // Ending screen: ending.<id>.title, ending.<id>.body and the Play again button. HUD hidden.
        void ShowEnding(EndingId ending);

        // v2: ending screen for a good ending (Morning / Changed): title and body as in ShowEnding, plus the
        // prompt ui.handoverPrompt and three note buttons showing the texts of HandoverNotes.OptionKey(0..2).
        // The Play again button is hidden and PlayAgainClicked is NOT raised (also not by Enter / Space)
        // until ShowHandoverDone is called.
        void ShowEndingWithHandover(EndingId ending);

        // v2: called after a note was chosen. Replaces the prompt and the three note buttons with the
        // line ui.handoverDone and shows the Play again button. Title and body stay.
        void ShowHandoverDone();

        // ---- HUD values (may be called at any time, also while the HUD is hidden) ----

        // Clock text, already formatted as "HH:MM".
        void SetClock(string clockText);

        // Noticed meter, 0..1.
        void SetNoticed(float noticed);

        void SetMistakes(int mistakes, int maxMistakes);

        // While active, the dialogue box shows the current customer's shiftHintKey line.
        void SetShiftView(bool active);

        // Updates the sound on/off indicator.
        void SetMuted(bool muted);

        // ---- Notebook (handled entirely inside the UI: Tab key or the notebook button) ----

        bool IsNotebookOpen { get; }

        // v2: the note left by the previous shift, already translated to text. The notebook shows the
        // heading ui.previousNote and this text after the last rule; "" hides both.
        // May be called at any time (at boot, and again after a note is chosen).
        void SetPreviousNote(string noteText);

        // ---- Per frame ----

        // Called once per frame by GameBootstrap, first in the frame. Reads the keys
        // 1 / 2 / 3 / Tab / Enter / Space / M and raises the matching events.
        // Never reads the Shift key (GameBootstrap does that).
        void Tick(float deltaSeconds);
    }
}
