namespace NightShift
{
    // All sound, generated from code (AudioClip.Create).
    // AudioController subscribes to EventBus inside Init() and calls these methods from its handlers
    // (see the "who reacts to what" table in Docs/ARCHITECTURE.md). GameBootstrap only calls
    // BeginAfterUserGesture, SetMuted and Tick.
    // Implemented by AudioController (MonoBehaviour) in Scripts/Audio/.
    public interface IAudioController
    {
        // The player clicked Start: browsers allow audio from now on. Starts the fluorescent hum loop.
        // Before this call NOTHING may play (every other method must silently do nothing).
        // Safe to call more than once.
        void BeginAfterUserGesture();

        // Master sound on/off.
        void SetMuted(bool muted);

        bool IsMuted { get; }

        // Low drone loop while Shift is held.
        void SetShiftDrone(bool on);

        // One-shot sounds.
        void PlayDoorBell();       // a customer arrives
        void PlayWhistle();        // the bekci arrives
        void PlayCashRegister();   // correct sale
        void PlayRefuse();         // correct refuse / special
        void PlayError();          // wrong decision
        void PlayEndingStinger(EndingId ending);

        // Called once per frame by GameBootstrap. Drives the heartbeat from IShiftVision.Noticed
        // (silent near 0, faster and louder towards 1; silent when the session is not running).
        void Tick(float deltaSeconds);
    }
}
