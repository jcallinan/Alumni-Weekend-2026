using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace GolfVR.EditorTools
{
    /// <summary>
    /// Checks the main menu: it is the first scene in Build Settings, every menu
    /// button targets a scene that exists and is enabled in Build Settings, the
    /// buttons have no dangling persistent listeners, and the return-to-menu
    /// component's scene name matches the menu scene.
    /// </summary>
    public static class MainMenuTest
    {
        private static int _failures;

        [MenuItem("Tools/GolfVR/Main Menu Test")]
        public static void Run()
        {
            _failures = 0;
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0 || scenes[0].path != "Assets/Scenes/MainMenu.unity" || !scenes[0].enabled)
                Fail("MainMenu must be the first, enabled scene in Build Settings.");

            if (SceneMenuReturn.MenuSceneName != "MainMenu")
                Fail("SceneMenuReturn.MenuSceneName does not match the menu scene name.");

            var buttons = Object.FindObjectsOfType<SceneLoadButton>();
            if (buttons.Length != 5) Fail($"expected 5 menu buttons, found {buttons.Length}");

            string[] required = { "ICARUS_v1", "ICARUS_TwoHole_v1", "New_Sample", "Dom_v4" };
            foreach (string req in required)
            {
                bool found = false;
                foreach (var b in buttons) if (b.sceneName == req) found = true;
                if (!found) Fail($"no menu button for {req}");
            }

            foreach (var b in buttons)
            {
                string path = "Assets/Scenes/" + b.sceneName + ".unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) Fail($"button '{b.sceneName}' points at a scene that doesn't exist");

                bool inBuild = false;
                foreach (var s in scenes) if (s.path == path && s.enabled) inBuild = true;
                if (!inBuild) Fail($"scene '{b.sceneName}' is not enabled in Build Settings");

                var hb = b.GetComponent<HoverButton>();
                if (hb == null || hb.onButtonDown.GetPersistentEventCount() != 0 || hb.onButtonUp.GetPersistentEventCount() != 0)
                    Fail($"button '{b.sceneName}' has a missing HoverButton or dangling persistent listeners");
            }

            if (_failures == 0) Debug.Log("[MenuTest] RESULT: PASS - menu is first in Build Settings; 5 buttons -> existing, enabled scenes; no dangling listeners.");
            else Debug.LogError($"[MenuTest] RESULT: FAIL - {_failures} check(s) failed.");
        }

        private static void Fail(string m)
        {
            _failures++;
            Debug.LogError("[MenuTest] FAIL: " + m);
        }
    }
}
