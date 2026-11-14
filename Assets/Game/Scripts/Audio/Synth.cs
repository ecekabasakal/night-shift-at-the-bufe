using System;

namespace NightShift
{
    // Small building blocks for making sounds as float[] sample buffers (mono, values -1..1).
    // Pure maths: no Unity types, so the output can be checked outside Unity.
    public static class Synth
    {
        public const int OneShotRate = 44100; // samples per second for short sounds
        public const int LoopRate = 22050;    // half rate for the long loops (saves memory)

        private const double TwoPi = Math.PI * 2.0;

        // A silent buffer of the given length.
        public static float[] Silence(double seconds, int sampleRate)
        {
            return new float[(int)Math.Round(seconds * sampleRate)];
        }

        // Adds a steady sine wave over the whole buffer. Used for loops: if the frequency fits a whole
        // number of times into the buffer, the end joins the start without a click.
        public static void AddSine(float[] buffer, int sampleRate, double frequency, double amplitude, double phase)
        {
            double step = TwoPi * frequency / sampleRate;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] += (float)(amplitude * Math.Sin(phase + step * i));
            }
        }

        // Adds a sine that starts at startSeconds and dies away exponentially (a struck bell partial).
        // decaySeconds is the time to fall to about 37 %.
        public static void AddDecayingSine(float[] buffer, int sampleRate, double startSeconds,
            double frequency, double amplitude, double decaySeconds)
        {
            const double attackSeconds = 0.002;
            int start = (int)(startSeconds * sampleRate);
            for (int i = start; i < buffer.Length; i++)
            {
                double t = (double)(i - start) / sampleRate;
                double envelope = Math.Min(1.0, t / attackSeconds) * Math.Exp(-t / decaySeconds);
                buffer[i] += (float)(amplitude * envelope * Math.Sin(TwoPi * frequency * t));
            }
        }

        // Adds a tone of fixed loudness between startSeconds and startSeconds + lengthSeconds,
        // gliding from startFrequency to endFrequency, with soft edges (edgeSeconds) so it does not click.
        public static void AddTone(float[] buffer, int sampleRate, double startSeconds, double lengthSeconds,
            double startFrequency, double endFrequency, double amplitude, double edgeSeconds)
        {
            int start = (int)(startSeconds * sampleRate);
            int count = Math.Min((int)(lengthSeconds * sampleRate), buffer.Length - start);
            double phase = 0.0;
            for (int i = 0; i < count; i++)
            {
                double t = (double)i / sampleRate;
                double progress = (double)i / count;
                // Exponential glide: equal musical steps in equal time.
                double frequency = startFrequency * Math.Pow(endFrequency / startFrequency, progress);
                double envelope = Math.Min(1.0, Math.Min(t, lengthSeconds - t) / edgeSeconds);
                buffer[start + i] += (float)(amplitude * envelope * Math.Sin(phase));
                phase += TwoPi * frequency / sampleRate;
            }
        }

        // Adds a short burst of white noise that dies away exponentially (a click or a puff of air).
        public static void AddNoiseBurst(float[] buffer, int sampleRate, double startSeconds,
            double lengthSeconds, double amplitude, double decaySeconds, System.Random rng)
        {
            int start = (int)(startSeconds * sampleRate);
            int count = Math.Min((int)(lengthSeconds * sampleRate), buffer.Length - start);
            for (int i = 0; i < count; i++)
            {
                double t = (double)i / sampleRate;
                double noise = rng.NextDouble() * 2.0 - 1.0;
                buffer[start + i] += (float)(amplitude * Math.Exp(-t / decaySeconds) * noise);
            }
        }

        // Adds band-limited noise that loops seamlessly. It is built from many sines with random
        // frequencies between lowHz and highHz, each moved to the nearest frequency that fits a whole
        // number of times into the buffer.
        public static void AddLoopingNoise(float[] buffer, int sampleRate, double lowHz, double highHz,
            int partialCount, double amplitude, System.Random rng)
        {
            double loopSeconds = (double)buffer.Length / sampleRate;
            double partialAmplitude = amplitude / Math.Sqrt(partialCount);
            for (int p = 0; p < partialCount; p++)
            {
                double wanted = lowHz + (highHz - lowHz) * rng.NextDouble();
                double wholeCycles = Math.Round(wanted * loopSeconds);
                double phase = rng.NextDouble() * TwoPi;
                AddSine(buffer, sampleRate, wholeCycles / loopSeconds, partialAmplitude, phase);
            }
        }

        // Multiplies the buffer by (1 + depth * sine). Use a frequency that fits the loop for loops.
        public static void ApplyTremolo(float[] buffer, int sampleRate, double frequency, double depth, double phase)
        {
            double step = TwoPi * frequency / sampleRate;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] *= (float)(1.0 + depth * Math.Sin(phase + step * i));
            }
        }

        // Linear fade-in at the start and fade-out at the end, so a one-shot begins and ends at zero.
        public static void ApplyFades(float[] buffer, int sampleRate, double attackSeconds, double releaseSeconds)
        {
            int attack = Math.Min((int)(attackSeconds * sampleRate), buffer.Length);
            int release = Math.Min((int)(releaseSeconds * sampleRate), buffer.Length);
            for (int i = 0; i < attack; i++)
            {
                buffer[i] *= (float)i / attack;
            }
            for (int i = 0; i < release; i++)
            {
                buffer[buffer.Length - 1 - i] *= (float)i / release;
            }
        }

        // Scales the buffer so its loudest sample is exactly `peak` (keeps everything inside -1..1).
        public static void Normalise(float[] buffer, float peak)
        {
            float loudest = 0f;
            for (int i = 0; i < buffer.Length; i++)
            {
                loudest = Math.Max(loudest, Math.Abs(buffer[i]));
            }
            if (loudest <= 0f) return;

            float scale = peak / loudest;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] *= scale;
            }
        }

        // Frequency of a note that is `cents` hundredths of a semitone away from `frequency`.
        public static double Detune(double frequency, double cents)
        {
            return frequency * Math.Pow(2.0, cents / 1200.0);
        }
    }
}
