using System;

namespace NightShift
{
    // One method per sound of the game. Each returns finished mono samples (peak 0.9 or 0.6) and always
    // returns the same samples (fixed random seeds). Loops use Synth.LoopRate, one-shots Synth.OneShotRate.
    public static class SoundBank
    {
        private const float Peak = 0.9f;
        // Steady buzzy sounds (error, fired) sound much louder than bells at the same peak, so they get a lower one.
        private const float DensePeak = 0.6f;
        private const double LoopSeconds = 4.0;

        // ---------- Loops (no fades: the last sample runs straight into the first) ----------

        // Fluorescent tube: mains hum (50 Hz + harmonics), a thin high buzz and a slight flicker.
        // Every frequency is a multiple of 0.25 Hz, so each fits a whole number of times into 4 s.
        public static float[] FluorescentHum()
        {
            int rate = Synth.LoopRate;
            System.Random rng = new System.Random(11);
            float[] samples = Synth.Silence(LoopSeconds, rate);

            Synth.AddSine(samples, rate, 50.0, 1.0, 0.0);
            Synth.AddSine(samples, rate, 100.0, 0.6, 0.0);
            Synth.AddSine(samples, rate, 150.0, 0.25, 0.0);
            Synth.AddSine(samples, rate, 200.0, 0.1, 0.0);

            // High buzz: weak odd harmonics of 100 Hz between 1.9 and 5.9 kHz.
            for (double frequency = 1900.0; frequency <= 5900.0; frequency += 200.0)
            {
                Synth.AddSine(samples, rate, frequency, 0.02, rng.NextDouble() * Math.PI * 2.0);
            }

            // Flicker: three slow, unrelated wobbles of a few percent.
            Synth.ApplyTremolo(samples, rate, 0.5, 0.03, rng.NextDouble() * Math.PI * 2.0);
            Synth.ApplyTremolo(samples, rate, 1.75, 0.02, rng.NextDouble() * Math.PI * 2.0);
            Synth.ApplyTremolo(samples, rate, 4.25, 0.015, rng.NextDouble() * Math.PI * 2.0);

            Synth.Normalise(samples, Peak);
            return samples;
        }

        // The other side: 55 Hz against 55.5 Hz (a slow beat every 2 s), a sub and two slightly sour
        // overtones, and a bed of low noise that swells once per loop.
        public static float[] ShiftDrone()
        {
            int rate = Synth.LoopRate;
            System.Random rng = new System.Random(23);

            float[] noise = Synth.Silence(LoopSeconds, rate);
            Synth.AddLoopingNoise(noise, rate, 120.0, 500.0, 48, 0.35, rng);
            Synth.ApplyTremolo(noise, rate, 0.25, 0.5, 0.0);

            float[] samples = noise;
            Synth.AddSine(samples, rate, 55.0, 1.0, 0.0);
            Synth.AddSine(samples, rate, 55.5, 0.9, 0.0);
            Synth.AddSine(samples, rate, 27.5, 0.4, 0.0);
            Synth.AddSine(samples, rate, 110.25, 0.35, 0.0);
            Synth.AddSine(samples, rate, 164.75, 0.2, 0.0);

            Synth.Normalise(samples, Peak);
            return samples;
        }

        // ---------- One-shots (short fades at both ends: they start and end at zero) ----------

        // One "lub-dub": two low thumps that fall in pitch, the second one shorter and softer.
        public static float[] Heartbeat()
        {
            int rate = Synth.OneShotRate;
            float[] samples = Synth.Silence(0.36, rate);

            AddThump(samples, rate, 0.0, 85.0, 50.0, 1.0, 0.07);
            AddThump(samples, rate, 0.14, 100.0, 60.0, 0.7, 0.05);

            return Finish(samples, rate, 0.003, 0.03);
        }

        // Small shop bell: two strikes (E6 then C6), each with bell-like overtones that are not
        // whole multiples of the base note.
        public static float[] DoorBell()
        {
            int rate = Synth.OneShotRate;
            float[] samples = Synth.Silence(1.4, rate);

            AddBellStrike(samples, rate, 0.0, 1318.5, 1.0);
            AddBellStrike(samples, rate, 0.22, 1046.5, 0.9);

            return Finish(samples, rate, 0.003, 0.05);
        }

        // Night watchman's pea whistle: two blasts (short, long) of a 2.7 kHz tone whose pitch and
        // loudness flutter 30 times per second. Starts with 0.3 s of silence so that it follows
        // the door bell instead of covering it.
        public static float[] Whistle()
        {
            int rate = Synth.OneShotRate;
            System.Random rng = new System.Random(37);
            float[] samples = Synth.Silence(0.95, rate);

            AddWhistleBlast(samples, rate, 0.30, 0.18, rng);
            AddWhistleBlast(samples, rate, 0.55, 0.38, rng);

            return Finish(samples, rate, 0.003, 0.01);
        }

        // "Ka-ching": two mechanical clicks, then a bright ring.
        public static float[] CashRegister()
        {
            int rate = Synth.OneShotRate;
            System.Random rng = new System.Random(41);
            float[] samples = Synth.Silence(0.9, rate);

            Synth.AddNoiseBurst(samples, rate, 0.0, 0.03, 0.8, 0.005, rng);
            Synth.AddDecayingSine(samples, rate, 0.0, 180.0, 0.6, 0.03);
            Synth.AddNoiseBurst(samples, rate, 0.06, 0.03, 0.6, 0.004, rng);

            Synth.AddDecayingSine(samples, rate, 0.11, 2217.0, 1.0, 0.30);
            Synth.AddDecayingSine(samples, rate, 0.11, 3332.0, 0.6, 0.22);
            Synth.AddDecayingSine(samples, rate, 0.11, 4421.0, 0.4, 0.15);
            Synth.AddDecayingSine(samples, rate, 0.11, 6645.0, 0.2, 0.10);

            return Finish(samples, rate, 0.002, 0.05);
        }

        // Correct refusal: a soft, neutral two-note "tu-dum" going down.
        public static float[] Refuse()
        {
            int rate = Synth.OneShotRate;
            float[] samples = Synth.Silence(0.24, rate);

            Synth.AddDecayingSine(samples, rate, 0.0, 330.0, 1.0, 0.05);
            Synth.AddDecayingSine(samples, rate, 0.0, 660.0, 0.2, 0.03);
            Synth.AddDecayingSine(samples, rate, 0.09, 262.0, 0.8, 0.05);

            return Finish(samples, rate, 0.003, 0.02);
        }

        // Wrong decision: two short low buzzes made of two notes that clash (110 and 116.5 Hz).
        public static float[] Error()
        {
            int rate = Synth.OneShotRate;
            float[] samples = Synth.Silence(0.36, rate);

            AddBuzz(samples, rate, 0.0, 0.12);
            AddBuzz(samples, rate, 0.16, 0.18);

            Finish(samples, rate, 0.003, 0.01);
            Synth.Normalise(samples, DensePeak);
            return samples;
        }

        // The ending stinger for each ending.
        public static float[] EndingStinger(EndingId ending)
        {
            switch (ending)
            {
                case EndingId.Morning: return EndingChime(false);
                case EndingId.Changed: return EndingChime(true);
                case EndingId.Seen: return EndingSeen();
                default: return EndingFired();
            }
        }

        // Morning: a gentle rising C major chord (C5 E5 G5 C6) that settles.
        // Changed (wrong = true): the same, but the E is flat and the last note lands below the C
        // with a second, slightly higher copy beating against it.
        private static float[] EndingChime(bool wrong)
        {
            int rate = Synth.OneShotRate;
            float[] samples = Synth.Silence(3.0, rate);

            double third = wrong ? Synth.Detune(659.25, -45.0) : 659.25;
            double top = wrong ? Synth.Detune(1046.5, -70.0) : 1046.5;

            AddChimeNote(samples, rate, 0.0, 523.25);
            AddChimeNote(samples, rate, 0.25, third);
            AddChimeNote(samples, rate, 0.5, 783.99);
            AddChimeNote(samples, rate, 0.75, top);
            if (wrong)
            {
                AddChimeNote(samples, rate, 0.75, Synth.Detune(top, 25.0));
            }

            return Finish(samples, rate, 0.003, 0.3);
        }

        // Seen: a cluster of clashing notes that swells for 1.5 s, sliding slightly upwards, and is cut off.
        private static float[] EndingSeen()
        {
            int rate = Synth.OneShotRate;
            const double swellSeconds = 1.5;
            double[] cluster = { 196.0, 207.65, 293.66, 311.13, 622.25, 1244.5, 1318.5 };
            float[] samples = Synth.Silence(1.8, rate);

            foreach (double frequency in cluster)
            {
                Synth.AddTone(samples, rate, 0.0, swellSeconds, frequency, frequency * 1.06, 1.0, 0.03);
            }

            // Loudness grows with the cube of time: quiet for a long while, then sudden.
            int swellSamples = (int)(swellSeconds * rate);
            for (int i = 0; i < swellSamples; i++)
            {
                double progress = (double)i / swellSamples;
                samples[i] *= (float)(progress * progress * progress);
            }

            return Finish(samples, rate, 0.003, 0.003);
        }

        // Fired: one dull tone sliding down an octave, with no expression at all.
        private static float[] EndingFired()
        {
            int rate = Synth.OneShotRate;
            float[] samples = Synth.Silence(1.4, rate);

            for (int harmonic = 1; harmonic <= 5; harmonic++)
            {
                Synth.AddTone(samples, rate, 0.0, 1.4, 311.13 * harmonic, 155.56 * harmonic, 1.0 / harmonic, 0.02);
            }

            Finish(samples, rate, 0.003, 0.15);
            Synth.Normalise(samples, DensePeak);
            return samples;
        }

        // ---------- Shared pieces ----------

        // Fades both ends and normalises: the last step of every one-shot.
        private static float[] Finish(float[] samples, int rate, double attackSeconds, double releaseSeconds)
        {
            Synth.ApplyFades(samples, rate, attackSeconds, releaseSeconds);
            Synth.Normalise(samples, Peak);
            return samples;
        }

        // One struck bell: the base note plus three overtones at the ratios of a small metal bell.
        // Higher overtones are quieter and die away faster.
        private static void AddBellStrike(float[] samples, int rate, double startSeconds, double frequency, double amplitude)
        {
            Synth.AddDecayingSine(samples, rate, startSeconds, frequency, amplitude, 0.35);
            Synth.AddDecayingSine(samples, rate, startSeconds, frequency * 2.76, amplitude * 0.5, 0.20);
            Synth.AddDecayingSine(samples, rate, startSeconds, frequency * 5.40, amplitude * 0.25, 0.12);
            Synth.AddDecayingSine(samples, rate, startSeconds, frequency * 8.93, amplitude * 0.1, 0.06);
        }

        // One soft chime note: the note and a quieter octave above it.
        private static void AddChimeNote(float[] samples, int rate, double startSeconds, double frequency)
        {
            Synth.AddDecayingSine(samples, rate, startSeconds, frequency, 1.0, 0.9);
            Synth.AddDecayingSine(samples, rate, startSeconds, frequency * 2.0, 0.2, 0.5);
        }

        // One heart thump: a low tone that drops in pitch and dies quickly. The octave above is mixed
        // in so that small laptop speakers still reproduce something.
        private static void AddThump(float[] samples, int rate, double startSeconds,
            double startFrequency, double endFrequency, double amplitude, double decaySeconds)
        {
            int start = (int)(startSeconds * rate);
            double phase = 0.0;
            for (int i = start; i < samples.Length; i++)
            {
                double t = (double)(i - start) / rate;
                double glide = Math.Exp(-t / 0.05); // 1 at the start, falls to 0
                double frequency = endFrequency + (startFrequency - endFrequency) * glide;
                double envelope = Math.Min(1.0, t / 0.004) * Math.Exp(-t / decaySeconds);
                samples[i] += (float)(amplitude * envelope * (Math.Sin(phase) + 0.3 * Math.Sin(2.0 * phase)));
                phase += Math.PI * 2.0 * frequency / rate;
            }
        }

        // One whistle blast: the pea inside makes pitch and loudness flutter (the trill).
        private static void AddWhistleBlast(float[] samples, int rate, double startSeconds, double lengthSeconds,
            System.Random rng)
        {
            const double carrierHz = 2700.0;
            const double trillHz = 30.0;
            const double trillPitchHz = 180.0; // how far the pitch swings up and down
            const double trillLoudness = 0.35; // how far the loudness swings

            int start = (int)(startSeconds * rate);
            int count = (int)(lengthSeconds * rate);
            double phase = 0.0;
            for (int i = 0; i < count; i++)
            {
                double t = (double)i / rate;
                double trill = Math.Sin(Math.PI * 2.0 * trillHz * t);
                double frequency = carrierHz + trillPitchHz * trill;
                double loudness = (1.0 - trillLoudness) + trillLoudness * Math.Sin(Math.PI * 2.0 * trillHz * t + 1.0);
                double envelope = Math.Min(1.0, Math.Min(t / 0.015, (lengthSeconds - t) / 0.03));
                double breath = 0.05 * (rng.NextDouble() * 2.0 - 1.0);
                double tone = Math.Sin(phase) + 0.15 * Math.Sin(2.0 * phase);
                samples[start + i] += (float)(envelope * loudness * (tone + breath));
                phase += Math.PI * 2.0 * frequency / rate;
            }
        }

        // One error buzz: odd harmonics (a hollow, buzzy colour) of two notes a semitone apart.
        private static void AddBuzz(float[] samples, int rate, double startSeconds, double lengthSeconds)
        {
            for (int harmonic = 1; harmonic <= 7; harmonic += 2)
            {
                double amplitude = 1.0 / harmonic;
                Synth.AddTone(samples, rate, startSeconds, lengthSeconds, 110.0 * harmonic, 110.0 * harmonic, amplitude, 0.008);
                Synth.AddTone(samples, rate, startSeconds, lengthSeconds, 116.54 * harmonic, 116.54 * harmonic, amplitude, 0.008);
            }
        }
    }
}
