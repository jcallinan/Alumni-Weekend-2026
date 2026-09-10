using System.Collections;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace GolfVR
{
    [RequireComponent(typeof(Rigidbody))]
    public class GolfPutter : MonoBehaviour
    {
        [Header("Putter Components")]
        [Tooltip("The putter head transform that impacts the ball")]
        public Transform clubHead;

        [Tooltip("Grip attachment point for the VR hand")]
        public Transform gripPoint;

        [Header("Striking Physics")]
        [Tooltip("Multiplier applied to swing velocity when striking the ball")]
        [Range(1.0f, 5.0f)]
        public float powerMultiplier = 2.2f;

        [Tooltip("Minimum club head speed required to register a putt stroke")]
        public float minSwingSpeed = 0.15f;

        [Tooltip("Maximum velocity that can be imparted on the ball")]
        public float maxBallSpeed = 15.0f;

        [Header("SteamVR / Haptics")]
        [Tooltip("Duration of haptic buzz in seconds on ball strike")]
        public float hapticDuration = 0.12f;

        [Tooltip("Frequency of haptic vibration on strike")]
        public float hapticFrequency = 160f;

        [Tooltip("Amplitude of haptic buzz (0 to 1)")]
        [Range(0f, 1f)]
        public float hapticAmplitude = 0.75f;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip customHitClip;

        // Velocity tracking for club head
        private Vector3 _lastHeadPosition;
        private Vector3 _headVelocity;
        private Hand _currentHoldingHand;
        private Interactable _interactable;
        private Rigidbody _rigidbody;
        private AudioClip _generatedHitClip;

        public bool IsHeld => _currentHoldingHand != null;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _interactable = GetComponent<Interactable>();

            if (clubHead == null)
            {
                clubHead = transform.Find("ClubHead") ?? transform;
            }

            if (gripPoint == null)
            {
                gripPoint = transform.Find("Grip") ?? transform;
            }

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

            GeneratePuttAudioClip();
        }

        private void Start()
        {
            _lastHeadPosition = clubHead.position;

            if (_interactable != null)
            {
                _interactable.onAttachedToHand += OnAttached;
                _interactable.onDetachedFromHand += OnDetached;
            }
        }

        private void OnDestroy()
        {
            if (_interactable != null)
            {
                _interactable.onAttachedToHand -= OnAttached;
                _interactable.onDetachedFromHand -= OnDetached;
            }
        }

        private void OnAttached(Hand hand)
        {
            _currentHoldingHand = hand;
        }

        private void OnDetached(Hand hand)
        {
            if (_currentHoldingHand == hand)
            {
                _currentHoldingHand = null;
            }
        }

        private void FixedUpdate()
        {
            // Estimate swing velocity of the putter club head
            Vector3 currentPos = clubHead.position;
            _headVelocity = (currentPos - _lastHeadPosition) / Time.fixedDeltaTime;
            _lastHeadPosition = currentPos;
        }

        private void OnCollisionEnter(Collision collision)
        {
            GolfBall ball = collision.gameObject.GetComponent<GolfBall>();
            if (ball == null) return;

            // Compute impact speed
            float swingSpeed = _headVelocity.magnitude;
            if (swingSpeed < minSwingSpeed) return;

            // Compute hit direction along the swing velocity or collision normal
            Vector3 hitDirection = _headVelocity.normalized;
            if (hitDirection.sqrMagnitude < 0.01f)
            {
                hitDirection = -collision.contacts[0].normal;
            }

            // Keep putt mostly on the ground plane unless lofted
            hitDirection.y = Mathf.Clamp(hitDirection.y, 0.0f, 0.25f);
            hitDirection.Normalize();

            float strikePower = Mathf.Min(swingSpeed * powerMultiplier, maxBallSpeed);
            Vector3 impulse = hitDirection * strikePower;

            // Hit the ball
            ball.ReceivePutt(impulse);

            // Play feedback
            PlayHitSound(strikePower);
            TriggerHapticPulse();
        }

        private void TriggerHapticPulse()
        {
            if (_currentHoldingHand != null)
            {
                try
                {
                    _currentHoldingHand.TriggerHapticPulse(hapticDuration, hapticFrequency, hapticAmplitude);
                }
                catch
                {
                    // Fallback or ignore if openvr handles elsewhere
                }
            }
        }

        private void PlayHitSound(float strikePower)
        {
            if (audioSource == null) return;

            float volume = Mathf.Clamp01(strikePower / 5.0f);
            AudioClip clip = customHitClip != null ? customHitClip : _generatedHitClip;
            if (clip != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(clip, Mathf.Max(0.3f, volume));
            }
        }

        /// <summary>
        /// Generates a crisp golf ball impact audio clip procedurally at runtime
        /// </summary>
        private void GeneratePuttAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.08f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 60f); // Quick transient decay
                // Blend of a resonant "thwack" frequency ~480Hz and a high click ~1800Hz
                float tone1 = Mathf.Sin(2f * Mathf.PI * 480f * t);
                float tone2 = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.4f;
                float noise = (Random.value * 2f - 1f) * 0.15f;
                samples[i] = (tone1 + tone2 + noise) * envelope;
            }

            _generatedHitClip = AudioClip.Create("PuttStrikeProcedural", numSamples, 1, sampleRate, false);
            _generatedHitClip.SetData(samples, 0);
        }

        /// <summary>
        /// Snaps putter back to the player's vicinity or tee
        /// </summary>
        public void ResetToPosition(Vector3 position, Quaternion rotation)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            transform.position = position;
            transform.rotation = rotation;
        }
    }
}
