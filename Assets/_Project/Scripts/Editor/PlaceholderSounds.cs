using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Synthesizes the placeholder sound effects (M9 feel pass) into Assets/_Project/Audio/Generated as
// 16-bit mono .wav files. Re-running overwrites them; real sounds can simply replace the files.
public static class PlaceholderSounds {

    private const int SampleRate = 44100;
    private const string Folder = "Assets/_Project/Audio/Generated";

    [MenuItem("MonkeeHunt/Generate Placeholder Sounds")]
    public static void GenerateAll() {
        Directory.CreateDirectory(Folder);
        Write("HitTick", 0.06f, 0.8f, HitTick);
        Write("KillConfirm", 0.26f, 0.8f, KillConfirm);
        Write("Hurt", 0.22f, 0.9f, Hurt);
        Write("Squelch", 0.3f, 0.8f, Squelch);
        Write("ImpactDirt", 0.15f, 0.7f, ImpactDirt);
        Write("ImpactBody", 0.14f, 0.8f, ImpactBody);
        Write("TranqShot", 0.18f, 0.7f, TranqShot);
        Write("NetShot", 0.26f, 0.9f, NetShot);
        Write("Jump", 0.16f, 0.35f, Jump);
        Write("Land", 0.15f, 0.5f, Land);
        Write("Throw", 0.18f, 0.4f, Throw);
        Write("RockClack", 0.2f, 0.9f, RockClack);
        Write("DecoyScreech", 0.8f, 0.8f, DecoyScreech);
        AssetDatabase.Refresh();
        Debug.Log("Placeholder sounds written to " + Folder);
    }

    // ---- Sounds. Each fills a buffer of the requested length; Write normalizes the peak to `gain`. ----

    private static void HitTick(float[] s) {
        for (int i = 0; i < s.Length; i++) {
            var t = T(i);
            s[i] = (Sin(2400f, t) + 0.5f * Sin(3600f, t)) * Env(t, 0.001f, 0.015f);
        }
    }

    private static void KillConfirm(float[] s) {
        for (int i = 0; i < s.Length; i++) {
            var t = T(i);
            var a = t < 0.11f ? Tone(880f, t) * Env(t, 0.002f, 0.05f) : 0f;
            var tb = t - 0.09f;
            var b = tb > 0f ? Tone(1320f, tb) * Env(tb, 0.002f, 0.06f) : 0f;
            s[i] = a + b;
        }
    }

    private static void Hurt(float[] s) {
        var noise = new Noise(1);
        var low = new LowPass(400f);
        var phase = 0f;
        for (int i = 0; i < s.Length; i++) {
            var t = T(i);
            phase += Freq(Mathf.Lerp(110f, 45f, t / 0.22f));
            s[i] = Mathf.Sin(phase) * Env(t, 0.002f, 0.07f) + 0.6f * low.Step(noise.Next()) * Env(t, 0.001f, 0.03f);
        }
    }

    private static void Squelch(float[] s) {
        var noise = new Noise(2);
        var low = new LowPass(300f);
        var phase = 0f;
        for (int i = 0; i < s.Length; i++) {
            var t = T(i);
            low.SetCutoff(300f + 900f * Mathf.Abs(Mathf.Sin(2f * Mathf.PI * 14f * t)));
            phase += Freq(Mathf.Lerp(260f, 90f, t / 0.3f));
            var env = Env(t, 0.005f, 0.09f);
            s[i] = (1.4f * low.Step(noise.Next()) + 0.5f * Mathf.Sin(phase)) * env;
        }
    }

    private static void ImpactDirt(float[] s) {
        var noise = new Noise(3);
        var bright = new LowPass(2500f);
        var dull = new LowPass(300f);
        for (int i = 0; i < s.Length; i++) {
            var t = T(i);
            var n = noise.Next();
            s[i] = bright.Step(n) * Env(t, 0.001f, 0.035f) + 1.5f * dull.Step(n) * Env(t, 0.001f, 0.05f);
        }
    }

    private static void ImpactBody(float[] s) {
        var noise = new Noise(4);
        var low = new LowPass(700f);
        var phase = 0f;
        for (int i = 0; i < s.Length; i++) {
            var t = T(i);
            phase += Freq(Mathf.Lerp(140f, 60f, t / 0.14f));
            s[i] = Mathf.Sin(phase) * Env(t, 0.001f, 0.04f) + 0.8f * low.Step(noise.Next()) * Env(t, 0.001f, 0.025f);
        }
    }

    private static void TranqShot(float[] s) {
        var noise = new Noise(5);
        var low = new LowPass(1500f);
        for (int i = 0; i < s.Length; i++) {
            var t = T(i);
            var n = noise.Next();
            var high = n - low.Step(n);
            s[i] = high * Env(t, 0.002f, 0.04f) + 0.3f * Sin(600f, t) * Env(t, 0.001f, 0.01f);
        }
    }

    private static void NetShot(float[] s) {
        var noise = new Noise(6);
        var low = new LowPass(1200f);
        var airLow = new LowPass(2000f);
        var phase = 0f;
        for (int i = 0; i < s.Length; i++) {
            var t = T(i);
            phase += Freq(Mathf.Lerp(180f, 70f, t / 0.26f));
            var n = noise.Next();
            var air = n - airLow.Step(n);
            s[i] = Mathf.Sin(phase) * Env(t, 0.001f, 0.08f) + 0.7f * low.Step(n) * Env(t, 0.001f, 0.03f)
                + 0.3f * air * Env(t, 0.01f, 0.1f);
        }
    }

    private static void Jump(float[] s) {
        var noise = new Noise(7);
        var high = new LowPass(1500f);
        var low = new LowPass(400f);
        for (int i = 0; i < s.Length; i++) {
            var t = T(i);
            var n = noise.Next();
            s[i] = (high.Step(n) - low.Step(n)) * Env(t, 0.03f, 0.06f);
        }
    }

    private static void Land(float[] s) {
        var noise = new Noise(8);
        var low = new LowPass(350f);
        var phase = 0f;
        for (int i = 0; i < s.Length; i++) {
            var t = T(i);
            phase += Freq(Mathf.Lerp(80f, 50f, t / 0.15f));
            s[i] = 1.5f * low.Step(noise.Next()) * Env(t, 0.001f, 0.04f) + 0.6f * Mathf.Sin(phase) * Env(t, 0.001f, 0.05f);
        }
    }

    private static void Throw(float[] s) {
        var noise = new Noise(9);
        var high = new LowPass(600f);
        var low = new LowPass(300f);
        for (int i = 0; i < s.Length; i++) {
            var t = T(i);
            high.SetCutoff(Mathf.Lerp(600f, 2500f, t / 0.18f));
            var n = noise.Next();
            s[i] = (high.Step(n) - low.Step(n)) * Env(t, 0.04f, 0.05f);
        }
    }

    private static void RockClack(float[] s) {
        var noise = new Noise(10);
        var low = new LowPass(4000f);
        for (int i = 0; i < s.Length; i++) {
            var t = T(i);
            s[i] = Clack(t, noise, low) + 0.6f * Clack(t - 0.07f, noise, low);
        }
    }

    private static float Clack(float t, Noise noise, LowPass low) {
        if (t < 0f) {
            return 0f;
        }
        var n = noise.Next();
        var click = (n - low.Step(n)) * Env(t, 0.0005f, 0.005f);
        return (Sin(2200f, t) + 0.7f * Sin(3100f, t)) * Env(t, 0.0005f, 0.012f) + click;
    }

    private static void DecoyScreech(float[] s) {
        var phase = 0f;
        for (int i = 0; i < s.Length; i++) {
            var t = T(i);
            // Two calls: "ooh" (rising) then "aah" (falling).
            float local, length, from, peak, to;
            if (t < 0.35f) {
                local = t; length = 0.35f; from = 700f; peak = 1300f; to = 1100f;
            } else if (t >= 0.42f) {
                local = t - 0.42f; length = 0.38f; from = 1200f; peak = 1400f; to = 800f;
            } else {
                s[i] = 0f;
                continue;
            }
            var u = local / length;
            var pitch = u < 0.4f ? Mathf.Lerp(from, peak, u / 0.4f) : Mathf.Lerp(peak, to, (u - 0.4f) / 0.6f);
            pitch += 40f * Mathf.Sin(2f * Mathf.PI * 25f * t);
            phase += Freq(pitch);
            var wave = 0f;
            for (int h = 1; h <= 5; h++) {
                wave += Mathf.Sin(phase * h) / h;
            }
            var env = Mathf.Clamp01(local / 0.02f) * Mathf.Clamp01((length - local) / 0.08f);
            s[i] = wave * env;
        }
    }

    // ---- Helpers ----

    private static float T(int i) => i / (float)SampleRate;

    private static float Freq(float hz) => 2f * Mathf.PI * hz / SampleRate;

    private static float Sin(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);

    // Sine with a little 2nd and 3rd harmonic: a softer, rounder beep.
    private static float Tone(float hz, float t) => Sin(hz, t) + 0.3f * Sin(hz * 2f, t) + 0.1f * Sin(hz * 3f, t);

    // Linear attack, exponential decay.
    private static float Env(float t, float attack, float decay) {
        if (t < 0f) {
            return 0f;
        }
        return t < attack ? t / attack : Mathf.Exp(-(t - attack) / decay);
    }

    private class Noise {
        private readonly System.Random random;
        public Noise(int seed) => random = new System.Random(seed);
        public float Next() => (float)(random.NextDouble() * 2.0 - 1.0);
    }

    // One-pole low-pass filter.
    private class LowPass {
        private float a;
        private float y;
        public LowPass(float cutoff) => SetCutoff(cutoff);
        public void SetCutoff(float cutoff) => a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / SampleRate);
        public float Step(float x) => y += a * (x - y);
    }

    private static void Write(string name, float seconds, float gain, Action<float[]> fill) {
        var samples = new float[Mathf.CeilToInt(seconds * SampleRate)];
        fill(samples);
        var peak = 0f;
        foreach (var v in samples) {
            peak = Mathf.Max(peak, Mathf.Abs(v));
        }
        var scale = peak > 0f ? gain / peak : 0f;
        // Short fade-out so nothing ends on a click.
        var fade = Mathf.Min(samples.Length, SampleRate / 200);
        for (int i = 0; i < fade; i++) {
            samples[samples.Length - 1 - i] *= i / (float)fade;
        }

        using var stream = new FileStream(Path.Combine(Folder, name + ".wav"), FileMode.Create);
        using var writer = new BinaryWriter(stream);
        var dataBytes = samples.Length * 2;
        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + dataBytes);
        writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(SampleRate);
        writer.Write(SampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(dataBytes);
        foreach (var v in samples) {
            writer.Write((short)Mathf.Clamp(Mathf.RoundToInt(v * scale * 32767f), -32768, 32767));
        }
    }
}
