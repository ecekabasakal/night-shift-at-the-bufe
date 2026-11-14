using UnityEngine;

namespace NightShift
{
    // Plays every sound of the game: builds the clips from SoundBank once, then reacts to EventBus
    // events (one-shots, Shift drone) and drives the hum, drone fade and heartbeat from Tick.
    public class AudioController : MonoBehaviour, IAudioController
    {
        // Playback volumes (the clips themselves peak at 0.9; error and fired at 0.6).
        private const float HumVolume = 0.12f;
        private const float DroneVolume = 0.3f;
        private const float DoorBellVolume = 0.45f;
        private const float WhistleVolume = 0.35f;
        private const float CashRegisterVolume = 0.5f;
        private const float RefuseVolume = 0.4f;
        private const float ErrorVolume = 0.45f;
        private const float StingerVolume = 0.5f;
        private const float HeartbeatMinVolume = 0.15f;
        private const float HeartbeatMaxVolume = 0.7f;

        private const float HumFadeInSeconds = 0.5f;
        private const float DroneFadeSeconds = 0.2f;
        private const float HumDuckWhileDrone = 0.4f;    // hum loses 40 % of its volume under the drone
        private const float HumDetuneWhileDrone = 0.03f; // and drops 3 % in pitch

        private const float HeartbeatSilentBelow = 0.15f; // Noticed value under which the heart is silent
        private const float HeartbeatSlowAt = 0.2f;       // Noticed value for the slowest rate
        private const float HeartbeatSlowBpm = 60f;
        private const float HeartbeatFastBpm = 150f;

        private IShiftVision vision;

        private AudioSource humSource;
        private AudioSource droneSource;
        private AudioSource heartSource;
        private AudioSource oneShotSource;

        private AudioClip doorBellClip;
        private AudioClip whistleClip;
        private AudioClip cashRegisterClip;
        private AudioClip refuseClip;
        private AudioClip errorClip;
        private AudioClip[] stingerClips; // indexed by (int)EndingId

        private bool started;      // false until the player clicked Start (browser audio rule)
        private bool muted;
        private bool nightRunning; // true while the phase is Serving
        private bool droneWanted;
        private float humLevel;    // 0..1 fade position
        private float droneLevel;  // 0..1 fade position
        private float secondsToNextBeat;

        public bool IsMuted
        {
            get { return muted; }
        }

        public void Init(IShiftVision vision)
        {
            this.vision = vision;

            humSource = CreateSource("Hum", ToClip("Hum", SoundBank.FluorescentHum(), Synth.LoopRate), true);
            droneSource = CreateSource("Drone", ToClip("Drone", SoundBank.ShiftDrone(), Synth.LoopRate), true);
            heartSource = CreateSource("Heartbeat", ToClip("Heartbeat", SoundBank.Heartbeat(), Synth.OneShotRate), false);
            oneShotSource = CreateSource("OneShots", null, false);

            doorBellClip = ToClip("DoorBell", SoundBank.DoorBell(), Synth.OneShotRate);
            whistleClip = ToClip("Whistle", SoundBank.Whistle(), Synth.OneShotRate);
            cashRegisterClip = ToClip("CashRegister", SoundBank.CashRegister(), Synth.OneShotRate);
            refuseClip = ToClip("Refuse", SoundBank.Refuse(), Synth.OneShotRate);
            errorClip = ToClip("Error", SoundBank.Error(), Synth.OneShotRate);

            EndingId[] endings = { EndingId.Morning, EndingId.Changed, EndingId.Seen, EndingId.Fired };
            stingerClips = new AudioClip[endings.Length];
            foreach (EndingId ending in endings)
            {
                stingerClips[(int)ending] = ToClip("Stinger" + ending, SoundBank.EndingStinger(ending), Synth.OneShotRate);
            }

            AudioListener.volume = 1f;

            EventBus.PhaseChanged += OnPhaseChanged;
            EventBus.CustomerArrived += OnCustomerArrived;
            EventBus.ShiftChanged += SetShiftDrone;
            EventBus.DecisionMade += OnDecisionMade;
            EventBus.EndingReached += OnEndingReached;
            EventBus.RestartRequested += OnRestartRequested;
        }

        // ---------- IAudioController ----------

        public void BeginAfterUserGesture()
        {
            if (started) return;
            started = true;

            humLevel = 0f;
            humSource.volume = 0f;
            humSource.Play();
        }

        public void SetMuted(bool muted)
        {
            this.muted = muted;
            AudioListener.volume = muted ? 0f : 1f;
        }

        public void SetShiftDrone(bool on)
        {
            if (!started) return;
            droneWanted = on;
        }

        public void PlayDoorBell()
        {
            PlayOneShot(doorBellClip, DoorBellVolume);
        }

        public void PlayWhistle()
        {
            PlayOneShot(whistleClip, WhistleVolume);
        }

        public void PlayCashRegister()
        {
            PlayOneShot(cashRegisterClip, CashRegisterVolume);
        }

        public void PlayRefuse()
        {
            PlayOneShot(refuseClip, RefuseVolume);
        }

        public void PlayError()
        {
            PlayOneShot(errorClip, ErrorVolume);
        }

        public void PlayEndingStinger(EndingId ending)
        {
            PlayOneShot(stingerClips[(int)ending], StingerVolume);
        }

        public void Tick(float deltaSeconds)
        {
            if (!started) return;

            UpdateHumAndDrone(deltaSeconds);
            UpdateHeartbeat(deltaSeconds);
        }

        // ---------- EventBus handlers ----------

        private void OnPhaseChanged(GamePhase phase)
        {
            nightRunning = phase == GamePhase.Serving;
        }

        private void OnCustomerArrived(CustomerData customer)
        {
            PlayDoorBell();
            if (customer != null && customer.appearance != null && customer.appearance.accessory == "whistle")
            {
                PlayWhistle();
            }
        }

        private void OnDecisionMade(DecisionResult result)
        {
            if (!result.Correct)
            {
                PlayError();
            }
            else if (result.Action == PlayerAction.Sell)
            {
                PlayCashRegister();
            }
            else
            {
                PlayRefuse();
            }
        }

        private void OnEndingReached(EndingId ending)
        {
            SetShiftDrone(false);
            PlayEndingStinger(ending);
        }

        // Back to the state right after Start was clicked: only the hum is playing. Mute is kept.
        private void OnRestartRequested()
        {
            if (!started) return;

            nightRunning = false;
            droneWanted = false;
            droneLevel = 0f;
            droneSource.Stop();
            heartSource.Stop();
            oneShotSource.Stop(); // cuts the tail of the ending stinger
            secondsToNextBeat = 0f;
        }

        // ---------- Per-frame work ----------

        // Fades the hum in after Start, fades the drone in and out, and lets the hum duck and sag
        // slightly in pitch while the drone is up.
        private void UpdateHumAndDrone(float deltaSeconds)
        {
            humLevel = Mathf.MoveTowards(humLevel, 1f, deltaSeconds / HumFadeInSeconds);
            droneLevel = Mathf.MoveTowards(droneLevel, droneWanted ? 1f : 0f, deltaSeconds / DroneFadeSeconds);

            if (droneLevel > 0f && !droneSource.isPlaying)
            {
                droneSource.Play(); // always from the start of the loop, so every Shift press sounds the same
            }
            else if (droneLevel <= 0f && droneSource.isPlaying)
            {
                droneSource.Stop();
            }

            droneSource.volume = DroneVolume * droneLevel;
            humSource.volume = HumVolume * humLevel * (1f - HumDuckWhileDrone * droneLevel);
            humSource.pitch = 1f - HumDetuneWhileDrone * droneLevel;
        }

        // Plays one lub-dub whenever the countdown runs out. The higher the Noticed meter,
        // the shorter the countdown and the louder the beat.
        private void UpdateHeartbeat(float deltaSeconds)
        {
            float noticed = vision.Noticed;
            if (!nightRunning || noticed < HeartbeatSilentBelow)
            {
                secondsToNextBeat = 0f; // the first beat comes as soon as the meter is high enough
                return;
            }

            secondsToNextBeat -= deltaSeconds;
            if (secondsToNextBeat > 0f) return;

            float bpm = Mathf.Lerp(HeartbeatSlowBpm, HeartbeatFastBpm, Mathf.InverseLerp(HeartbeatSlowAt, 1f, noticed));
            secondsToNextBeat = 60f / bpm;

            heartSource.volume = Mathf.Lerp(HeartbeatMinVolume, HeartbeatMaxVolume,
                Mathf.InverseLerp(HeartbeatSilentBelow, 1f, noticed));
            heartSource.Play(); // restarts the clip; it is shorter than the fastest beat interval
        }

        // ---------- Helpers ----------

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (!started) return;
            oneShotSource.PlayOneShot(clip, volume);
        }

        private static AudioClip ToClip(string clipName, float[] samples, int sampleRate)
        {
            AudioClip clip = AudioClip.Create(clipName, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // A 2D AudioSource on its own child object.
        private AudioSource CreateSource(string sourceName, AudioClip clip, bool loop)
        {
            GameObject child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);

            AudioSource source = child.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
