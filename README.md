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
- Development server starts at `https://localhost:8080` (or your local IP).
- Open the URL inside the **Meta Quest Browser** on the same Wi-Fi network.
- Click **"Enter Mixed Reality"** to place the Panther on the floor or tables in the VR Lab with full color passthrough.

### Creating a Production Build
```bash
cd panther-builder
npm run build
```
- Outputs optimized static web assets into `panther-builder/dist/`.
- Serve `dist/` using any SSL-enabled http server (e.g. `npx serve dist -s -p 8080 --ssl`).

### Automated Testing
```bash
cd panther-builder
npm test
```
Runs unit tests validating scale calculations, boundary limits, and material presets.

---

## Project 2: GolfVR - 9-Hole Championship Mini Golf (`GolfVR/GolfVR/`)

A 9-hole miniature golf course in Unity built with the *Stylized Mini Golf* asset pack and SteamVR, featuring the authentic campus Panther statue monument on Hole 9.

### Course Holes (Par 26)
1. **Hole 1**: Bradford Welcome (Par 2) - Straight intro fairway.
2. **Hole 2**: Blaisdell Dogleg (Par 3) - Bank turns and rail rebounds.
3. **Hole 3**: Tunungwant Hill (Par 3) - Incline with crest roll-down.
4. **Hole 4**: Allegheny Loop (Par 3) - Spiral tunnel and momentum curves.
5. **Hole 5**: Panther's Leap (Par 3) - Narrow bridge over water.
6. **Hole 6**: Marilyn Horne S-Curve (Par 3) - Double chicane with slalom bumpers.
7. **Hole 7**: Kessel Ridge (Par 3) - Elevated ridge spine with side sand traps.
8. **Hole 8**: Windmill Challenge (Par 3) - Rotating obstacle blades.
9. **Hole 9**: Championship Panther's Den (Par 3) - Multi-tier green surrounding the authentic campus Panther statue monument.

### Prerequisites
- Unity Editor: `6000.3.20f1` or `2022.3.62f3 LTS`.
- SteamVR running on the VR Lab Workstation.
- Meta Quest 2 / 3 / Pro (via Quest Link or AirLink), or Valve Index / HTC Vive.

### Opening & Running in Editor
1. Open **Unity Hub** -> Add -> Select `GolfVR/GolfVR`.
2. Open the project.
3. In the Project window, open:
   ```
   Assets/Scenes/MiniGolf_AlumniCourse.unity
   ```
4. Start **SteamVR** and put on your VR headset.
5. Click **Play** at the top of the Unity Editor.

### Making a Standalone PC VR Build
```bash
1. In Unity, File -> Build Settings...
2. Ensure Assets/Scenes/MiniGolf_AlumniCourse.unity is checked at Index 0.
2. Select Platform: Windows, Mac, Linux (Target: Windows, x86_64).
4. Click Build and Run -> choose folder (e.g. GolfVR/Build/).
```

### Automated Course Logic Verification
```bash
python test_golf_logic.py
``
Validates cup-trigger collisions, stroke tracking, out-of-bounds respawns, and par-tallying across all 9 holes.
