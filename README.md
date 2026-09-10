# UPB Alumni Weekend 2026 - VR Lab Showcases
**University of Pittsburgh at Bradford**  
**Marilyn Horne Hall VR Lab**

This repository contains two interactive Virtual & Mixed Reality showcase projects developed for UPB Alumni & Family Weekend:

1. **`panther-builder/`**: WebXR Mixed Reality / AR Pitt Bradford Panther 3D Customizer.
2. *`GolfVR/`**: 9-Hole Championship Mini Golf VR Experience in Unity with SteamVR, featuring the authentic campus Panther statue monument.

---

## Project 1: WebXR Panther Customizer (`panther-builder/`)

A WebXR Mixed Reality application ported from Meta's WebXR Showcase framework. It showcases the authentic photogrammetry 3D scan of the campus Pitt Bradford Panther statue with custom finishes and dynamic scaling.

### Prerequisites
- [Node.js](https://nodejs.org/) (v18+ or v20+ recommended)
- A modern WebXR-capable browser:
  - Tested in VR Lab: Meta Quest Browser on Meta Quest 2, 3, or Pro.
  - On Desktop: Chrome, Edge, or Firefox (includes mouse OrbitControls fallback).

### Running in Development
```bash
cd panther-builder
npm install
npm start
```
- Development server starts at `https://localhost:8081` (or your local IP).
- Open the URL inside the **Meta Quest Browser** on the same Wi-Fi network.
- Click **"Enter AR"** to place the Panther on the floor or tables in the VR Lab with full color passthrough.

### Live GitHub Pages URL (Headset Ready)
The WebXR Panther Customizer is deployed directly to GitHub Pages with HTTPS:
> **Headset URL**: **`https://jcallinan.github.io/Alumni-Weekend-2026/`**

Simply open the **Meta Quest Browser** inside the headset, navigate to that URL, and click **"Enter AR"**!

### Building and Publishing to GitHub Pages
- **Automatic (CI/CD)**: Any push to `main` triggering `.github/workflows/deploy-pages.yml` automatically tests, builds, and publishes to GitHub Pages.
- **Manual from Desktop**:
  ```bash
  cd panther-builder
  npm run deploy
  ```
  This builds the production bundle and pushes it to the `gh-pages` branch.

### Automated Testing
```bash
cd panther-builder
npm test
```
Runs unit tests validating scale calculations, boundary limits, and material presets.

---

## Project 2: GolfVR - 9-Hole Championship Mini Golf (`GolfVR/GolfVR/`)

A complete 9-hole miniature golf course in Unity built with the *Stylized Mini Golf* asset kit and SteamVR. All 9 holes are grounded flush on the indoor hall floor (`Y = 0.0`) with borders, realistic colliders, audio effects, and VR teleportation.

### Complete 9-Hole Course Card (Par 26)
| Hole | Name | Par | Features & Obstacles | Coordinates |
| :--- | :--- | :---: | :--- | :--- |
| **1** | **Panther Straightaway** | 2 | Clean intro straightaway with side containment bumpers | Tee `[-10, 0, 10]`, Cup `[-6, 0, 10]` |
| **2** | **Chicane Wave** | 3 | S-curve fairway with banked edge rails | Tee `[-10, 0, 6]`, Cup `[-6, 0, 6]` |
| **3** | **Panther Wedge** | 3 | Triangular wedge obstacle splitting the fairway | Tee `[-10, 0, 2]`, Cup `[-6, 0, 2]` |
| **4** | **Gateway Bumpers** | 3 | Dual twin bumper slalom gates | Tee `[-2, 0, 6]`, Cup `[2, 0, 6]` |
| **5** | **Slalom Chicane** | 3 | Cross obstacle puzzle requiring bank rebound | Tee `[6, 0, 6]`, Cup `[10, 0, 6]` |
| **6** | **The Zippo Flame** | 3 | **Historic Bradford Zippo Lighter Monument** + Bumper pin hazard | Tee `[-2, 0, 0]`, Cup `[2, 0, 0]` |
| **7** | **Dogleg Corner** | 3 | 90-degree dogleg turn with banked corner tile | Tee `[6, 0, 2]`, Cup `[8, 0, -2]` |
| **8** | **The Windmill Hazard** | 3 | Rotating animated windmill blades obstacle | Tee `[-2, 0, -6]`, Cup `[2, 0, -6]` |
| **9** | **Grand Finale: Panther's Roar** | 4 | **Authentic Pitt Panther Statue Monument** centerpiece + L-wrap green | Tee `[6, 0, -6]`, Cup `[10, 0, -10]` |

### Lab Hardware & Software Prerequisites
- **Unity Editor**: `2022.3.62f3 LTS` (recommended) or `6000.3.20f1`.
- **SteamVR**: Installed and running on the VR workstation.
- **Headset**: Meta Quest 2 / 3 / Pro (via Quest Link cable or AirLink), or Valve Index / HTC Vive Cosmos.

### Opening & Testing in the Unity Editor
1. Launch **Unity Hub**.
2. Click **Add** -> **Add project from disk** -> Navigate to `c:\GitHub\Alumni-Weekend-2026\Alumni-Weekend-2026\GolfVR\GolfVR`.
3. Open the project using Unity `2022.3` LTS.
4. In the Project window, double-click:
   ```
   Assets/Scenes/MiniGolf_AlumniCourse.unity
   ```
5. Ensure **SteamVR** is running and your VR headset status shows green (Ready).
6. Click the **Play** button at the top of the Unity Editor.
7. Grab the putter with your VR controller trigger and begin putting!

### Building a Standalone Windows VR Executable (.exe)
To create a high-performance standalone build that runs without launching the Unity Editor:
1. In Unity, go to **File** -> **Build Settings...**
2. In **Scenes In Build**, verify `Assets/Scenes/MiniGolf_AlumniCourse.unity` is listed and checked at Index 0.
3. In **Platform**, select **Windows, Mac, Linux** (Architecture: `Intel 64-bit`).
4. Click **Build and Run** (or **Build**).
5. Choose or create a folder: `GolfVR/Builds/AlumniMiniGolf/`.
6. Unity compiles the standalone player `AlumniMiniGolf.exe`.
7. Put on the VR headset, launch `AlumniMiniGolf.exe`, and play.

### Automated Logic & Physics Testing
To run the automated course simulation suite (validating cup detection, scoring terms, stroke counters, audio synthesis, and putter physics):
```bash
python test_golf_logic.py
```
Expected output:
```
Running GolfVR test suite...
[PASS] test_score_terms passed
[PASS] test_nine_hole_round passed (Total strokes: 26, Par: 26, Diff: 0)
[PASS] test_physics_impulse passed
[PASS] test_fanfare_audio_synthesis passed
[PASS] test_putt_audio_synthesis passed
ALL TESTS PASSED SUCCESSFULLY!
```

---

## Troubleshooting in the VR Lab

- **SteamVR shows "Headset Not Detected"**: Unplug and replug the USB-C Link cable, confirm Quest Link is enabled inside the Quest headset settings, and restart SteamVR.
- **Controller Putter Not Grabbing**: Point controller at the putter handle and squeeze the grip or trigger button.
- **Teleportation**: Use the thumbstick forward/arc teleportation to jump across the hall and position yourself at any hole's tee area.
- **Lighting / Shader Warnings**: Clean baked GI configuration with version-compatible LightingData reference ensures clean loading with zero console errors.
