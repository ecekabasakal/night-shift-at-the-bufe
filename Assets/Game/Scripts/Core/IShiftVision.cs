namespace NightShift
{
    // What the presentation layer (art, audio) may read about Shift vision. Read-only.
    // Implemented by GameSession (through IGameSession, which extends this interface).
    public interface IShiftVision
    {
        // True while the player is holding Shift during a running night.
        // Always false when Step is NotStarted or Finished.
        bool IsShiftActive { get; }

        // The Noticed meter, 0..1. Keeps its value after the night finishes; 0 after Reset().
        float Noticed { get; }

        // True while Shift is active AND the current customer has staresBack = true.
        bool IsStaredAt { get; }

        // Seconds of Shift held so far this night.
        float TotalShiftSeconds { get; }
    }
}
