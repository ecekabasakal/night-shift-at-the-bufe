using System;

namespace NightShift
{
    // Static C# events that announce what just happened in the game.
    //
    // Rules:
    //  - ONLY GameBootstrap calls the Raise... methods.
    //  - KioskView and AudioController subscribe once, inside their Init(), and never unsubscribe.
    //    "Play again" does not rebuild them: they stay subscribed and reset themselves on RestartRequested.
    //  - UIController does NOT subscribe: GameBootstrap drives it through IUIController directly.
    //  - Clear() removes all subscribers. GameBootstrap calls it in Awake (before creating the systems)
    //    and in OnDestroy, so nothing leaks between editor play sessions or tests.
    public static class EventBus
    {
        // The game phase changed (Intro / Serving / Ending). Also raised for the very first Intro.
        public static event Action<GamePhase> PhaseChanged;

        // The player clicked Start on the title screen. Audio is already allowed at this point.
        public static event Action GameStarted;

        // A customer stepped up to the counter.
        public static event Action<CustomerData> CustomerArrived;

        // The player pressed Continue after the feedback line; this customer walks away.
        public static event Action<CustomerData> CustomerLeft;

        // Shift vision turned on (true) or off (false).
        public static event Action<bool> ShiftChanged;

        // The Noticed meter changed. Value is 0..1.
        public static event Action<float> NoticedChanged;

        // The clock changed. Value is minutes after 23:00 (use GameData.FormatClock).
        public static event Action<int> ClockChanged;

        // The player decided (action, correct?, feedback key, ends the night?).
        public static event Action<DecisionResult> DecisionMade;

        // The mistake counter changed: (mistakes, maxMistakes).
        public static event Action<int, int> MistakesChanged;

        // The night is over with this ending.
        public static event Action<EndingId> EndingReached;

        // The player clicked Play again. Listeners put themselves back to their start-up state.
        public static event Action RestartRequested;

        public static void RaisePhaseChanged(GamePhase phase)
        {
            if (PhaseChanged != null) PhaseChanged(phase);
        }

        public static void RaiseGameStarted()
        {
            if (GameStarted != null) GameStarted();
        }

        public static void RaiseCustomerArrived(CustomerData customer)
        {
            if (CustomerArrived != null) CustomerArrived(customer);
        }

        public static void RaiseCustomerLeft(CustomerData customer)
        {
            if (CustomerLeft != null) CustomerLeft(customer);
        }

        public static void RaiseShiftChanged(bool active)
        {
            if (ShiftChanged != null) ShiftChanged(active);
        }

        public static void RaiseNoticedChanged(float noticed)
        {
            if (NoticedChanged != null) NoticedChanged(noticed);
        }

        public static void RaiseClockChanged(int minutesAfter23)
        {
            if (ClockChanged != null) ClockChanged(minutesAfter23);
        }

        public static void RaiseDecisionMade(DecisionResult result)
        {
            if (DecisionMade != null) DecisionMade(result);
        }

        public static void RaiseMistakesChanged(int mistakes, int maxMistakes)
        {
            if (MistakesChanged != null) MistakesChanged(mistakes, maxMistakes);
        }

        public static void RaiseEndingReached(EndingId ending)
        {
            if (EndingReached != null) EndingReached(ending);
        }

        public static void RaiseRestartRequested()
        {
            if (RestartRequested != null) RestartRequested();
        }

        // Removes every subscriber from every event.
        public static void Clear()
        {
            PhaseChanged = null;
            GameStarted = null;
            CustomerArrived = null;
            CustomerLeft = null;
            ShiftChanged = null;
            NoticedChanged = null;
            ClockChanged = null;
            DecisionMade = null;
            MistakesChanged = null;
            EndingReached = null;
            RestartRequested = null;
        }
    }
}
