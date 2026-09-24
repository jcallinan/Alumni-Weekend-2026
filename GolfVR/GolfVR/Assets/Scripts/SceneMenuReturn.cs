using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR;

namespace GolfVR
{
    /// <summary>
    /// Lets the player get back to the main menu from ANY scene without that
    /// scene having to know about it. It creates itself once at startup
    /// (RuntimeInitializeOnLoadMethod), survives scene loads
    /// (DontDestroyOnLoad), and does nothing while the menu itself is showing.
    ///
    /// Press and briefly HOLD the MENU button (the three-line button above the
    /// trackpad) on either Vive controller. Backup gesture: HOLD BOTH GRIP BUTTONS at the same time for a few seconds. A
    /// small message in front of the headset counts it down, so it can't happen
    /// by accident (holding one grip -- as when carrying the club -- does
    /// nothing). In the Editor, Escape also returns to the menu.
    /// </summary>
    public class SceneMenuReturn : MonoBehaviour
    {
        public const string MenuSceneName = "MainMenu";

        [Tooltip("Seconds both grips must be held")]
        public float holdSeconds = 2.5f;

        [Tooltip("Seconds the Vive MENU button (the three-line button above the trackpad) must be held")]
        public float menuHoldSeconds = 0.7f;

        private SteamVR_Action_Boolean _menu;
        private float _menuHeld;
        private SteamVR_Action_Boolean _grip;
        private bool _lookedUp;
        private float _held;
        private TextMesh _hud;
        private Font _font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<SceneMenuReturn>() != null) return;

            GameObject go = new GameObject("SceneMenuReturn");
            go.AddComponent<SceneMenuReturn>();
            DontDestroyOnLoad(go);
        }

        private static bool InMenu => SceneManager.GetActiveScene().name == MenuSceneName;

        private void Update()
        {
            if (InMenu)
            {
                SetHud(null);
                _held = 0f;
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GoToMenu();
                return;
            }

            if (!_lookedUp)
            {
                _lookedUp = true;
                try { _grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip"); }
                catch { _grip = null; }
                try { _menu = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Menu"); }
                catch { _menu = null; }
            }

            // The Vive MENU button (three lines): a short hold on either controller.
            bool menuDown = false;
            if (_menu != null)
            {
                try { menuDown = _menu.GetState(SteamVR_Input_Sources.Any); }
                catch { menuDown = false; }
            }

            if (menuDown)
            {
                _menuHeld += Time.unscaledDeltaTime;
                if (_menuHeld >= menuHoldSeconds)
                {
                    GoToMenu();
                    return;
                }
                SetHud("Returning to menu...");
                return;
            }
            _menuHeld = 0f;

            bool both = false;
            if (_grip != null)
            {
                try
                {
                    both = _grip.GetState(SteamVR_Input_Sources.LeftHand) && _grip.GetState(SteamVR_Input_Sources.RightHand);
                }
                catch { both = false; }
            }

            if (both)
            {
                _held += Time.unscaledDeltaTime;
                if (_held >= holdSeconds)
                {
                    GoToMenu();
                    return;
                }
                SetHud($"Returning to menu...\n{Mathf.CeilToInt(holdSeconds - _held)}");
            }
            else
            {
                _held = 0f;
                SetHud(null);
            }
        }

        public static void GoToMenu()
        {
            if (InMenu) return;

            if (!Application.CanStreamedLevelBeLoaded(MenuSceneName))
            {
                Debug.LogError($"[GolfVR] Cannot return to the menu: scene '{MenuSceneName}' is not in Build Settings.");
                return;
            }

            Debug.Log("[GolfVR] Returning to the main menu.");
            SceneManager.LoadScene(MenuSceneName);
        }

        // A small readable message parked in front of the player's eyes.
        private void SetHud(string message)
        {
            if (message == null)
            {
                if (_hud != null) _hud.gameObject.SetActive(false);
                return;
            }

            Camera cam = Camera.main;
            if (cam == null) return;

            if (_hud == null)
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                GameObject go = new GameObject("MenuReturnHud");
                _hud = go.AddComponent<TextMesh>();
                _hud.fontSize = 64;
                _hud.characterSize = 0.012f;
                _hud.anchor = TextAnchor.MiddleCenter;
                _hud.alignment = TextAlignment.Center;
                _hud.color = new Color(1f, 0.85f, 0.25f);
                if (_font != null)
                {
                    _hud.font = _font;
                    go.GetComponent<MeshRenderer>().sharedMaterial = _font.material;
                }
            }

            _hud.gameObject.SetActive(true);
            _hud.text = message;
            Transform t = _hud.transform;
            t.SetParent(cam.transform, false);
            t.localPosition = new Vector3(0f, -0.05f, 0.7f);
            t.localRotation = Quaternion.identity;
        }
    }
}
