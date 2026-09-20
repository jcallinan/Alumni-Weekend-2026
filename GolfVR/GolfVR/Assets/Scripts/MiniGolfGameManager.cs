using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace GolfVR
{
    public class MiniGolfGameManager : MonoBehaviour
    {
        public static MiniGolfGameManager Instance { get; private set; }

        [Header("Course Setup (9 Holes)")]
        public GolfHole[] holes = new GolfHole[9];

        [Header("Equipment & Player")]
        public GolfBall golfBall;
        public GolfPutter golfPutter;
        public Transform playerTransform;

        [Header("UI Scoreboard")]
        public ScoreboardUI scoreboard;

        [Header("Transitions & Delays")]
        [Tooltip("Delay in seconds after sinking ball before transitioning to next hole")]
        public float holeTransitionDelay = 3.5f;

        [Header("Audio")]
        public AudioSource globalAudioSource;

        // Game State
        private int _currentHoleIndex = 0;
        private int[] _strokesPerHole;
        private bool _isTransitioning = false;
        private bool _isGameFinished = false;

        public int CurrentHoleIndex => _currentHoleIndex;
        public int CurrentHoleNumber => _currentHoleIndex + 1;
        public int CurrentHoleStrokes => (_strokesPerHole != null && _currentHoleIndex < _strokesPerHole.Length) ? _strokesPerHole[_currentHoleIndex] : 0;
        public bool IsGameFinished => _isGameFinished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (globalAudioSource == null)
            {
                globalAudioSource = GetComponent<AudioSource>();
            }

            int count = holes != null && holes.Length > 0 ? holes.Length : 9;
            _strokesPerHole = new int[count];
        }

        private void Start()
        {
            InitializeRound();
        }

        /// <summary>
        /// Resets scoring and every hole and sends the player back to Hole 1.
        /// The putter's position is left alone by default -- on first app
        /// launch it should stay wherever it's placed in the scene (e.g. a
        /// starting display table) until a player has picked it up -- but
        /// pass true (as ResetForNextGroup does) to also snap it back into
        /// place, e.g. when staff are resetting the course for a new group
        /// who'll expect to find it back on its stand.
        /// </summary>
        public void InitializeRound(bool repositionPutter = false)
        {
            _currentHoleIndex = 0;
            _isGameFinished = false;
            _isTransitioning = false;

            int count = holes != null && holes.Length > 0 ? holes.Length : 9;
            _strokesPerHole = new int[count];

            for (int i = 0; i < count; i++)
            {
                _strokesPerHole[i] = 0;
                if (holes != null && i < holes.Length && holes[i] != null)
                {
                    holes[i].ResetHole();
                }
            }

            // A putter that sticks to the hand has to be let go before it can be
            // put back on its stand for the next group.
            if (repositionPutter && golfPutter != null)
            {
                golfPutter.ReleaseFromHand();
            }

            SetupHole(_currentHoleIndex, repositionPutter);

            if (scoreboard != null)
            {
                scoreboard.UpdateScoreboard(holes, _strokesPerHole, _currentHoleIndex);
                scoreboard.ShowBanner("WELCOME TO HOLE 1! Ready to putt!");
            }
        }

        /// <summary>
        /// For event staff: resets the whole round for the next group of
        /// players, including snapping the putter and ball back to Hole 1's
        /// tee so the course looks freshly set up rather than however the
        /// previous group left it.
        /// </summary>
        public void ResetForNextGroup()
        {
            InitializeRound(repositionPutter: true);
        }

        private void SetupHole(int index, bool repositionPutter = true)
        {
            if (index < 0 || index >= holes.Length || holes[index] == null) return;

            GolfHole currentHole = holes[index];
            currentHole.ResetHole();

            // 1. Move ball to tee
            if (golfBall != null && currentHole.teePoint != null)
            {
                golfBall.SpawnAtTee(currentHole.teePoint.position);
            }

            // 2. Move player near the tee
            if (currentHole.playerTeeLocation != null)
            {
                TeleportPlayer(currentHole.playerTeeLocation.position, currentHole.playerTeeLocation.rotation);
            }

            // 3. Place putter near player/ball if not currently held
            if (repositionPutter && golfPutter != null && !golfPutter.IsHeld && currentHole.teePoint != null)
            {
                // Tee markers are authored at ground level, but the actual
                // fairway surface can sit well above that on raised or
                // sloped course pieces -- raycast down to find it instead
                // of assuming a fixed height.
                Vector3 sideSpot = currentHole.teePoint.position + Vector3.right * 0.8f;
                Vector3 castOrigin = sideSpot + Vector3.up * 3f;
                float surfaceY = Physics.Raycast(castOrigin, Vector3.down, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore)
                    ? hit.point.y
                    : currentHole.teePoint.position.y;

                Vector3 putterPos = new Vector3(sideSpot.x, surfaceY + 0.5f, sideSpot.z);
                golfPutter.ResetToPosition(putterPos, Quaternion.identity);
            }

            // 4. Bring the scoreboard along to this hole's tee instead of leaving
            // it stranded at a single fixed spot the player would have to walk
            // back to after being teleported around the course.
            if (scoreboard != null && currentHole.playerTeeLocation != null)
            {
                Transform teeLoc = currentHole.playerTeeLocation;
                Vector3 boardPos = teeLoc.position + teeLoc.right * 1.8f + Vector3.up * 1.0f;
                scoreboard.transform.position = boardPos;
                // LookRotation points the object's local +Z at the target,
                // but a Canvas reads correctly from its local -Z side (same
                // convention as this project's TextMesh signs) -- so the
                // forward vector needs to point AWAY from the tee, not at
                // it, or a player standing at the tee sees the scoreboard
                // mirrored/backwards (confirmed via a headless screenshot
                // taken from the tee's own position).
                scoreboard.transform.rotation = Quaternion.LookRotation(boardPos - teeLoc.position, Vector3.up);
            }

            if (scoreboard != null)
            {
                scoreboard.UpdateScoreboard(holes, _strokesPerHole, _currentHoleIndex);
            }
        }

        public void RecordStroke()
        {
            if (_isTransitioning || _isGameFinished) return;

            _strokesPerHole[_currentHoleIndex]++;
            Debug.Log($"[GolfVR] Stroke recorded! Hole {_currentHoleIndex + 1}: {_strokesPerHole[_currentHoleIndex]} strokes.");

            if (scoreboard != null)
            {
                scoreboard.UpdateScoreboard(holes, _strokesPerHole, _currentHoleIndex);
            }
        }

        public void RecordPenaltyStroke(string reason)
        {
            if (_isTransitioning || _isGameFinished) return;

            _strokesPerHole[_currentHoleIndex]++;

            if (scoreboard != null)
            {
                scoreboard.UpdateScoreboard(holes, _strokesPerHole, _currentHoleIndex);
                scoreboard.ShowBanner(reason, 2.5f);
            }
        }

        public void OnBallStopped(Vector3 position)
        {
            // Ball came to rest
        }

        private float _transitionStartTime;

        public void OnHoleSunk(GolfHole hole)
        {
            if (_isGameFinished)
            {
                Debug.Log($"[GolfVR] Hole {hole.holeNumber} sunk but the round is already finished -- ignoring. Press the reset kiosk to start a new round.");
                return;
            }

            if (_isTransitioning)
            {
                // Watchdog: a hole transition that never finished (something
                // threw mid-routine, or the manager object was disabled and
                // re-enabled) would otherwise leave the round wedged forever.
                if (Time.time - _transitionStartTime < holeTransitionDelay + 5f)
                {
                    Debug.Log($"[GolfVR] Hole {hole.holeNumber} sunk while a transition is already running -- ignoring.");
                    return;
                }
                Debug.LogWarning("[GolfVR] Previous hole transition never finished -- recovering.");
            }

            _transitionStartTime = Time.time;
            StartCoroutine(HandleHoleSunkRoutine(hole));
        }

        private IEnumerator HandleHoleSunkRoutine(GolfHole hole)
        {
            _isTransitioning = true;

            int strokes = _strokesPerHole[_currentHoleIndex];
            int par = hole.par;
            int diff = strokes - par;

            string scoreTerm;
            if (strokes == 1) scoreTerm = "HOLE IN ONE! 🌟";
            else if (diff <= -2) scoreTerm = "EAGLE! 🦅";
            else if (diff == -1) scoreTerm = "BIRDIE! 🐦";
            else if (diff == 0) scoreTerm = "PAR! ⛳";
            else if (diff == 1) scoreTerm = "BOGEY 🏌️";
            else scoreTerm = $"+{diff} BOGEY 🏌️";

            string msg = $"HOLE {hole.holeNumber} COMPLETE!\n{scoreTerm} ({strokes} Strokes)";
            Debug.Log($"[GolfVR] {msg} -- moving to the next hole in {holeTransitionDelay:F1}s.");

            // Scoreboard/banner work is presentation only: guard it so a UI
            // problem can never strand the round on the hole that was just
            // completed.
            try
            {
                if (scoreboard != null)
                {
                    scoreboard.UpdateScoreboard(holes, _strokesPerHole, _currentHoleIndex);
                    scoreboard.ShowBanner(msg, holeTransitionDelay);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            yield return new WaitForSeconds(holeTransitionDelay);

            // Advance to next hole or complete round
            if (_currentHoleIndex < holes.Length - 1)
            {
                _currentHoleIndex++;
                _isTransitioning = false;
                Debug.Log($"[GolfVR] Advancing to hole {_currentHoleIndex + 1}.");
                SetupHole(_currentHoleIndex);
                try
                {
                    if (scoreboard != null)
                    {
                        scoreboard.ShowBanner($"NOW PLAYING HOLE {_currentHoleIndex + 1} (Par {holes[_currentHoleIndex].par})", 3.0f);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
            else
            {
                // Round Complete!
                _isGameFinished = true;
                _isTransitioning = false;
                Debug.Log("[GolfVR] Round complete.");

                int totalStrokes = 0;
                int totalPar = 0;
                for (int i = 0; i < holes.Length; i++)
                {
                    if (i < _strokesPerHole.Length) totalStrokes += _strokesPerHole[i];
                    if (holes[i] != null) totalPar += holes[i].par;
                }

                int totalDiff = totalStrokes - totalPar;
                string totalDiffStr = totalDiff == 0 ? "Even Par" : (totalDiff > 0 ? $"+{totalDiff}" : $"{totalDiff}");
                string finalMsg = $"🏆 9-HOLE CHAMPIONSHIP COMPLETE! 🏆\nTotal: {totalStrokes} Strokes ({totalDiffStr})\nThank you for visiting the UPB VR Lab!";

                try
                {
                    if (scoreboard != null)
                    {
                        scoreboard.UpdateScoreboard(holes, _strokesPerHole, holes.Length - 1);
                        scoreboard.ShowBanner(finalMsg, 12.0f);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private void TeleportPlayer(Vector3 targetPos, Quaternion targetRot)
        {
            if (Player.instance != null)
            {
                Transform playerRoot = Player.instance.transform;
                playerRoot.position = targetPos;
                playerRoot.rotation = targetRot;
            }
            else if (playerTransform != null)
            {
                playerTransform.position = targetPos;
                playerTransform.rotation = targetRot;
            }
        }

        public void ResetCurrentBallToTee()
        {
            if (golfBall != null && _currentHoleIndex < holes.Length && holes[_currentHoleIndex] != null)
            {
                golfBall.SpawnAtTee(holes[_currentHoleIndex].teePoint.position);
            }
        }

        /// <summary>
        /// Testing convenience: jumps straight to any hole -- resetting
        /// every hole's completed flag and the target hole's stroke count,
        /// then teleporting the player and snapping the ball/putter to its
        /// tee -- without needing to play through every prior hole first.
        /// For the physical "pick a hole" panel used during in-headset
        /// testing, so a specific hole's fairway/geometry can be checked
        /// directly. Also doubles as a per-hole "put the ball and putter
        /// back" button: pressing the button for whichever hole is already
        /// active resets just that hole, in place.
        /// </summary>
        public void JumpToHole(int index)
        {
            if (holes == null || index < 0 || index >= holes.Length || holes[index] == null)
            {
                Debug.LogWarning($"[GolfVR] JumpToHole: index {index} is out of range.");
                return;
            }

            _isGameFinished = false;
            _isTransitioning = false;
            _currentHoleIndex = index;

            int count = holes.Length;
            if (_strokesPerHole == null || _strokesPerHole.Length != count)
            {
                _strokesPerHole = new int[count];
            }
            for (int i = 0; i < count; i++)
            {
                if (holes[i] != null) holes[i].ResetHole();
            }
            _strokesPerHole[index] = 0;

            SetupHole(index, repositionPutter: true);

            if (scoreboard != null)
            {
                scoreboard.UpdateScoreboard(holes, _strokesPerHole, index);
                scoreboard.ShowBanner($"JUMPED TO HOLE {index + 1}", 2.5f);
            }

            Debug.Log($"[GolfVR] Jumped to hole {index + 1}.");
        }

#if UNITY_EDITOR
        [Tooltip("Editor-only: press this key in Play mode to sink the current hole for quick testing")]
        public KeyCode debugSinkKey = KeyCode.K;

        private void Update()
        {
            if (Input.GetKeyDown(debugSinkKey))
            {
                DebugSinkCurrentHole();
            }
        }
#endif

        /// <summary>
        /// Testing convenience: sinks whichever hole is currently being
        /// played -- ball snaps into the cup and the real celebration/scoring
        /// path runs (confetti, fanfare, fireworks, advance to next hole) --
        /// without needing to actually putt it in. Right-click the "Mini Golf
        /// Game Manager" component header in the Inspector during Play mode
        /// and choose "DEBUG: Sink Current Hole", or press K.
        /// </summary>
        [ContextMenu("DEBUG: Sink Current Hole")]
        public void DebugSinkCurrentHole()
        {
            if (holes == null || _currentHoleIndex < 0 || _currentHoleIndex >= holes.Length || holes[_currentHoleIndex] == null)
            {
                Debug.LogWarning("[GolfVR] DebugSinkCurrentHole: no valid current hole to sink.");
                return;
            }

            holes[_currentHoleIndex].DebugForceSink();
        }
    }
}
