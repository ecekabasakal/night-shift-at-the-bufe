namespace NightShift
{
    // The whole night as pure game logic: no MonoBehaviour, no UnityEngine.Time, no Input, no EventBus.
    // GameBootstrap (or a test) drives it by calling these methods.
    // Implemented by GameSession in Scripts/Gameplay/.
    public interface IGameSession : IShiftVision
    {
        // Back to the start-up state: Step = NotStarted, no customer, Noticed 0, Mistakes 0,
        // TotalShiftSeconds 0, ClockMinute 0. A freshly constructed session is in this state.
        void Reset();

        // Reset() and bring the first customer to the counter (Step = AwaitingDecision).
        // With zero customers the night finishes at once with the Morning ending.
        void StartNight();

        ServingStep Step { get; }

        // The customer at the counter; null when Step is NotStarted or Finished.
        CustomerData CurrentCustomer { get; }

        // Zero-based index of CurrentCustomer; -1 when there is none.
        int CustomerIndex { get; }

        int CustomerCount { get; }

        // Advances the Noticed meter. Does nothing unless Step is AwaitingDecision or ShowingFeedback.
        //  shiftHeld:  Noticed += rate * deltaSeconds and TotalShiftSeconds += deltaSeconds, where rate is
        //              noticedFillPerSecondStare if the customer staresBack, else noticedFillPerSecondOther
        //              if isOther, else noticedFillPerSecond.
        //  otherwise:  Noticed -= noticedDrainPerSecond * deltaSeconds.
        // Noticed is clamped to 0..1. Reaching 1 finishes the night immediately:
        // Step = Finished, Ending = Seen (this wins over an ending that was pending after a decision).
        void Tick(float deltaSeconds, bool shiftHeld);

        // True when Step is AwaitingDecision and the action is available
        // (Special only when the current customer has a specialActionKey).
        bool CanDecide(PlayerAction action);

        // Applies the decision; call only when CanDecide(action) is true. Step becomes ShowingFeedback.
        //  correct (action == correctAction): FeedbackKey = correctResultKey.
        //  wrong + wrongOutcome mistake:      Mistakes += 1; EndsNight when Mistakes reached MaxMistakes (Fired).
        //  wrong + wrongOutcome instantEnd:   Mistakes unchanged; EndsNight with the customer's instantEndingId.
        //  In both wrong cases FeedbackKey = wrongResultKey.
        DecisionResult Decide(PlayerAction action);

        // Call when Step is ShowingFeedback (the player pressed Continue).
        //  - the last decision ended the night -> Step = Finished with that ending;
        //  - otherwise, more customers         -> next customer, Step = AwaitingDecision;
        //  - otherwise (all served)            -> Step = Finished, ClockMinute = shiftLengthMinutes,
        //    Ending = Changed if TotalShiftSeconds > changedShiftSecondsThreshold, else Morning.
        void Advance();

        int Mistakes { get; }

        int MaxMistakes { get; }

        // Minutes after 23:00 shown on the clock: the current customer's arrivalMinute
        // (kept after a Seen / Fired ending), shiftLengthMinutes after Morning / Changed, 0 after Reset().
        int ClockMinute { get; }

        // Same as Step == ServingStep.Finished.
        bool IsFinished { get; }

        // Only valid when IsFinished is true.
        EndingId Ending { get; }
    }
}
