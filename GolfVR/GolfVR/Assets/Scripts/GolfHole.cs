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

            if (celebrationParticles == null)
            {
                celebrationParticles = CreateDefaultCelebrationParticles();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCompleted) return;

            GolfBall ball = other.GetComponent<GolfBall>();
            if (ball != null)
            {
                // Cut its speed immediately so a fast putt can't blow straight
                // through the trigger before OnTriggerStay gets a chance to sink it.
                ball.DampenForCup();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (_isCompleted) return;

            GolfBall ball = other.GetComponent<GolfBall>();
            if (ball == null) return;

            // Checked every physics frame the ball is inside the trigger, rather
            // than a single delayed snapshot -- a one-shot check could catch the
            // ball mid-roll on the far side of the cup and never retry even if
            // it settles in a moment later.
            float dist = Vector3.Distance(ball.transform.position, transform.position);
            if (dist < 0.6f)
            {
                Sink(ball, snapBallToHole: false);
            }
        }

        private void Sink(GolfBall ball, bool snapBallToHole)
        {
            _isCompleted = true;
            if (ball != null)
            {
                if (snapBallToHole)
                {
                    // Only used by the debug force-sink path, where the ball
                    // hasn't actually rolled into the cup and needs a visual
                    // nudge. SpawnAtTee raycasts down to the real surface
                    // first -- directly teleporting to transform.position
                    // (the hole trigger's own anchor, which can sit at or
                    // below the green's solid collider) used to embed the
                    // ball in that geometry, and the physics engine would
                    // violently eject it out of view on the next physics
                    // step the instant it settled here.
                    ball.SpawnAtTee(transform.position);
                }
                ball.StopBall();
            }

            // Trigger celebratory fanfare and visuals
            PlayCelebration();

            if (MiniGolfGameManager.Instance != null)
            {
                MiniGolfGameManager.Instance.OnHoleSunk(this);
            }
        }

        /// <summary>
        /// Testing convenience: forces this hole to sink right now, running
        /// the exact same celebration/scoring path a real putt would (ball
        /// snapped into the cup, confetti, fanfare, fireworks, hole
        /// advance) without needing to actually putt the ball in. Right-click
        /// the MiniGolfGameManager component in the Inspector during Play
        /// mode and choose "DEBUG: Sink Current Hole" to call it.
        /// </summary>
        [ContextMenu("DEBUG: Force Sink This Hole")]
        public void DebugForceSink()
        {
            if (_isCompleted)
            {
                Debug.Log($"[GolfVR] {name} is already completed.");
                return;
            }

            // MiniGolfGameManager.OnHoleSunk scores against whatever hole
            // it currently thinks is active (_currentHoleIndex), not the
            // specific GolfHole instance passed in -- so force-sinking a
            // hole other than the active one would advance the wrong
            // counter and show the wrong par/hole number in the banner.
            // Right-clicking a hole in the Hierarchy and choosing this
            // command by hand only makes sense for the active one, so warn
            // rather than silently producing a mismatched scoreboard.
            if (MiniGolfGameManager.Instance != null
                && MiniGolfGameManager.Instance.holes != null
                && MiniGolfGameManager.Instance.CurrentHoleIndex < MiniGolfGameManager.Instance.holes.Length
                && MiniGolfGameManager.Instance.holes[MiniGolfGameManager.Instance.CurrentHoleIndex] != this)
            {
                Debug.LogWarning($"[GolfVR] {name} is not the active hole (current is hole {MiniGolfGameManager.Instance.CurrentHoleNumber}) -- use MiniGolfGameManager's \"DEBUG: Sink Current Hole\" instead, or this will score against the wrong hole.");
                return;
            }

            GolfBall ball = MiniGolfGameManager.Instance != null ? MiniGolfGameManager.Instance.golfBall : null;
            if (ball == null)
            {
                ball = FindObjectOfType<GolfBall>();
            }

            Sink(ball, snapBallToHole: true);
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

            // 4. Alumni Weekend fireworks: night sky, fireworks, horns, back to day
            if (FireworksCelebrationController.Instance != null)
            {
                FireworksCelebrationController.Instance.PlayFireworksCelebration();
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

        /// <summary>
        /// Builds a simple confetti-burst particle effect so sinking the ball
        /// has a visual payoff, not just a sound.
        /// </summary>
        private ParticleSystem CreateDefaultCelebrationParticles()
        {
            GameObject psObject = new GameObject("CelebrationConfetti");
            psObject.transform.SetParent(transform, false);
            psObject.transform.localPosition = Vector3.up * 0.3f;

            ParticleSystem ps = psObject.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 1.5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 1.2f;
            main.startSpeed = 3.0f;
            main.startSize = 0.08f;
            main.gravityModifier = 1.0f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.25f, 0.25f), new Color(0.25f, 0.55f, 1f));

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.1f;

            return ps;
        }
    }
}
