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

        public void InitializeRound()
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

            // Skip the putter's auto-reposition on the very first hole: it
            // should stay wherever it's placed in the scene (e.g. a starting
            // display table) until the player has picked it up at least once.
            SetupHole(_currentHoleIndex, repositionPutter: false);

            if (scoreboard != null)
            {
                scoreboard.UpdateScoreboard(holes, _strokesPerHole, _currentHoleIndex);
                scoreboard.ShowBanner("WELCOME TO HOLE 1! Ready to putt!");
            }
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
                float surfaceY = Physics.Raycast(castOrigin, Vector3.down, out RaycastHit hit, 10f)
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
                scoreboard.transform.rotation = Quaternion.LookRotation(teeLoc.position - boardPos, Vector3.up);
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

        public void OnHoleSunk(GolfHole hole)
        {
            if (_isTransitioning || _isGameFinished) return;
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
            Debug.Log($"[GolfVR] {msg}");

            if (scoreboard != null)
            {
                scoreboard.UpdateScoreboard(holes, _strokesPerHole, _currentHoleIndex);
                scoreboard.ShowBanner(msg, holeTransitionDelay);
            }

            yield return new WaitForSeconds(holeTransitionDelay);

            // Advance to next hole or complete round
            if (_currentHoleIndex < holes.Length - 1)
            {
                _currentHoleIndex++;
                _isTransitioning = false;
                SetupHole(_currentHoleIndex);
                if (scoreboard != null)
                {
                    scoreboard.ShowBanner($"NOW PLAYING HOLE {_currentHoleIndex + 1} (Par {holes[_currentHoleIndex].par})", 3.0f);
                }
            }
            else
            {
                // Round Complete!
                _isGameFinished = true;
                _isTransitioning = false;

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

                if (scoreboard != null)
                {
                    scoreboard.UpdateScoreboard(holes, _strokesPerHole, holes.Length - 1);
                    scoreboard.ShowBanner(finalMsg, 12.0f);
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
    }
}
