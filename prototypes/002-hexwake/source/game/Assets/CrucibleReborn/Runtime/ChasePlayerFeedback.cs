using System;
using UnityEngine;

namespace GameDevHarness.CrucibleReborn
{
    public enum ChasePlayerCue
    {
        Jump, Land, Damage, PickupXp, PickupHealth, ClassSelect, UpgradeSelect,
        Leap, Teleport, ShieldDash, StrongLand, FootstepLeft, FootstepRight
    }

    // Call Play only on accepted gameplay/UI events, never from a held input.
    // Advance drives presentation time; no cue is emitted by advancing a frame.
    public sealed class ChasePlayerFeedback : IDisposable
    {
        public const int VoiceCapacity = 4;
        public const float MixBudget = .18f;
        const int CueCapacity = 13;
        public const float FootstepStride = 1.65f;
        readonly GameObject root;
        readonly Camera camera;
        readonly AudioSource[] voices = new AudioSource[VoiceCapacity];
        readonly float[] remaining = new float[VoiceCapacity];
        readonly AudioClip[] clips = new AudioClip[CueCapacity];
        readonly float[] cooldowns = new float[CueCapacity];
        readonly int[] counts = new int[CueCapacity];
        readonly ChasePlayerCue[] activeCues = new ChasePlayerCue[VoiceCapacity];
        float strideDistance;
        bool rightFoot;
        bool freshDamage;
        bool disposed;

        public float DamageFlash { get; private set; }
        public int SubmittedCues { get; private set; }
        public int PlayedCues { get; private set; }
        public int SuppressedCues { get; private set; }
        public int RecycledVoices { get; private set; }
        public bool IsDisposed => disposed;
        public ChasePlayerCue LastCue { get; private set; }
        public int CueCount(ChasePlayerCue cue) { return counts[(int)cue]; }
        public int ActiveVoiceCount
        {
            get
            {
                int count = 0;
                foreach (float time in remaining) if (time > 0) count++;
                return count;
            }
        }

        public static ChasePlayerFeedback Create(Transform parent, Camera camera)
        {
            return new ChasePlayerFeedback(parent, camera);
        }

        ChasePlayerFeedback(Transform parent, Camera camera)
        {
            this.camera = camera;
            root = new GameObject("G bounded player event feedback");
            root.transform.SetParent(parent, false);
            root.transform.position = camera.transform.position;
            for (int i = 0; i < clips.Length; i++)
            {
                float[] samples = Synthesize((ChasePlayerCue)i);
                clips[i] = AudioClip.Create("G original player " + (ChasePlayerCue)i,
                    samples.Length, 1, ChaseEffects.SampleRate, false);
                clips[i].SetData(samples, 0);
            }
            for (int i = 0; i < voices.Length; i++)
            {
                var go = new GameObject("Player cue voice " + i);
                go.transform.SetParent(root.transform, false);
                voices[i] = go.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0;
                voices[i].dopplerLevel = 0;
                voices[i].priority = 24;
            }
        }

        public void Play(ChasePlayerCue cue)
        {
            if (disposed) return;
            SubmittedCues++;
            // Blood edges remain available even if a clustered audio cue is gated.
            if (cue == ChasePlayerCue.Damage) { DamageFlash = 1; freshDamage = true; }
            int id = (int)cue;
            if (cooldowns[id] > 0) { SuppressedCues++; return; }
            cooldowns[id] = cue == ChasePlayerCue.PickupXp ? .1f : .06f;
            int slot = 0;
            for (int i = 0; i < voices.Length; i++)
            {
                if (remaining[i] <= 0) { slot = i; break; }
                if (remaining[i] < remaining[slot]) slot = i;
            }
            if (remaining[slot] > 0) RecycledVoices++;
            voices[slot].Stop();
            voices[slot].clip = clips[id];
            activeCues[slot] = cue;
            remaining[slot] = clips[id].length;
            Mix();
            voices[slot].Play();
            counts[id]++;
            PlayedCues++;
            LastCue = cue;
        }

        // Distance comes from the collision-resolved move, not input or desired speed.
        public void UpdateFootsteps(float distance, bool grounded, bool playing)
        {
            if (disposed) return;
            if (!playing || !grounded || distance <= .0001f)
            {
                strideDistance = 0;
                if (!playing)
                    for (int i = 0; i < voices.Length; i++)
                        if (activeCues[i] == ChasePlayerCue.FootstepLeft ||
                            activeCues[i] == ChasePlayerCue.FootstepRight)
                        { voices[i].Stop(); remaining[i] = 0; }
                return;
            }
            strideDistance += distance;
            if (strideDistance < FootstepStride) return;
            strideDistance %= FootstepStride;
            Play(rightFoot ? ChasePlayerCue.FootstepRight : ChasePlayerCue.FootstepLeft);
            rightFoot = !rightFoot;
        }

        public void ResetFootsteps() { strideDistance = 0; rightFoot = false; }

        public void Advance(float dt)
        {
            if (disposed) return;
            dt = Mathf.Max(0, dt);
            root.transform.position = camera.transform.position;
            // The next Update's delta describes the preceding frame, which can
            // include a heavy arena reset before this damage event existed.
            // Present one frame before decay so an accepted hit cannot vanish unseen.
            if (!freshDamage) DamageFlash = Mathf.Max(0, DamageFlash - dt * 2.2f);
            freshDamage = false;
            for (int i = 0; i < cooldowns.Length; i++)
                cooldowns[i] = Mathf.Max(0, cooldowns[i] - dt);
            for (int i = 0; i < remaining.Length; i++)
            {
                remaining[i] = Mathf.Max(0, remaining[i] - dt);
                if (remaining[i] <= 0) voices[i].Stop();
            }
            Mix();
        }

        void Mix()
        {
            float volume = MixBudget / Mathf.Max(1, ActiveVoiceCount);
            for (int i = 0; i < voices.Length; i++)
                voices[i].volume = remaining[i] > 0 ? volume *
                    (activeCues[i] == ChasePlayerCue.FootstepLeft ||
                     activeCues[i] == ChasePlayerCue.FootstepRight ? .52f : 1) : 0;
        }

        // Original deterministic foley, breath, metal and arcane phrases.
        // Peak normalization and a short attack/release prevent clicks and harsh
        // stacked transients. Nothing allocates or synthesizes on a gameplay event.
        static float[] Synthesize(ChasePlayerCue cue)
        {
            double duration = cue == ChasePlayerCue.FootstepLeft || cue == ChasePlayerCue.FootstepRight ? .19 :
                cue == ChasePlayerCue.StrongLand ? .6 :
                cue == ChasePlayerCue.Teleport ? .48 : .32;
            int frames = (int)(duration * ChaseEffects.SampleRate);
            var samples = new float[frames];
            uint seed = (uint)(0x7341 + (int)cue * 7919);
            double low = 0, mid = 0, phase = 0, peak = 0;
            for (int i = 0; i < frames; i++)
            {
                double t = (double)i / ChaseEffects.SampleRate;
                seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
                double noise = seed / (double)uint.MaxValue * 2 - 1;
                low += .018 * (noise - low);
                mid += .12 * (noise - mid);
                double value;
                switch (cue)
                {
                    case ChasePlayerCue.FootstepLeft:
                    case ChasePlayerCue.FootstepRight:
                        // Heel impact, gritty ash compression, then a short leather scrape.
                        double heel = cue == ChasePlayerCue.FootstepLeft ? 88 : 94;
                        value = (Math.Sin(t * 2 * Math.PI * heel) * .4 + low * 3.2)
                            * Math.Exp(-t * 42) + mid * 1.5 * Math.Exp(-Math.Abs(t - .037) * 47)
                            + noise * .11 * Math.Exp(-Math.Abs(t - .085) * 32);
                        break;
                    case ChasePlayerCue.Jump:
                        value = mid * Math.Exp(-t * 23) + low * 3 * Math.Exp(-t * 14);
                        break;
                    case ChasePlayerCue.Land:
                    case ChasePlayerCue.StrongLand:
                        phase += 2 * Math.PI * (cue == ChasePlayerCue.Land ? 96 : 54) / ChaseEffects.SampleRate;
                        value = (Math.Sin(phase) * .55 + low * 3) * Math.Exp(-t * (cue == ChasePlayerCue.Land ? 24 : 10))
                            + mid * .6 * Math.Exp(-t * 65);
                        break;
                    case ChasePlayerCue.Damage:
                        value = (Math.Sin(t * 2 * Math.PI * 71) * .45 + low * 4) * Math.Exp(-t * 16)
                            + mid * Math.Exp(-t * 45);
                        break;
                    case ChasePlayerCue.PickupXp:
                    case ChasePlayerCue.PickupHealth:
                        double frequency = cue == ChasePlayerCue.PickupXp ? 660 : 330;
                        value = (Math.Sin(t * 2 * Math.PI * frequency) * .4
                            + Math.Sin(t * 2 * Math.PI * frequency * 1.498) * .2) * Math.Exp(-t * 16)
                            + low * Math.Exp(-t * 35);
                        break;
                    case ChasePlayerCue.ClassSelect:
                    case ChasePlayerCue.UpgradeSelect:
                        double pitch = cue == ChasePlayerCue.ClassSelect ? 196 : 262;
                        value = (Math.Sin(t * 2 * Math.PI * pitch) * .5
                            + Math.Sin(t * 2 * Math.PI * pitch * 2.73) * .16) * Math.Exp(-t * 12)
                            + mid * .35 * Math.Exp(-Math.Abs(t - .045) * 85);
                        break;
                    case ChasePlayerCue.Leap:
                        value = (mid * 2 + Math.Sin(t * 2 * Math.PI * 82) * .3)
                            * Math.Sin(Math.Min(1, t / duration) * Math.PI) * Math.Exp(-t * 6);
                        break;
                    case ChasePlayerCue.Teleport:
                        phase += 2 * Math.PI * (70 + 430 * Math.Exp(-t * 11)) / ChaseEffects.SampleRate;
                        value = (Math.Sin(phase) * .4 + low * 3 + mid * .5)
                            * Math.Exp(-Math.Abs(t - .1) * 15);
                        break;
                    default: // Shield-first dash: dry plate catch then rushing cloth.
                        value = (Math.Sin(t * 2 * Math.PI * 431) * .35
                            + Math.Sin(t * 2 * Math.PI * 719) * .18) * Math.Exp(-t * 25)
                            + mid * 1.4 * Math.Exp(-Math.Abs(t - .08) * 20);
                        break;
                }
                value *= Math.Min(1, t / .004) * Math.Min(1, (duration - t) / .035);
                samples[i] = (float)value;
                peak = Math.Max(peak, Math.Abs(value));
            }
            float scale = (float)(.55 / Math.Max(.001, peak));
            for (int i = 0; i < samples.Length; i++) samples[i] *= scale;
            return samples;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            DamageFlash = 0;
            for (int i = 0; i < voices.Length; i++)
            { voices[i].Stop(); remaining[i] = 0; }
            foreach (var clip in clips) UnityEngine.Object.Destroy(clip);
            UnityEngine.Object.Destroy(root);
        }
    }
}
