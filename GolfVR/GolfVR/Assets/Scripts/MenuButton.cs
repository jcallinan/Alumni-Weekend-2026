using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GolfVR
{
    /// <summary>
    /// A mouse-clickable button on the (flat, non-VR) main menu. Click it to load
    /// the scene named in <see cref="sceneName"/> (which must be in Build
    /// Settings), or -- if <see cref="quitApplication"/> is set -- to quit.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MenuButton : MonoBehaviour
    {
        public string sceneName;
        public bool quitApplication;

        [Tooltip("Optional text that shows a message if the scene can't be loaded")]
        public Text statusText;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Activate);
        }

        /// <summary>True if the scene can actually be loaded (it's in Build Settings).</summary>
        public bool SceneIsAvailable()
        {
            return !string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);
        }

        public void Activate()
        {
            if (quitApplication)
            {
                Debug.Log("[GolfVR] Menu: quitting.");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                return;
            }

            if (!SceneIsAvailable())
            {
                string msg = $"Scene '{sceneName}' is not in Build Settings.";
                Debug.LogError("[GolfVR] Menu: " + msg);
                if (statusText != null) statusText.text = msg;
                return;
            }

            Debug.Log($"[GolfVR] Menu: loading scene '{sceneName}'.");
            SceneManager.LoadScene(sceneName);
        }
    }
}
