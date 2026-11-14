namespace NightShift
{
    // The world-space picture of the kiosk: owns every SpriteRenderer (background, customer,
    // Shift tint, vignette) and the small animations.
    // KioskView subscribes to EventBus inside Init() and calls these methods from its handlers
    // (see the "who reacts to what" table in Docs/ARCHITECTURE.md); GameBootstrap only calls Tick.
    // Implemented by KioskView (MonoBehaviour) in Scripts/Art/.
    // (Named "Kiosk" and not "Scene" to avoid a clash with UnityEditor.SceneView.)
    public interface IKioskView
    {
        // Puts the customer at the counter (GameLayout.CustomerFeet...), in the current normal/Shift look.
        void ShowCustomer(CustomerData customer);

        // Removes the customer from the counter.
        void HideCustomer();

        // Switches background and customer between the normal and the Shift look and turns the
        // screen effect (tint, slight shake, vignette) on or off.
        void SetShiftView(bool active);

        // Hides the customer, turns the Shift look off and shows the ending picture.
        void ShowEnding(EndingId ending);

        // Back to the start-up picture: normal background, no customer, no effects.
        void ResetView();

        // Called once per frame by GameBootstrap: animation frames, flicker, shake, effect strength
        // (may read IShiftVision.Noticed / IsStaredAt).
        void Tick(float deltaSeconds);
    }
}
