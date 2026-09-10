using System.Collections;
using UnityEngine;

namespace GolfVR
{
    [RequireComponent(typeof(Collider))]
    public class GolfHole : MonoBehaviour
    {
        [Header("Hole Identity")]
        [Tooltip("Hole number (1, 2, or 3)")]
        public int holeNumber = 1;

        [Tooltip("Hole name")]
        public string holeName = "Panther Straightaway";

        [Tooltip("Par score for this hole")]
        public int par = 2;

        [Header("Course Navigation Points")]
        [Tooltip("Where the ball starts on this hole")]
        public Transform teePoint;

        [Tooltip("Where the player teleports/stands when starting this hole")]
        public Transform playerTeeLocation;

        [Header("Celebratory Effects")]
        [Tooltip("Optional animated flag")]
        public Transform flagTransform;

        [Tooltip("Confetti or particle celebration")]
        public ParticleSystem celebrationParticles;

        [Tooltip("Audio source for victory chime")]
        public AudioSource audioSource;
        public AudioClip celebrationFanfare;

        private bool _isCompleted = false;
        private AudioClip _generatedFanfareClip;

        public bool IsCompleted => _isCompleted;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.spatialBlend = 1.0f;
                    audioSource.playOnAwake = false;
                }
            }

            GenerateFanfareAudioClip();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCompleted) return;

            GolfBall ball = other.GetComponent<GolfBall>();
            if (ball != null)
            {
                StartCoroutine(VerifyAndSinkBall(ball));
            }
        }

        private IEnumerator VerifyAndSinkBall(GolfBall ball)
        {
            // Brief moment to ensure ball actually drops in and doesn't just skim over
            yield return new WaitForSeconds(0.12f);

            if (_isCompleted) yield break;

            // Check if ball is still close to the hole center
            float dist = Vector3.Distance(ball.transform.position, transform.position);
            if (dist < 0.6f)
            {
                _isCompleted = true;
                ball.StopBall();

                // Trigger celebratory fanfare and visuals
                PlayCelebration();

                if (MiniGolfGameManager.Instance != null)
                {
                    MiniGolfGameManager.Instance.OnHoleSunk(this);
                }
            }
        }

        public void PlayCelebration()
        {
            // 1. Particle effect
            if (celebrationParticles != null)
            {
                celebrationParticles.Play();
            }

            // 2. Victory chime / fanfare
            if (audioSource != null)
            {
                AudioClip clip = celebrationFanfare != null ? celebrationFanfare : _generatedFanfareClip;
                if (clip != null)
                {
                    audioSource.pitch = 1.0f;
                    audioSource.PlayOneShot(clip, 0.9f);
                }
            }

            // 3. Flag spin / bounce animation
            if (flagTransform != null)
            {
                StartCoroutine(AnimateFlag());
            }
        }

        private IEnumerator AnimateFlag()
        {
            float elapsed = 0f;
            float duration = 2.0f;
            Vector3 origScale = flagTransform.localScale;
            Quaternion origRot = flagTransform.localRotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // Spin around Y and slight celebration bounce
                flagTransform.Rotate(Vector3.up, 360f * Time.deltaTime * 2.0f, Space.World);
                float bounce = 1f + 0.15f * Mathf.Sin(progress * Mathf.PI * 4f);
                flagTransform.localScale = origScale * bounce;

                yield return null;
            }

            flagTransform.localScale = origScale;
            flagTransform.localRotation = origRot;
        }

        public void ResetHole()
        {
            _isCompleted = false;
        }

        /// <summary>
        /// Generates a triumphant 4-note chord fanfare procedurally
        /// </summary>
        private void GenerateFanfareAudioClip()
        {
            int sampleRate = 44100;
            float duration = 1.2f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            // Notes: C5 (523Hz), E5 (659Hz), G5 (784Hz), C6 (1046Hz) arpeggio
            float[] frequencies = { 523.25f, 659.25f, 783.99f, 1046.50f };
            float noteDuration = 0.22f;

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Min((int)(t / noteDuration), frequencies.Length - 1);
                float freq = frequencies[noteIndex];

                // Envelope per note
                float noteT = (t % noteDuration);
                float env = Mathf.Exp(-noteT * 3.5f);

                // Pleasant bell-like harmonics
                float v = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t);
                samples[i] = v * env * 0.4f;
            }

            _generatedFanfareClip = AudioClip.Create("VictoryFanfareProcedural", numSamples, 1, sampleRate, false);
            _generatedFanfareClip.SetData(samples, 0);
        }
    }
}
