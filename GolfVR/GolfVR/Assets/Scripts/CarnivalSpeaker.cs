using UnityEngine;

namespace GolfVR
{
    /// <summary>
    /// Plays a short looping calliope-style tune from a 3D positioned AudioSource,
    /// so players can localize the sound to the physical speaker prop it's on.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class CarnivalSpeaker : MonoBehaviour
    {
        [Range(0f, 1f)]
        public float volume = 0.45f;

        [Tooltip("Distance at which the sound is at full volume")]
        public float minDistance = 3f;

        [Tooltip("Distance at which the sound becomes inaudible")]
        public float maxDistance = 20f;

        private void Awake()
        {
            AudioSource source = GetComponent<AudioSource>();
            source.clip = GenerateCarnivalLoop();
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 1f;
            source.volume = volume;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();
        }

        /// <summary>
        /// Generates a seamlessly-looping carnival calliope tune procedurally:
        /// a bouncy melody with pitch vibrato over an "oom-pah" bass pulse,
        /// rather than a single bare arpeggio.
        /// </summary>
        private AudioClip GenerateCarnivalLoop()
        {
            int sampleRate = 44100;
            const float stepDuration = 0.2f;

            // A bouncy carousel-style phrase in C major.
            float[] melody =
            {
                523.25f, 659.25f, 783.99f, 659.25f, // C E G E
                523.25f, 587.33f, 659.25f, 783.99f, // C D E G
                880.00f, 783.99f, 659.25f, 587.33f, // A G E D
                523.25f, 659.25f, 783.99f, 1046.50f, // C E G C
            };

            // Oom-pah accompaniment: root/fifth pulse an octave-plus below.
            float[] bass = new float[melody.Length];
            for (int i = 0; i < bass.Length; i++)
            {
                bass[i] = (i % 2 == 0) ? 130.81f : 196.00f; // C3 / G3
            }

            int stepCount = melody.Length;
            float totalDuration = stepCount * stepDuration;
            int numSamples = (int)(sampleRate * totalDuration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                int step = Mathf.Min((int)(t / stepDuration), stepCount - 1);
                float stepT = t % stepDuration;

                // Sine-shaped envelope per step: zero at each boundary so the
                // whole clip starts and ends at silence and loops without clicks.
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(stepT / stepDuration));

                // Calliope-style pitch wobble on the melody voice.
                float vibrato = 1f + 0.03f * Mathf.Sin(2f * Mathf.PI * 5.5f * t);
                float melodyFreq = melody[step] * vibrato;

                float melodyTone = Mathf.Sin(2f * Mathf.PI * melodyFreq * t)
                                  + 0.55f * Mathf.Sin(2f * Mathf.PI * melodyFreq * 2f * t)
                                  + 0.30f * Mathf.Sin(2f * Mathf.PI * melodyFreq * 3f * t)
                                  + 0.12f * Mathf.Sin(2f * Mathf.PI * melodyFreq * 4f * t);

                float bassFreq = bass[step];
                float bassTone = Mathf.Sin(2f * Mathf.PI * bassFreq * t)
                                + 0.4f * Mathf.Sin(2f * Mathf.PI * bassFreq * 2f * t);

                samples[i] = (melodyTone * 0.6f + bassTone * 0.35f) * envelope * 0.35f;
            }

            AudioClip clip = AudioClip.Create("CarnivalLoopProcedural", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
