using UnityEngine;

namespace NightShift
{
    // The world picture: background, the customer at the counter, the Shift screen effect
    // (tint, shake, vignette), the small animations and the ending picture.
    // It listens to the EventBus (subscribed once in Init) and is ticked by GameBootstrap.
    public class KioskView : MonoBehaviour, IKioskView
    {
        private const float CustomerFrameSeconds = 0.3f;
        private const float CatAlarmSeconds = 1.5f;
        private const float CatAlarmFrameSeconds = 0.12f;
        private const float ShakeStepSeconds = 0.05f;
        private const float EffectFadePerSecond = 10f;       // the tint fades in over 0.1 s
        private const float NormalVignetteAlpha = 0.4f;      // a little darkness at the edges, always

        private ISpriteFactory sprites;
        private IShiftVision vision;

        private Transform shaken;                // background + customer hang here so they shake together
        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer catAlarmRenderer;
        private SpriteRenderer customerRenderer;
        private SpriteRenderer tintRenderer;
        private SpriteRenderer vignetteRenderer;
        private SpriteRenderer endingRenderer;

        private Sprite[] backgroundNormal;
        private Sprite[] backgroundShift;
        private Sprite[] catAlarmNormal;
        private Sprite[] catAlarmShift;
        private Sprite[] customerNormal;
        private Sprite[] customerShift;

        private bool shiftActive;
        private float clock;                     // seconds since Init, drives the animations
        private float effect;                    // 0 = normal view, 1 = Shift effect fully on
        private float catAlarmLeft;              // seconds the street cat keeps hissing; 0 = calm

        // Fluorescent flicker: long quiet pauses, then a short burst of off/on blinks.
        private bool tubeDark;
        private float flickerTimer = 3f;
        private int flickerBlinksLeft;

        private float shakeTimer;
        private readonly SeededNumbers numbers = new SeededNumbers(1907);

        public void Init(ISpriteFactory sprites, IShiftVision vision)
        {
            this.sprites = sprites;
            this.vision = vision;

            shaken = new GameObject("Shaken").transform;
            shaken.SetParent(transform, false);

            backgroundRenderer = CreateRenderer("Background", shaken, GameLayout.SortBackground);
            catAlarmRenderer = CreateRenderer("StreetCatAlarm", shaken, GameLayout.SortStreetCat);
            customerRenderer = CreateRenderer("Customer", shaken, GameLayout.SortCustomer);
            tintRenderer = CreateRenderer("ShiftTint", transform, GameLayout.SortShiftTint);
            vignetteRenderer = CreateRenderer("Vignette", transform, GameLayout.SortVignette);
            endingRenderer = CreateRenderer("EndingPicture", transform, GameLayout.SortEnding);

            customerRenderer.transform.localPosition = GameLayout.CustomerFeetWorld();

            // The white pixel is one world unit; stretch it a bit past the screen edges.
            tintRenderer.sprite = sprites.CreateWhitePixel();
            tintRenderer.transform.localScale = new Vector3(
                GameLayout.LogicalWidth / GameLayout.LogicalPixelsPerUnit + 1f,
                GameLayout.LogicalHeight / GameLayout.LogicalPixelsPerUnit + 1f,
                1f);
            vignetteRenderer.sprite = sprites.CreateVignette();

            backgroundNormal = sprites.CreateBackgroundFrames(false);
            backgroundShift = sprites.CreateBackgroundFrames(true);
            catAlarmNormal = sprites.CreateStreetCatAlarmFrames(false);
            catAlarmShift = sprites.CreateStreetCatAlarmFrames(true);

            EventBus.CustomerArrived += OnCustomerArrived;
            EventBus.CustomerLeft += OnCustomerLeft;
            EventBus.ShiftChanged += SetShiftView;
            EventBus.EndingReached += ShowEnding;
            EventBus.RestartRequested += ResetView;

            ResetView();
        }

        public void ShowCustomer(CustomerData customer)
        {
            if (customer == null)
            {
                HideCustomer();
                return;
            }
            customerNormal = sprites.CreateCustomerFrames(customer, false);
            customerShift = sprites.CreateCustomerFrames(customer, true);
            customerRenderer.enabled = true;
            Refresh();
        }

        public void HideCustomer()
        {
            customerNormal = null;
            customerShift = null;
            customerRenderer.enabled = false;
            customerRenderer.sprite = null;
            StopCatAlarm();
        }

        public void PlayCatAlarm()
        {
            catAlarmLeft = CatAlarmSeconds;
            Refresh();
        }

        public void SetShiftView(bool active)
        {
            shiftActive = active;
            Refresh(); // the pictures swap at once; only the tint fades
        }

        public void ShowEnding(EndingId ending)
        {
            HideCustomer();
            shiftActive = false;
            effect = 0f;
            endingRenderer.sprite = sprites.CreateEndingBackground(ending);
            endingRenderer.enabled = true;
            Refresh();
        }

        public void ResetView()
        {
            HideCustomer();
            shiftActive = false;
            effect = 0f;
            tubeDark = false;
            flickerBlinksLeft = 0;
            flickerTimer = 3f;
            endingRenderer.enabled = false;
            endingRenderer.sprite = null;
            Refresh();
        }

        public void Tick(float deltaSeconds)
        {
            clock += deltaSeconds;
            catAlarmLeft = Mathf.Max(0f, catAlarmLeft - deltaSeconds);
            effect = Mathf.MoveTowards(effect, shiftActive ? 1f : 0f, deltaSeconds * EffectFadePerSecond);

            TickFlicker(deltaSeconds);
            TickShake(deltaSeconds);
            Refresh();
        }

        private void OnCustomerArrived(CustomerData customer)
        {
            ShowCustomer(customer);
            if (customer != null && customer.catReacts) PlayCatAlarm();
            else StopCatAlarm();
        }

        private void StopCatAlarm()
        {
            catAlarmLeft = 0f;
            catAlarmRenderer.enabled = false;
        }

        private void OnCustomerLeft(CustomerData customer)
        {
            HideCustomer();
        }

        // ---------- Per-frame pieces ----------

        private void TickFlicker(float deltaSeconds)
        {
            flickerTimer -= deltaSeconds;
            if (flickerTimer > 0f) return;

            // A burst is an even number of switches, so it always ends with the tube on.
            if (flickerBlinksLeft == 0) flickerBlinksLeft = 2 * numbers.Next(1, 4);

            tubeDark = !tubeDark;
            flickerBlinksLeft--;

            if (flickerBlinksLeft > 0)
            {
                flickerTimer = 0.04f + 0.09f * numbers.NextFloat();
            }
            else
            {
                // The light is more nervous on the other side.
                flickerTimer = shiftActive ? 0.8f + 2f * numbers.NextFloat() : 4f + 6f * numbers.NextFloat();
            }
        }

        private void TickShake(float deltaSeconds)
        {
            if (effect <= 0f)
            {
                shaken.localPosition = Vector3.zero;
                return;
            }

            shakeTimer -= deltaSeconds;
            if (shakeTimer > 0f) return;
            shakeTimer = ShakeStepSeconds;

            // Size of the shake in art pixels: barely there at first, worse as you are noticed.
            float artPixels = 0.25f + 1.25f * Noticed();
            if (StaredAt()) artPixels += 1.5f;

            float reach = effect * artPixels / GameLayout.ArtPixelsPerUnit;
            shaken.localPosition = new Vector3(
                (numbers.NextFloat() * 2f - 1f) * reach,
                (numbers.NextFloat() * 2f - 1f) * reach,
                0f);
        }

        // Makes every renderer show what the current state says.
        private void Refresh()
        {
            Sprite[] background = shiftActive ? backgroundShift : backgroundNormal;
            backgroundRenderer.sprite = background[tubeDark && background.Length > 1 ? 1 : 0];

            catAlarmRenderer.enabled = catAlarmLeft > 0f;
            if (catAlarmRenderer.enabled)
            {
                Sprite[] alarm = shiftActive ? catAlarmShift : catAlarmNormal;
                int frame = (int)((CatAlarmSeconds - catAlarmLeft) / CatAlarmFrameSeconds) % alarm.Length;
                catAlarmRenderer.sprite = alarm[frame];
            }

            Sprite[] customer = shiftActive ? customerShift : customerNormal;
            if (customer != null && customer.Length > 0)
            {
                int frame = (int)(clock / CustomerFrameSeconds) % customer.Length;
                customerRenderer.sprite = customer[frame];
            }

            float noticed = Noticed();
            float pulse = StaredAt() ? 0.5f + 0.5f * Mathf.Sin(clock * 20f) : 0f;

            Color tint = Palette.ShiftTint;
            tint.a = effect * (0.1f + 0.16f * noticed + 0.12f * pulse);
            tintRenderer.color = tint;
            tintRenderer.enabled = tint.a > 0f;

            Color vignette = Color.Lerp(Color.black, Palette.ShiftVignette, effect);
            vignette.a = Mathf.Lerp(NormalVignetteAlpha, Mathf.Clamp01(0.62f + 0.3f * noticed + 0.08f * pulse), effect);
            vignetteRenderer.color = vignette;
        }

        private float Noticed()
        {
            return vision != null ? Mathf.Clamp01(vision.Noticed) : 0f;
        }

        private bool StaredAt()
        {
            return vision != null && vision.IsStaredAt;
        }

        private SpriteRenderer CreateRenderer(string objectName, Transform parent, int sortingOrder)
        {
            GameObject child = new GameObject(objectName);
            child.transform.SetParent(parent, false);
            SpriteRenderer spriteRenderer = child.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }
    }
}
