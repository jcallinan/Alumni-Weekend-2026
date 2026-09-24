# UPB Alumni Weekend 2026 - VR Lab Showcases

**University of Pittsburgh at Bradford - Marilyn Horne Hall VR Lab**

This repository holds two interactive Virtual / Mixed Reality showcases built for UPB Alumni & Family Weekend 2026:

| Project | What it is | Runs on |
| :--- | :--- | :--- |
| [`panther-builder/`](panther-builder/) | **Panther Customizer** - a WebXR / 3D web app that puts the campus Panther statue in your room and lets you change its finish and size | Any WebXR headset browser (Meta Quest Browser), or a desktop browser |
| [`GolfVR/GolfVR/`](GolfVR/GolfVR/) | **VR Mini Golf + Driving Range** - a Unity / SteamVR experience with a 9-hole course, a 2-hole variant, a driving range and a main menu | Windows PC + SteamVR, tested with an **HTC Vive Pro** and Vive wand controllers |

---

## Project 1: Panther Customizer (`panther-builder/`)

An interactive Mixed Reality and 3D web experience built around a photogrammetry scan of the Pitt Bradford Panther statue. Details and screenshots: [`panther-builder/README.md`](panther-builder/README.md).

**Try it now (headset ready):** <https://jcallinan.github.io/Alumni-Weekend-2026/>

### Using it in a headset
1. Open the browser in the headset (e.g. **Meta Quest Browser**).
2. Go to `https://jcallinan.github.io/Alumni-Weekend-2026/`.
3. Click **Enter AR** / **View in Mixed Reality** and allow the permission prompts.
4. Look at the floor or a table and pull the controller trigger to place the Panther.
5. Use the floating panel to switch finishes (Campus Scan, Pitt Royal Blue, Pitt Gold, Cast Bronze, White Marble, Bradford Onyx) and to resize it from a 20 cm desk model up to the 2.7 m life-size statue (or 4 m "monumental").

On a desktop / laptop / tablet it opens in a normal 3D viewer instead - drag to orbit, scroll to zoom.

### Running it locally
Requires [Node.js](https://nodejs.org/) 18+.
```bash
cd panther-builder
npm install
npm run serve      # https dev server at https://localhost:8081 (also reachable from a headset on the same Wi-Fi via your PC's IP)
npm test           # unit tests (node --test)
npm run build      # production bundle into dist/
```
The dev server uses a self-signed certificate; accept the browser warning once (WebXR requires HTTPS).

### Publishing
- **Automatic:** every push to `main` runs `.github/workflows/deploy-pages.yml`, which tests, builds and deploys to GitHub Pages (Repo **Settings -> Pages -> Source: GitHub Actions**).
- **Manual:** `npm run deploy` builds `dist/` and pushes it to the `gh-pages` branch (then set Pages to *Deploy from a branch -> gh-pages*).

---

## Project 2: GolfVR (`GolfVR/GolfVR/`)

A VR mini-golf and golf-practice experience in **Unity 2022.3.62f3** with **SteamVR**, built for the **HTC Vive Pro** (Vive wand controllers). Full technical notes, testing tools and the list of bugs found/fixed are in [`GolfVR/GolfVR/Docs/README.md`](GolfVR/GolfVR/Docs/README.md).

### What's in it
Everything is launched from a **main menu** (`MainMenu.unity`, first scene in Build Settings). It is a plain flat screen with buttons you **click with the mouse** (no VR needed for the menu); pick:

| Menu button | Scene | What it is |
| :--- | :--- | :--- |
| ICARUS 9 Hole Course | `ICARUS_v1` | The full 9-hole course. **Every hole has its own ball** waiting at its tee; scores per hole, fireworks + horns on every hole sunk, scoreboard that follows you, NEXT HOLE buttons at every tee, a staff reset kiosk and a "pick a hole" test panel |
| ICARUS 2 Hole Course | `ICARUS_TwoHole_v1` | Holes 1 and 2 only, with an invisible fence and invisible walls around the fairways |
| New Sample | `New_Sample` | Sample / experimental scene |
| Dom v4 | `Dom_v4` | Dom's scene |
| Driving range | `ICARUS_DrivingRange_v1` | Hit a driver off a tee: 3D ball flight with a coloured trail, carry + total distance in yards/metres, a history board, and a fresh ball after every shot |

### Controls (Vive wand)
| Input | Action |
| :--- | :--- |
| **Grip** | Pick up the putter / driver. It then **stays stuck to your hand** (no need to keep squeezing) |
| **Trigger** | Push buttons / interact |
| **Trackpad - center** | Teleport |
| **Trackpad - right edge** | While holding the club: cycle the club angle (remembered). Otherwise: snap the putter to you |
| **Trackpad - left edge** | Reset the ball in front of you |
| **MENU button (three lines above the trackpad) - press and hold ~0.7 s** | **Return to the main menu from any scene** |
| Hold **both grips** for 2.5 s | Backup way back to the menu |
| `Esc` (in the Unity Editor) | Back to the menu |

Push-buttons are pressed by poking them with your hand. On the course, the **red reset kiosk** near the start sends all balls back to their tees, clears the scores and puts the putter back on its table; the **hole panel** next to it jumps to any hole (and resets that hole's ball and putter); the small **NEXT HOLE post at each tee** moves you to the next tee without resetting anything.

### Setup on the VR PC
- Windows PC with **SteamVR** installed and the **Vive Pro** working (SteamVR status green).
- **Unity Hub** with Editor **2022.3.62f3**.

### Running it from the Unity Editor
1. Unity Hub -> **Add project from disk** -> select the `GolfVR/GolfVR` folder -> open with 2022.3.62f3.
2. Open **`Assets/Scenes/MainMenu.unity`** (all scenes are already listed in *File -> Build Settings*).
3. Start SteamVR, put the headset on, and press **Play**.
4. Click a menu button with the mouse to pick an experience (the menu shows on the monitor, not in the headset). To come back, press and hold the MENU button on a controller, or press Esc.

To try a single scene directly, open it (e.g. `ICARUS_v1`) and press Play - the hold-MENU return still works, but you need the menu scene listed in Build Settings (it is).

### Building a standalone Windows player
1. **File -> Build Settings**: confirm `MainMenu` is index 0 and the other scenes are checked.
2. Platform **Windows, Mac, Linux**, architecture **x86_64** -> **Build** into e.g. `GolfVR/Builds/AlumniMiniGolf/`.
3. Start SteamVR, then run the built `.exe`.

### Automated tests (no headset needed)
The project ships editor tests that run in Unity batch mode and are also available in the Editor under **Tools -> GolfVR** (real-physics putt and drive tests, grip geometry, hole/menu checks, and more). To run one headlessly:
```bash
unity run GolfVR/GolfVR --editor-version 2022.3.62f3 -- -executeMethod GolfVR.EditorTools.PhysicsPuttTest.Run -logFile out.log
```
The list of tests and what each checks is in [`GolfVR/GolfVR/Docs/README.md`](GolfVR/GolfVR/Docs/README.md). (`test_golf_logic.py` in the repo root is an older stand-alone simulation of the original scoring rules and is no longer maintained.)

### Troubleshooting
- **SteamVR shows "Headset not detected"** - check the Vive Pro link box/cables, restart SteamVR, then press Play again.
- **Can't pick up the putter** - reach for the shaft and squeeze the **grip**. If the club feels tilted wrong, press the **trackpad right edge** while holding it to cycle the angle.
- **The MENU button does nothing** - SteamVR may be using an old saved binding for this app. Open SteamVR's controller-binding screen for the app and reset to the default binding.
- **A menu button says the scene isn't in Build Settings** - add the scene under *File -> Build Settings*.
- **Ball doesn't go in the hole** - it must actually drop into the cup; a slow ball near the cup is gently pulled in.
