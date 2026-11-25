# Installation, Configuration and Build Manual

This guide explains how to obtain the DriveRight source project, restore dependencies, open it in Unity, and build the three target experiences: Android touchscreen, Meta Quest standalone VR, and Google Cardboard phone VR.

## 1. Project and branch

- Repository: https://github.com/SiddH1212/DriveRight
- Unity source branch reviewed: `siddharth_main`
- Documentation branch: `handover/driveright-documentation`
- Unity Editor version: **2022.3.30f1**
- Unity project root: repository root (contains `Assets`, `Packages`, and `ProjectSettings`).

The Quest experience is part of this same Unity project. It is not a separate Android Studio app or separate repository. The same project is configured for different scene flows and input/display systems.

## 2. Prerequisites

Install through Unity Hub:

- Unity Editor 2022.3.30f1.
- Android Build Support.
- Android SDK & NDK Tools.
- OpenJDK.

Also install Git and Git LFS. For Quest testing, have a Meta Quest headset, USB data cable, developer mode enabled, and the device authorized for USB debugging. For Cardboard, have a compatible Android phone and Cardboard viewer.

Do not upgrade Unity or XR packages before reproducing the current project. The project uses pinned package versions and Unity 2022.3 APIs. Perform upgrades on a separate branch.

## 3. Clone source and restore Git LFS assets

```bash
git clone --branch siddharth_main --single-branch https://github.com/SiddH1212/DriveRight.git
cd DriveRight
git lfs install
git lfs pull
git lfs ls-files
```

Git LFS matters because scenes and project settings may be stored as LFS objects. A pointer file is not the actual scene/settings content. If Unity shows missing scenes, missing serialized references, or empty build settings, confirm that LFS objects downloaded correctly before troubleshooting the project itself.

## 4. Open the Unity project

1. Open Unity Hub and add the cloned repository root.
2. Select Unity 2022.3.30f1.
3. Allow Package Manager to resolve dependencies and Unity to import assets.
4. Wait for compilation and shader import to finish.
5. Open the Console and resolve compile errors before building.
6. Check for missing scripts and missing Inspector references in all target scenes.

If the Cardboard Git package cannot be fetched, check that Git is installed and available on PATH, then retry package resolution.

## 5. Important package dependencies

The source `Packages/manifest.json` includes:

| Package | Version | Purpose |
|---|---:|---|
| Universal Render Pipeline | 14.0.11 | Rendering |
| XR Interaction Toolkit | 2.5.4 | XR interaction components |
| XR Plug-in Management | 4.5.3 | XR provider/loader lifecycle |
| Oculus XR Plugin | 4.2.0 | Oculus/Meta XR provider available to this project |
| OpenXR Plugin | 1.10.0 | OpenXR provider available to this project |
| Google Cardboard XR Plugin | Git tag v1.29.0 | Phone-based Cardboard VR |
| TextMesh Pro | 3.0.9 | UI text |
| Splines | 2.6.0 | Spline-related functionality |

The package list only proves packages are installed. It does not prove a specific provider is enabled for Android, that a scene contains the correct XR rig, or that the build is compatible with a target headset.

## 6. Understand the three build targets

| Target | Device | Primary scene flow | Main interaction |
|---|---|---|---|
| Android touchscreen | Android phone/tablet | `Start_Mobile` → `Mobile2` / `Night_Mobile` | Touch UI, `ArcSteering`, Android controls |
| Meta Quest VR | Quest headset | `Start_Quest` → `VR` / `Night_Quest` | XR rig, headset tracking, controller/VR UI |
| Google Cardboard | Android phone + viewer | Cardboard-compatible scene flow must be confirmed | Phone sensors, stereoscopic display, Cardboard gear/trigger/recenter |

The Quest build is still an **Android APK** because Quest runs Android. The difference is that it is configured for headset XR, not normal phone display/input. Do not confuse “Android build target” with “touchscreen mobile experience”.

## 7. Build A — Android touchscreen

### 7.1 Switch platform

1. Open **File → Build Settings** (Unity 2022.3 workflow).
2. Select **Android**.
3. Choose **Switch Platform**.
4. Wait for asset reimport.

### 7.2 Android Player Settings

Review **Player Settings → Android**:

- Package Identifier: set the app's intended reverse-domain ID.
- Minimum API Level: use a level compatible with the intended devices and Unity 2022.3.
- Target API Level: use the currently required Play/device target.
- Scripting Backend: IL2CPP is suitable for release; Mono may be useful for local debugging if supported by the target configuration.
- Target Architectures: ARM64 for modern Android distribution; include ARMv7 only if there is a documented device requirement.
- Orientation: set to the orientation expected by the mobile UI.
- Internet Access: required for Supabase REST/Storage calls.
- Graphics APIs: keep the project-tested settings unless a device compatibility issue requires a change.

Do not copy Quest-only XR settings into the touchscreen build. The mobile target should launch the non-VR camera and touchscreen controls.

### 7.3 Scenes

The source code maps the mobile menu to:

- `Start_Mobile`
- `Mobile2` for Basic
- `Night_Mobile` for Advanced

Older/alternate scene names may exist (for example `EasyMobile`), but the active code mapping above is the one observed in `StartMobile.cs`. Add the scenes actually required by the app to Build Settings in the intended launch order. The exact serialized Build Settings list is LFS-backed and must be inspected in the Editor.

### 7.4 Mobile build verification

1. Confirm `Start_Mobile` is first in the build scene list.
2. Confirm `StartMobile` references its Canvas objects, buttons, dropdowns and loading panel.
3. Confirm the selected user and language are available before starting a run.
4. Confirm the mobile gameplay scene has `GameManagerMobile`, car, camera, touch controls, road graph, path renderer and destination references wired.
5. Build and install the APK.
6. Test Basic and Advanced flows.
7. Test touch steering, acceleration, brake and gear.
8. Test Supabase user/session/violation calls with a development backend.
9. Test offline behavior; current network operations are not durably queued.

## 8. Build B — Meta Quest standalone VR

### 8.1 Key concept: same Unity project, different runtime configuration

The Quest build uses the same C# codebase and assets, but it must launch the Quest menu/scenes and initialize an XR provider. It uses the Android build target to create an APK for the headset.

The observed Quest-oriented scenes are:

- `Start_Quest`
- `VR` (Basic)
- `Night_Quest` (Advanced)

`StartVR.cs` navigates Basic to `VR` and Advanced to `Night_Quest`. The `SceneModeBootstrapper` checks `AppMode.UseVR`, initializes the active XR loader, starts XR subsystems, enables the VR rig, and disables the non-VR camera and Android controls. `GameManagerMobile` contains VR-specific UI selection logic based on `AppMode.UseVR`, so the same gameplay manager is used by the mobile/VR scene variants in this code path.

### 8.2 Switch to Android platform

1. Open **File → Build Settings**.
2. Select **Android** and switch platform.
3. Keep Android as the target platform; Quest does not require selecting a desktop Windows build target for a standalone APK.

### 8.3 Configure XR Plug-in Management

Open **Edit → Project Settings → XR Plug-in Management**.

1. Select the Android tab.
2. Inspect the enabled providers. The repository includes Oculus XR Plugin and OpenXR packages, but the correct provider choice for the currently wired project must be confirmed from the actual project settings and headset version.
3. Do not enable multiple providers blindly. Choose and configure the provider intended by the project owner/tested build.
4. If using OpenXR, inspect the Android OpenXR feature set and Meta Quest feature group/features required by the installed package version.
5. If using the Oculus provider, inspect its Android settings and supported Quest target.
6. Confirm the active loader initializes on device. The script's successful `XRGeneralSettings.Instance.Manager.InitializeLoader()` is required for its runtime path.

The source tree contains both Oculus XR and OpenXR packages. It does not, by itself, prove which provider is selected in Project Settings. Check the Editor settings before producing a release build.

### 8.4 Quest scene setup

For each Quest scene:

- Confirm the scene is included in Build Settings.
- Confirm a VR rig/XR Origin exists and its camera is assigned.
- Confirm the rig is assigned to `SceneModeBootstrapper.vrRig`.
- Confirm the non-VR camera is assigned to `nonVrCamera`.
- Confirm Android controls are assigned to `androidControls`.
- Confirm VR panel, gear UI and steering UI references are assigned.
- Confirm the player/car object and camera are aligned to the intended starting position.
- Confirm controller input actions are bound to the correct XR controller controls.
- Confirm UI canvases are configured for world-space/headset viewing and are reachable by the intended interaction method.
- Confirm the scene has the gameplay manager, road graph, path renderer, destination, instructions and violation detectors.
- Confirm the correct night/day graph data is present in StreamingAssets.

These are Inspector-level references; script presence alone cannot verify them.

### 8.5 Quest Android Player Settings

Review:

- Package Identifier unique to the Quest app.
- ARM64 architecture.
- IL2CPP for release distribution (confirm the target provider's requirements).
- Internet access if using Supabase.
- Graphics API and color space according to the tested Quest configuration.
- Minimum/target Android API levels compatible with the installed Unity/Meta runtime.
- Multithreaded rendering and other performance options only after device testing.
- Application orientation/display settings appropriate to headset runtime.

Do not guess at Quest-specific texture compression, refresh rate, Vulkan/OpenGLES, or API-level settings; use the Meta/Unity requirements for the actual XR provider and test headset.

### 8.6 Build and deploy

1. Ensure the active scenes include `Start_Quest`, `VR`, and `Night_Quest`.
2. Ensure Android is selected and Quest XR provider settings are correct.
3. Connect the headset with USB debugging authorized.
4. Build an APK.
5. Install/run through Unity Build & Run or an approved ADB workflow.
6. Verify the XR loader starts and the app enters headset mode.
7. Test controller tracking, input, steering, braking, gear, VR UI, score UI, path instructions and scene transitions.
8. Test Supabase calls from the headset's network connection.

### 8.7 Important AppMode caveat

`AppMode.UseVR` is a static in-memory boolean. The PlayerPrefs implementation is commented out. It defaults to false after a cold app launch. `ModeSwitcher` updates it and reloads the active scene, but this value is not persisted across process restarts.

Therefore, do not assume that building the Quest APK automatically makes `AppMode.UseVR` true. Confirm the Quest entry flow sets the mode before the gameplay scene's `SceneModeBootstrapper` and UI scripts run. If it does not, make the target mode explicit in the Quest entry point (or use a build-target configuration flag) and test a cold launch. This is a code-level handover item, not a Build Settings checkbox.

## 9. Build C — Google Cardboard phone VR

### 9.1 What Cardboard is

Cardboard is phone-based VR: the Android phone is placed in a viewer and uses stereoscopic rendering and phone tracking. It is not the same as Quest VR and does not use Quest controllers.

The repository includes:
- Google Cardboard XR Plugin Git dependency v1.29.0.
- `CardboardStartup.cs`, which imports `Google.XR.Cardboard` and calls the Cardboard API for viewer parameters, gear/close buttons, recentering and screen parameter updates.

### 9.2 What must be verified

The package and startup script prove code-level integration, not a complete working Cardboard target. Verify:

- Cardboard XR Plug-in is enabled for Android under XR Plug-in Management.
- The intended scene contains the Cardboard XR camera/XR rig and the `CardboardStartup` component.
- The camera is configured for Cardboard stereo rendering.
- Input/gear/trigger behavior works with the viewer.
- The scene's controls are usable without a touchscreen overlay obscuring the VR view.
- Android manifest/permissions and Player Settings are compatible with the plugin version.
- The Cardboard scene is included in Build Settings.

The currently observed menu navigation maps `StartMobile` to touchscreen scenes and `StartVR` to Quest-named scenes. A dedicated Cardboard menu/scene mapping was not proven from the inspected code. Do not claim Cardboard is a separate fully wired scene until this is confirmed in Unity.

### 9.3 Build workflow

1. Switch Build Settings to Android.
2. Enable/configure Google Cardboard XR Plug-in for Android.
3. Select the Cardboard-compatible scene flow after confirming the rig and component wiring.
4. Confirm the project uses the Cardboard XR camera/rig and does not initialize the Quest provider for this target.
5. Build an Android APK.
6. Install on a compatible phone.
7. Place phone in the viewer and verify head tracking, stereo rendering, recentering and gear/close controls.

Cardboard and Quest are both VR, but their XR providers, hardware, camera setup, interaction model and device deployment are different.

## 10. How to choose the target at build time

Treat these as separate build configurations, not as a single APK that magically detects all hardware:

- **Touchscreen Android:** no headset XR initialization; normal camera and Android touch controls.
- **Meta Quest:** Android APK + Quest XR provider + headset rig + Quest scenes + controller/VR UI.
- **Cardboard:** Android APK + Cardboard provider + Cardboard rig + phone viewer + Cardboard input.

A maintainable next step is to define explicit build profiles or scripting symbols/config assets for each target, and use a deliberate startup scene/boot configuration. Avoid relying only on a static runtime boolean to decide the build identity.

## 11. Build failure checklist

- Wrong Unity version or package resolution: confirm 2022.3.30f1 and Package Manager dependencies.
- Missing scenes/assets: run `git lfs pull`.
- XR loader null: inspect XR Plug-in Management Android provider selection and package compatibility.
- Quest opens as flat 2D: verify Quest provider enabled, XR rig present/active, and `AppMode.UseVR` true before bootstrap.
- Mobile opens with VR rig: verify the non-VR mode is selected and the correct mobile scene is being built.
- Cardboard opens as flat 2D: verify Cardboard provider and Cardboard rig/stereo setup; the presence of `CardboardStartup` alone is insufficient.
- Missing controls: inspect serialized Input Action asset references and scene Inspector fields.
- Supabase requests fail: verify internet access, URL/key, grants, RLS and Storage policies.
- Graph load fails: confirm the correct `graph_data.dat` / `graph_data_night.dat` file is under StreamingAssets and scene-name mapping is correct.

## 12. Release checklist

Before handing an APK to another person:

- [ ] Record Git commit SHA and Unity version.
- [ ] Confirm LFS assets are materialized.
- [ ] Confirm the intended build target and active XR provider.
- [ ] Confirm correct startup scene and scene list.
- [ ] Confirm all scene Inspector references.
- [ ] Confirm correct road graph data for each scene.
- [ ] Confirm platform-specific controls and UI.
- [ ] Confirm Supabase environment and security policies.
- [ ] Test fresh install/cold launch, not only Editor Play Mode.
- [ ] Test offline and backend failure cases.
- [ ] Test on the actual target hardware.
- [ ] Record APK version, package ID, target device and known limitations.

## 13. Official platform references

- [Unity Android build settings](https://docs.unity3d.com/2022.3/Documentation/Manual/android-BuildProcess.html)
- [Unity XR Plug-in Management](https://docs.unity3d.com/Packages/com.unity.xr.management@4.5/manual/index.html)
- [Meta Unity development overview](https://developers.meta.com/horizon/documentation/unity/unity-development-overview/)
- [Google Cardboard Unity quickstart](https://developers.google.com/cardboard/develop/unity/quickstart)
