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
        /// Generates a short, seamlessly-looping carnival calliope melody procedurally.
        /// </summary>
        private AudioClip GenerateCarnivalLoop()
        {
            int sampleRate = 44100;
            float[] noteFrequencies = { 523.25f, 659.25f, 783.99f, 1046.50f, 783.99f, 659.25f, 587.33f, 523.25f }; // C E G C G E D C
            float noteDuration = 0.22f;
            float totalDuration = noteFrequencies.Length * noteDuration;
            int numSamples = (int)(sampleRate * totalDuration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Min((int)(t / noteDuration), noteFrequencies.Length - 1);
                float freq = noteFrequencies[noteIndex];

                // Sine-shaped envelope per note: zero at each note boundary so the
                // whole clip starts and ends at silence and loops without clicks.
                float noteT = t % noteDuration;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(noteT / noteDuration));

                // Bright, harmonic-rich tone approximating a calliope/carousel organ.
                float v = Mathf.Sin(2f * Mathf.PI * freq * t)
                        + 0.5f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t)
                        + 0.25f * Mathf.Sin(2f * Mathf.PI * freq * 3f * t);

                samples[i] = v * envelope * 0.3f;
            }

            AudioClip clip = AudioClip.Create("CarnivalLoopProcedural", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
