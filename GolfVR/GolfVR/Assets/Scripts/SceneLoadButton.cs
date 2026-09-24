using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR.InteractionSystem;

namespace GolfVR
{
    /// <summary>
    /// A physical push-button (SteamVR HoverButton) on the main-menu scene that
    /// loads the scene named in <see cref="sceneName"/>. The scene must be in
    /// the project's Build Settings.
    /// </summary>
    [RequireComponent(typeof(HoverButton))]
    public class SceneLoadButton : MonoBehaviour
    {
        [Tooltip("Name of the scene to load (as listed in Build Settings), e.g. ICARUS_v1")]
        public string sceneName;

        public AudioSource audioSource;

        private AudioClip _clip;
        private Renderer[] _renderers;
        private Color[] _originalColors;
        private bool _loading;

        private void Awake()
        {
            HoverButton hoverButton = GetComponent<HoverButton>();
            hoverButton.onButtonDown.AddListener(OnPressed);
            hoverButton.onButtonUp.AddListener(OnReleased);

            _renderers = GetComponentsInChildren<Renderer>();
            _originalColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++) _originalColors[i] = _renderers[i].material.color;

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
            }
            GenerateClip();
        }

        /// <summary>True if the scene can actually be loaded (it's in Build Settings).</summary>
        public bool SceneIsAvailable()
        {
            return !string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);
        }

        /// <summary>What a press does; separate from OnPressed so tests can call it.</summary>
        public void Load()
        {
            if (_loading) return;

            if (!SceneIsAvailable())
            {
                Debug.LogError($"[GolfVR] Menu: scene '{sceneName}' is not in Build Settings, cannot load it.");
                return;
            }

            _loading = true;
            Debug.Log($"[GolfVR] Menu: loading scene '{sceneName}'.");
            SceneManager.LoadScene(sceneName);
        }

        private void OnPressed(Hand hand)
        {
            for (int i = 0; i < _renderers.Length; i++) _renderers[i].material.color = Color.white;
            if (hand != null) hand.TriggerHapticPulse(1000);
            if (audioSource != null && _clip != null) audioSource.PlayOneShot(_clip, 0.8f);
            Load();
        }

        private void OnReleased(Hand hand)
        {
            for (int i = 0; i < _renderers.Length; i++) _renderers[i].material.color = _originalColors[i];
        }

        private void GenerateClip()
        {
            int sampleRate = 44100;
            int n = (int)(sampleRate * 0.25f);
            float[] s = new float[n];
            float[] f = { 523f, 659f, 784f };
            float noteLen = 0.25f / f.Length;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sampleRate;
                int k = Mathf.Min((int)(t / noteLen), f.Length - 1);
                s[i] = Mathf.Sin(2f * Mathf.PI * f[k] * t) * Mathf.Exp(-(t % noteLen) * 14f) * 0.5f;
            }
            _clip = AudioClip.Create("MenuSelectProcedural", n, 1, sampleRate, false);
            _clip.SetData(s, 0);
        }
    }
}
