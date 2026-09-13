# GolfVR — University of Pittsburgh at Bradford Alumni Weekend 2026

A 9-hole VR mini-golf course built in Unity 2022.3.62f3 with SteamVR, for the HTC Vive Pro (Vive Wand controllers). Scene: `Assets/Scenes/MiniGolf_AlumniCourse_v3.unity`.

## Controls (Vive Wand)

| Input | Action |
|---|---|
| Trigger | Grab / interact |
| Grip button | Pick up the putter |
| Trackpad — center | Teleport |
| Trackpad — right edge | Snap putter to you |
| Trackpad — left edge | Reset ball in front of you |

These are also shown in-course on the "HOW TO PLAY" sign near Hole 1.

## Features

- **9-hole course** with per-hole tee signs, par, and named holes ("Long Drive", "Chicane Slalom", "Windmill Gauntlet", ... "The Final Roar" for the Hole 9 finale).
- **Scoreboard** (world-space canvas) tracks strokes/par per hole and follows the player to whichever hole is active, showing a live "HOLE X COMPLETE!" banner with score terms (Hole in One, Eagle, Birdie, Par, Bogey) after each sink.
- **Beginner-friendly reset buttons** (`QuickResetController`) repurpose the unused SteamVR snap-turn actions: one snaps the putter back in front of the player, the other resets the ball in front of the player.
- **Fireworks celebration**: every hole sunk triggers confetti + victory fanfare (as before) plus a new sequence — the sky fades to a night skybox, 9 colorful firework bursts go off over ~7 seconds with light flashes, two procedural horn blasts play, then it fades back to day (`FireworksCelebrationController`).
- **Out-of-bounds handling**: a ball that falls off course gets a penalty stroke and resets to its last rest position.
- **Reset-for-next-group kiosk**: a physical push-button near the entrance plaza (`ResetRoundButton`, built on SteamVR's own proven push-button interaction rather than a hand-rolled one) lets event staff reset the whole round — scores, ball, and putter all snapped back to Hole 1 — between groups without relaunching the app.

## Screenshots

**Course overview**
![Course overview](Screenshots/01_course_overview.png)

**Entrance / welcome sign** (correctly facing the actual spawn point — see Bugs Found below)
![Entrance sign](Screenshots/02_entrance_sign.png)

**Instructions sign + Hole 1**
![Instructions and Hole 1 sign](Screenshots/03_instructions_and_hole1_sign.png)

**Festive entrance plaza**
![Festive entrance plaza](Screenshots/04_festive_entrance_plaza.png)

**Scoreboard + hole-complete banner**
![Scoreboard and banner](Screenshots/05_scoreboard_and_banner.png)

**Night sky + fireworks show**
![Night fireworks show](Screenshots/06_night_fireworks_show.png)

**Reset-for-next-group kiosk**
![Reset round kiosk](Screenshots/07_reset_round_kiosk.png)

The first six were captured headlessly (no live Editor session) via `Tools/GolfVR/Capture Documentation Screenshots` — see Testing Tools below. The seventh (kiosk) was captured the same way with an ad hoc one-off script during development and isn't part of that regenerable set.

## Testing tools

Unity's batch mode (`-executeMethod`) exits as soon as the method returns — it never pumps Play mode, coroutines, or per-frame `Update()`/`Time.deltaTime` logic. So instead of the normal Unity Test Framework (which also can't be referenced from this project's default `Assembly-CSharp`, since it compiles after everything else), all of these tools work entirely in **Edit mode**: they reflection-invoke the same private `Awake()`/`Start()`/coroutine methods Unity would normally call, and manually pump any `WaitForSeconds`-based coroutine to completion in a tight loop. All live in `Assets/Editor/` and are safe to run only when no other Unity Editor instance has the project open.

Run any of them from a live Editor via the `Tools/GolfVR/` menu, or headlessly:

```bash
unity run <project-path> --editor-version 2022.3.62f3 -- -executeMethod <FullMethodName> -logFile <path>
```

| Menu item | Method | What it checks |
|---|---|---|
| Run Reflective Full-Course Playtest | `GolfVR.EditorTools.ReflectivePlaytest.Run` | Plays all 9 holes back-to-back via the real sink/scoring/hole-transition code path; fails loudly if any hole doesn't register as sunk or the round doesn't finish. |
| Extended Feature Test | `GolfVR.EditorTools.ExtendedFeatureTest.Run` | Score-term wording (hole-in-one/eagle/birdie/par/bogey), the out-of-bounds penalty path, the `QuickResetController` debug buttons, and the force-sink guard (below). |
| Capture Scene Screenshot | `GolfVR.EditorTools.SceneCameraCapture.Capture` | Renders one still from an arbitrary camera pose (`-shotPos`, `-shotLookAt`, `-shotFov`, `-shotOut`) — the only way to get visual feedback on scene changes without a live Editor. |
| Capture Prefab Preview | `GolfVR.EditorTools.SceneCameraCapture.CapturePrefabPreview` | Instantiates a prefab (`-prefabPath`) and auto-frames a shot of it, without saving — for checking an asset's real look/scale before committing to using it. |
| Capture Documentation Screenshots | `GolfVR.EditorTools.DocumentationScreenshots.Run` | Regenerates all six screenshots above into `Docs/Screenshots/`. |
| Bug Fix Verification Test | `GolfVR.EditorTools.BugFixVerificationTest.Run` | Regression test for the four bugs reported from real playtesting (below): a real sink no longer teleports the ball, a debug sink still snaps it safely, the reset button has no dangling event listeners and still fires, and the putter's attachment flags include `SnapOnAttach`. |

### Manually testing the sink/celebration flow

`MiniGolfGameManager` has a debug shortcut for testing the sink → scoreboard → confetti → fireworks flow live, in a real Play session, without actually putting the ball in:

- **Press `K`** during Play mode to sink whichever hole is currently active.
- Or right-click the **Mini Golf Game Manager** component header in the Inspector and choose **"DEBUG: Sink Current Hole"**.

Both run the real path — the ball snaps into the cup and the actual `OnHoleSunk` scoring/celebration logic fires — not a shortcut animation. Compiled out of real builds (`#if UNITY_EDITOR`).

Each `GolfHole` also has its own **"DEBUG: Force Sink This Hole"** context-menu item, but it only works on whichever hole `MiniGolfGameManager` currently considers active — forcing a different hole logs a warning and does nothing, rather than silently scoring against the wrong hole and number (see Bugs Found below).

For testing the reset kiosk specifically without walking up to it in a headset, call `MiniGolfGameManager.Instance.ResetForNextGroup()` directly, or trigger `ResetRoundButton`'s private `OnPressed` the same way a hand-hover-press would.

## Bugs found and fixed via this testing

Working entirely headless (no live Editor / VR headset available for direct testing) meant relying on the tools above to catch problems that would otherwise only surface once someone actually played the course. Found and fixed so far:

1. **Scoreboard crash on round start** — `ScoreboardUI`'s hole-row `Text` arrays were serialized empty in the scene, so `EnsureUIComponents()` threw `IndexOutOfRangeException` the instant a round started, before Hole 1 was even playable. Fixed by reallocating the arrays if undersized.
2. **Entrance sign faced away from the spawn point** — its readable face pointed south, but `PlayerTee_Hole1` (where players actually start) is north of it; anyone starting a round would see the blank/dark back of the "Welcome" sign. Fixed by rotating it 180°.
3. **Sign panels went solid black in shadow** — the Standard-shader board materials only showed their true color on the side catching direct sunlight. Made all sign panels emissive at their own albedo color so they're legible from any angle regardless of the sun.
4. **Background ground plane rendered as a broken blue checkerboard** — the "Floor" plane had picked up an alpha-cutout rock material instead of a tileable ground texture. Restored it to match its sibling "floor far" plane.
5. **Scoreboard read backwards from the tee** — same class of bug as #2: `SetupHole()`'s `Quaternion.LookRotation` pointed the board's *unreadable* side at the player standing at the tee. Found via the `05_scoreboard_and_banner.png` screenshot above (first capture showed fully mirrored text); fixed by flipping the look-at direction.
6. **New props inherit the same two pitfalls automatically** — while building the reset kiosk (#3 and #5's root causes, generalized): its label board came out solid black until made emissive like every other sign, and its `LookRotation` (aimed at a ground-level reference point from 1.65m up) pitched the whole board into a tilted lectern angle instead of just yawing it to face the right way. Both fixed the same way as the originals; worth remembering for any future sign/label.
7. **Ball disappeared after a hole was sunk** — a real bug introduced by refactoring `OnTriggerStay`'s inline sink logic into a shared `Sink()` helper for the debug force-sink feature: the refactor accidentally made *every* sink (not just debug-forced ones) teleport the ball to `transform.position` (the hole trigger's own anchor point, which can sit at or below the green's solid collider). A ball teleported into overlapping solid geometry gets violently ejected by the physics engine on the next step — looking exactly like it "disappeared." Fixed by only snapping the ball's position for the debug path, and doing that snap through `GolfBall.SpawnAtTee()` (which raycasts down to the real surface first) instead of a raw position assignment.
8. **Putter grip attach point wrong** — `Throwable.attachmentFlags` on the putter was `44` (`TurnOnKinematic | ParentToHand | DetachFromOtherHand`), missing `SnapOnAttach` and `DetachOthers` from Valve's own tested `Hand.defaultAttachmentFlags`. Realigned to the documented default so the grab snaps deterministically to the `attachmentOffset` (the Grip transform) instead of drifting.
9. **Reset button did nothing when pressed** — the `AddResetRoundButton` build script had deleted the stock `ButtonEffect` component (to replace its hardcoded revert-to-white with `ResetRoundButton`'s real-color restore), but Unity's prefab-instance override recorded the now-dangling `onButtonDown`/`onButtonUp` persistent listeners as `m_Target: {fileID: 0}` instead of actually removing them. A `UnityEvent` invoking a persistent call whose target is null throws mid-`Invoke()`, which aborted the call before it ever reached `ResetRoundButton`'s own dynamically-added listener — so the button never did anything. Confirmed by finding exactly one dangling listener on each event in the saved scene data. `UnityEventTools.RemovePersistentListener` turned out not to be enough to fix this on an already-diverged prefab instance (it re-nulls the slot but doesn't shrink the array, so the dangling call comes right back on the next save); actually clearing it required going through `SerializedObject`/`SerializedProperty` and setting `m_PersistentCalls.m_Calls.arraySize` to 0 directly.
10. **Instructions board too small for its own text** — the "HOW TO PLAY" board was sized for far less text than it actually holds (15 lines once every blank spacer line is counted); most lines floated directly against the sky with no backing at all. Tightened the copy to 8 lines (still wrapped so no single line is too wide) and grew the board to comfortably fit them, resizing the text and frame to preserve their original absolute (world-space) size rather than just inheriting the bigger board's scale.

If you notice anything else that looks wrong in a live headset session that these tools didn't catch, it's worth adding as a case to `ExtendedFeatureTest` or `BugFixVerificationTest` so it stays caught.

## Known limitations

- The fireworks day→night→day fade (`FireworksCelebrationController`) animates using `Time.deltaTime`, which only advances in a real running Play session — Edit-mode batch scripts can't pump it the way they can a `WaitForSeconds`-based coroutine. Its logic (skybox swap, light dimming, burst spawning, horn audio) is verified piece-by-piece and a forced mid-show screenshot confirms the visuals, but its real-time pacing (does 1 second actually feel like 1 second) has not been verified in a live Play session — worth a quick check next time you're in the Editor or headset.
- From an elevated/aerial camera angle you can see a faint checkerboard pattern on the horizon beyond the green grass ring — that's SteamVR's own sample "floor far" material (deliberately a chaperone-style grid, used here as a far-distance ground filler), not a bug; it's barely visible at normal player eye height.
