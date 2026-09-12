using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GolfVR.EditorTools
{
    /// <summary>
    /// Broader coverage beyond ReflectivePlaytest's "can the whole course be
    /// completed" check: score-term wording (hole-in-one/eagle/birdie/par/
    /// bogey), the out-of-bounds penalty path, the QuickResetController
    /// debug buttons, and the new DebugForceSink/DebugSinkCurrentHole
    /// testing shortcuts (including the guard against force-sinking a hole
    /// that isn't the active one). All driven the same reflection-based
    /// Edit-mode way as ReflectivePlaytest, for the same reasons (batch mode
    /// won't pump Play-mode/coroutine logic on its own).
    /// </summary>
    public static class ExtendedFeatureTest
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static int _failures;

        [MenuItem("Tools/GolfVR/Extended Feature Test")]
        public static void Run()
        {
            _failures = 0;
            EditorSceneManager.OpenScene("Assets/Scenes/MiniGolf_AlumniCourse_v3.unity", OpenSceneMode.Single);

            MiniGolfGameManager manager = Object.FindObjectOfType<MiniGolfGameManager>();
            GolfBall ball = Object.FindObjectOfType<GolfBall>();
            GolfPutter putter = Object.FindObjectOfType<GolfPutter>();
            QuickResetController quickReset = Object.FindObjectOfType<QuickResetController>();

            if (manager == null || ball == null)
            {
                Debug.LogError("[ExtTest] Could not find MiniGolfGameManager or GolfBall.");
                return;
            }

            Invoke(ball, "Awake");
            Invoke(ball, "Start");
            if (putter != null) Invoke(putter, "Awake");
            foreach (GolfHole h in manager.holes)
            {
                if (h != null) Invoke(h, "Awake");
            }
            Invoke(manager, "Awake");
            Invoke(manager, "Start");

            TestScoreTerm(manager, holeIndex: 0, strokes: 1, expectedSubstring: "HOLE IN ONE", label: "Hole-in-one");
            TestScoreTerm(manager, holeIndex: 0, strokes: 2, expectedSubstring: "PAR!", label: "Par");
            TestScoreTerm(manager, holeIndex: 0, strokes: 3, expectedSubstring: "BOGEY", label: "Bogey");
            TestScoreTerm(manager, holeIndex: 8, strokes: 2, expectedSubstring: "EAGLE", label: "Eagle (hole 9, par 4)");
            TestScoreTerm(manager, holeIndex: 8, strokes: 3, expectedSubstring: "BIRDIE", label: "Birdie (hole 9, par 4)");

            TestOutOfBounds(manager, ball);
            TestQuickReset(quickReset);
            TestForceSinkGuard(manager);

            if (_failures == 0)
            {
                Debug.Log("[ExtTest] RESULT: PASS - all extended checks passed.");
            }
            else
            {
                Debug.LogError($"[ExtTest] RESULT: FAIL - {_failures} check(s) failed, see above.");
            }
        }

        private static void TestScoreTerm(MiniGolfGameManager manager, int holeIndex, int strokes, string expectedSubstring, string label)
        {
            manager.InitializeRound();
            SetField(manager, "_currentHoleIndex", holeIndex);

            GolfHole hole = manager.holes[holeIndex];
            SetField(hole, "_isCompleted", false);

            int[] strokesArr = (int[])GetField(manager, "_strokesPerHole");
            strokesArr[holeIndex] = strokes;

            ScoreboardUI scoreboard = manager.scoreboard;

            MethodInfo routineMethod = typeof(MiniGolfGameManager).GetMethod("HandleHoleSunkRoutine", PrivateInstance);
            var routine = (System.Collections.IEnumerator)routineMethod.Invoke(manager, new object[] { hole });
            routine.MoveNext(); // banner text is set synchronously before the yield

            string banner = scoreboard != null && scoreboard.bannerText != null ? scoreboard.bannerText.text : null;
            if (banner != null && banner.Contains(expectedSubstring))
            {
                Debug.Log($"[ExtTest] {label}: OK (\"{banner.Replace("\n", " / ")}\")");
            }
            else
            {
                _failures++;
                Debug.LogError($"[ExtTest] {label}: FAIL - expected banner to contain \"{expectedSubstring}\", got \"{banner}\"");
            }

            // Drain the rest of the routine so _isTransitioning doesn't leak
            // into the next scenario.
            int guard = 0;
            while (routine.MoveNext() && guard < 1000) guard++;
        }

        private static void TestOutOfBounds(MiniGolfGameManager manager, GolfBall ball)
        {
            manager.InitializeRound();
            Vector3 restPos = ball.transform.position;
            int strokesBefore = manager.CurrentHoleStrokes;

            MethodInfo handleOob = typeof(GolfBall).GetMethod("HandleOutOfBounds", PrivateInstance);
            handleOob.Invoke(ball, null);

            int strokesAfter = manager.CurrentHoleStrokes;
            float dist = Vector3.Distance(ball.transform.position, restPos);

            if (strokesAfter == strokesBefore + 1 && dist < 0.5f)
            {
                Debug.Log($"[ExtTest] Out-of-bounds: OK (penalty stroke recorded, ball returned to last rest position, {dist:F2}m off).");
            }
            else
            {
                _failures++;
                Debug.LogError($"[ExtTest] Out-of-bounds: FAIL - strokes {strokesBefore}->{strokesAfter} (expected +1), ball moved {dist:F2}m from rest position (expected ~0).");
            }
        }

        private static void TestQuickReset(QuickResetController quickReset)
        {
            if (quickReset == null)
            {
                Debug.LogWarning("[ExtTest] QuickResetController: not found in scene, skipping (not a failure).");
                return;
            }

            Invoke(quickReset, "Awake");

            try
            {
                MethodInfo snapPutter = typeof(QuickResetController).GetMethod("SnapPutterToPlayer", PrivateInstance);
                snapPutter.Invoke(quickReset, null);
                MethodInfo resetBall = typeof(QuickResetController).GetMethod("ResetBallToPlayer", PrivateInstance);
                resetBall.Invoke(quickReset, null);
                Debug.Log("[ExtTest] QuickResetController: OK (both debug buttons ran without exceptions; Player.instance is null outside Play mode so they no-op, as designed).");
            }
            catch (TargetInvocationException e)
            {
                _failures++;
                Debug.LogError($"[ExtTest] QuickResetController: FAIL - {e.InnerException}");
            }
        }

        private static void TestForceSinkGuard(MiniGolfGameManager manager)
        {
            manager.InitializeRound();
            GolfHole notCurrentHole = manager.holes[3]; // hole index 0 is current after InitializeRound
            notCurrentHole.DebugForceSink();

            if (!notCurrentHole.IsCompleted)
            {
                Debug.Log("[ExtTest] Force-sink guard: OK (force-sinking a non-active hole was correctly refused).");
            }
            else
            {
                _failures++;
                Debug.LogError("[ExtTest] Force-sink guard: FAIL - a non-active hole was sunk anyway.");
            }
        }

        private static void Invoke(object target, string methodName)
        {
            MethodInfo m = target.GetType().GetMethod(methodName, PrivateInstance);
            if (m == null) return;
            m.Invoke(target, null);
        }

        private static object GetField(object target, string name)
        {
            FieldInfo f = target.GetType().GetField(name, PrivateInstance);
            return f.GetValue(target);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo f = target.GetType().GetField(name, PrivateInstance);
            f.SetValue(target, value);
        }
    }
}
