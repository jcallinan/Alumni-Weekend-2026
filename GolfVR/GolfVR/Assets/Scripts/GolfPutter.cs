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

        [Header("Generated Visual Mesh")]
        [Tooltip("Build simple shaft/grip/head meshes at runtime if the putter has no visible MeshRenderer yet")]
        public bool autoGenerateVisuals = true;

        [Tooltip("Radius of the generated shaft, in meters")]
        public float shaftRadius = 0.008f;

        [Tooltip("Radius of the generated grip wrap, in meters")]
        public float gripRadius = 0.014f;

        [Tooltip("Length of the generated grip wrap, in meters")]
        public float gripLength = 0.22f;

        [Tooltip("Color of the generated shaft and club head")]
        public Color shaftColor = new Color(0.75f, 0.75f, 0.78f);

        [Tooltip("Color of the generated grip wrap")]
        public Color gripColor = new Color(0.08f, 0.08f, 0.08f);

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
            BuildVisualsIfMissing();
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
        /// Builds a simple shaft/grip/head mesh at runtime so the putter is visible in VR.
        /// Skipped if a MeshRenderer already exists (e.g. a real model was added to the prefab).
        /// </summary>
        private void BuildVisualsIfMissing()
        {
            if (!autoGenerateVisuals) return;
            if (GetComponentInChildren<MeshRenderer>() != null) return;

            Material shaftMat = new Material(Shader.Find("Standard"));
            shaftMat.color = shaftColor;
            shaftMat.SetFloat("_Metallic", 0.8f);
            shaftMat.SetFloat("_Glossiness", 0.6f);

            Material gripMat = new Material(Shader.Find("Standard"));
            gripMat.color = gripColor;
            gripMat.SetFloat("_Metallic", 0f);
            gripMat.SetFloat("_Glossiness", 0.2f);

            // Shaft runs the full length from the grip point down to the club head
            CreateVisualBar("ShaftVisual", gripPoint.localPosition, clubHead.localPosition, shaftRadius, shaftMat);

            // Grip wrap covers the top portion of the shaft, starting at the hand attach point
            Vector3 shaftDir = (clubHead.localPosition - gripPoint.localPosition).normalized;
            if (shaftDir.sqrMagnitude > 0.0001f)
            {
                Vector3 gripTop = gripPoint.localPosition;
                Vector3 gripBottom = gripTop + shaftDir * gripLength;
                CreateVisualBar("GripVisual", gripTop, gripBottom, gripRadius, gripMat);
            }

            // Club head: simple mallet-style block matching the collider footprint
            BoxCollider headCollider = clubHead.GetComponent<BoxCollider>();
            Vector3 headSize = headCollider != null ? headCollider.size : new Vector3(0.12f, 0.04f, 0.035f);
            Vector3 headCenter = headCollider != null ? headCollider.center : Vector3.zero;

            GameObject headVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headVisual.name = "ClubHeadVisual";
            Destroy(headVisual.GetComponent<Collider>());
            headVisual.transform.SetParent(clubHead, false);
            headVisual.transform.localPosition = headCenter;
            headVisual.transform.localRotation = Quaternion.identity;
            headVisual.transform.localScale = headSize;
            headVisual.GetComponent<MeshRenderer>().sharedMaterial = shaftMat;
        }

        /// <summary>
        /// Creates a thin cylinder between two points local to the putter root, with no collider.
        /// </summary>
        private void CreateVisualBar(string name, Vector3 localA, Vector3 localB, float radius, Material mat)
        {
            Vector3 delta = localB - localA;
            float length = delta.magnitude;
            if (length < 0.001f) return;

            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bar.name = name;
            Destroy(bar.GetComponent<Collider>());
            bar.transform.SetParent(transform, false);
            bar.transform.localPosition = (localA + localB) * 0.5f;
            bar.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            bar.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
            bar.GetComponent<MeshRenderer>().sharedMaterial = mat;
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
