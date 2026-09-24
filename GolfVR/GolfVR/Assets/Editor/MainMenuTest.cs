using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GolfVR.EditorTools
{
    /// <summary>
    /// Checks the flat, mouse-driven main menu: it is the first scene in Build
    /// Settings, has an EventSystem and a Camera (no VR rig needed), every scene
    /// button targets a scene that exists and is enabled in Build Settings, and
    /// the return-to-menu component's scene name matches the menu scene.
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

            if (Object.FindObjectOfType<EventSystem>() == null) Fail("no EventSystem: mouse clicks would do nothing");
            if (Object.FindObjectOfType<Camera>() == null) Fail("no Camera in the menu scene");
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null || canvas.GetComponent<GraphicRaycaster>() == null) Fail("no Canvas with a GraphicRaycaster");

            int sceneButtons = 0;
            string[] required = { "ICARUS_v1", "ICARUS_TwoHole_v1", "New_Sample", "Dom_v4" };
            var buttons = Object.FindObjectsOfType<MenuButton>();
            foreach (string req in required)
            {
                bool found = false;
                foreach (var b in buttons) if (b.sceneName == req) found = true;
                if (!found) Fail($"no menu button for {req}");
            }

            foreach (var b in buttons)
            {
                var ui = b.GetComponent<Button>();
                if (ui == null || ui.onClick.GetPersistentEventCount() != 0) Fail($"button '{b.name}' has no Button or has stray persistent listeners");
                if (b.quitApplication) continue;

                sceneButtons++;
                string path = "Assets/Scenes/" + b.sceneName + ".unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) Fail($"button '{b.sceneName}' points at a scene that doesn't exist");

                bool inBuild = false;
                foreach (var s in scenes) if (s.path == path && s.enabled) inBuild = true;
                if (!inBuild) Fail($"scene '{b.sceneName}' is not enabled in Build Settings");
            }
            if (sceneButtons != 5) Fail($"expected 5 scene buttons, found {sceneButtons}");

            if (_failures == 0) Debug.Log("[MenuTest] RESULT: PASS - flat mouse menu (Camera + Canvas + EventSystem) is first in Build Settings; 5 buttons -> existing, enabled scenes.");
            else Debug.LogError($"[MenuTest] RESULT: FAIL - {_failures} check(s) failed.");
        }

        private static void Fail(string m)
        {
            _failures++;
            Debug.LogError("[MenuTest] FAIL: " + m);
        }
    }
}
