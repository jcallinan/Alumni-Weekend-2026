using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GolfVR.EditorTools
{
    /// <summary>
    /// Renders a one-off still from an arbitrary camera pose in the scene and
    /// saves it as a PNG, entirely in batch mode (no live Editor / Game view
    /// needed). This is the only way to get visual feedback on scene changes
    /// when working purely through the headless CLI.
    ///
    /// Camera pose and output path are passed as extra command-line args
    /// (Unity ignores args it doesn't recognize, but they're still visible
    /// via Environment.GetCommandLineArgs()):
    ///   -shotPos x,y,z        camera position (default 0,2,6)
    ///   -shotLookAt x,y,z     point the camera aims at (default 0,1,0)
    ///   -shotFov f            vertical field of view in degrees (default 50)
    ///   -shotOut path         output PNG path, absolute or project-relative
    ///   -shotWidth / -shotHeight  resolution (default 1280x720)
    /// </summary>
    public static class SceneCameraCapture
    {
        [MenuItem("Tools/GolfVR/Capture Scene Screenshot")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MiniGolf_AlumniCourse_v3.unity", OpenSceneMode.Single);

            Vector3 pos = GetVector3Arg("-shotPos", new Vector3(0f, 2f, 6f));
            Vector3 lookAt = GetVector3Arg("-shotLookAt", new Vector3(0f, 1f, 0f));
            float fov = GetFloatArg("-shotFov", 50f);
            int width = GetIntArg("-shotWidth", 1280);
            int height = GetIntArg("-shotHeight", 720);
            string outPath = GetStringArg("-shotOut", "shot.png");

            if (!Path.IsPathRooted(outPath))
            {
                outPath = Path.Combine(Directory.GetCurrentDirectory(), outPath);
            }

            GameObject camGo = new GameObject("__TempCaptureCamera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation((lookAt - pos).normalized, Vector3.up);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 500f;
            cam.clearFlags = CameraClearFlags.Skybox;

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

            RenderTexture prevActive = RenderTexture.active;
            try
            {
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                byte[] png = tex.EncodeToPNG();
                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                File.WriteAllBytes(outPath, png);
                Debug.Log($"[Capture] Saved screenshot to {outPath} ({width}x{height}) from pos={pos} lookAt={lookAt} fov={fov}");
            }
            finally
            {
                RenderTexture.active = prevActive;
                cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(tex);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(camGo);
            }
        }

        private static string GetStringArg(string name, string def)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return def;
        }

        private static float GetFloatArg(string name, float def)
        {
            string s = GetStringArg(name, null);
            return s != null && float.TryParse(s, out float v) ? v : def;
        }

        private static int GetIntArg(string name, int def)
        {
            string s = GetStringArg(name, null);
            return s != null && int.TryParse(s, out int v) ? v : def;
        }

        private static Vector3 GetVector3Arg(string name, Vector3 def)
        {
            string s = GetStringArg(name, null);
            if (s == null) return def;
            string[] parts = s.Split(',');
            if (parts.Length != 3) return def;
            if (float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y) && float.TryParse(parts[2], out float z))
            {
                return new Vector3(x, y, z);
            }
            return def;
        }
    }
}
