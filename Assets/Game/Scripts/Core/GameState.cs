using System;

namespace NightShift
{
    // The top-level state machine: Intro -> Serving -> Ending -> (Play again) -> Intro.
    // Only GameBootstrap changes the phase; it forwards every change to EventBus.PhaseChanged.
    public class GameState
    {
        public GamePhase Phase { get; private set; }

        // Raised on every Set call, with the new phase.
        public event Action<GamePhase> PhaseChanged;

        public GameState()
        {
            Phase = GamePhase.Intro;
        }

        public void Set(GamePhase phase)
        {
            Phase = phase;
            if (PhaseChanged != null) PhaseChanged(phase);
        }
    }
}
