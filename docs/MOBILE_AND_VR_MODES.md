# Mobile, Meta Quest and Google Cardboard Modes

## 1. One Unity project, three deployment experiences

DriveRight's Unity repository contains code and dependencies for multiple deployment targets. The Quest version is not a separate codebase. It is built from the same Unity project with a different scene flow, XR provider configuration, rig, input and UI.

| Dimension | Android touchscreen | Meta Quest | Google Cardboard |
|---|---|---|---|
| Hardware | Android phone/tablet | Quest headset | Android phone + Cardboard viewer |
| Build platform | Android | Android | Android |
| Display | Flat phone screen | Head-mounted stereo XR | Phone-based stereo viewer |
| Camera | Non-VR camera | XR rig camera | Cardboard XR camera/rig |
| Input | Touchscreen UI | XR controllers / VR UI | Viewer/phone tracking and Cardboard controls |
| Entry flow | Start_Mobile | Start_Quest | Dedicated Cardboard entry must be verified |
| Basic scene | Mobile2 | VR | Must be verified |
| Advanced scene | Night_Mobile | Night_Quest | Must be verified |
| XR provider | No headset loader for normal touch mode | Quest-compatible Oculus/OpenXR provider | Google Cardboard XR Plugin |

## 2. Evidence for each target in the source

### Android touchscreen

- `StartMobile.cs` is the mobile menu.
- Basic opens `Mobile2`.
- Advanced opens `Night_Mobile`.
- `ArcSteering.cs` implements pointer/drag steering.
- `GameManagerMobile.cs` manages mobile gameplay and contains the non-VR UI path.
- `SceneModeBootstrapper` enables Android controls and non-VR camera when `AppMode.UseVR` is false.

### Meta Quest

- `StartVR.cs` navigates Basic to `VR` and Advanced to `Night_Quest`.
- `SceneModeBootstrapper` initializes the configured XR loader, starts XR subsystems, enables the VR rig, and disables the non-VR camera/Android controls.
- `GameManagerMobile.cs` contains VR UI branches using `AppMode.UseVR`.
- Packages include XR Plug-in Management, Oculus XR and OpenXR.
- The project contains Quest-oriented scene names.

This supports the conclusion that the project has a Quest-oriented version in the same Unity project. Actual provider enablement, rig/controller wiring and build readiness must be confirmed in Unity.

### Google Cardboard

- `Packages/manifest.json` includes Google Cardboard XR Plugin v1.29.0.
- `CardboardStartup.cs` imports `Google.XR.Cardboard` and calls `Api.HasDeviceParams`, `ScanDeviceParams`, `IsGearButtonPressed`, `IsCloseButtonPressed`, `IsTriggerHeldPressed`, `Recenter`, `HasNewDeviceParams`, `ReloadDeviceParams` and `UpdateScreenParams`.

This is clear code-level Cardboard integration. It does not prove that a complete Cardboard scene, camera rig, provider settings and Build Settings entry are wired. The inspected menu mappings do not show a dedicated Cardboard scene route.

## 3. What changes when building Quest instead of touchscreen mobile?

This is the most important operational distinction. Both are Android builds, but the Quest APK must be configured as a headset XR application.

### A. Scene selection

Touchscreen:
- Start with `Start_Mobile`.
- Basic → `Mobile2`.
- Advanced → `Night_Mobile`.

Quest:
- Start with `Start_Quest`.
- Basic → `VR`.
- Advanced → `Night_Quest`.

Make sure all required scenes are in Build Settings. The serialized Build Settings file is LFS-backed, so inspect it after LFS checkout in Unity.

### B. XR provider

Touchscreen:
- Normal flat-screen camera.
- Do not start the Quest XR loader.
- Android touch controls are enabled.

Quest:
- Android remains the build target.
- Enable the Quest-compatible provider in XR Plug-in Management.
- Configure Oculus XR or OpenXR according to the provider actually intended for this project. Both packages exist; the source does not prove which one is currently enabled.
- Confirm XR loader initialization on the headset.

Do not blindly enable both providers or assume package installation means provider activation.

### C. Camera and rig

Touchscreen:
- Use the non-VR camera.
- Android controls are enabled.

Quest:
- A headset XR rig must be present and correctly assigned.
- The XR rig is enabled after loader initialization.
- The non-VR camera and Android controls are disabled.
- VR panel, gear and steering UI objects are enabled by the bootstrapper.

Inspect the serialized references on `SceneModeBootstrapper` in each scene. The script expects references for `vrRig`, `nonVrCamera`, `androidControls`, `VRPanel`, `Gear` and `steering`.

### D. Input

Touchscreen:
- Touch pointer events feed `ArcSteering.steeringValue`.
- Acceleration, brake and gear use Input System actions.
- CarController has a legacy horizontal-axis fallback.

Quest:
- The scene must bind the intended controller actions.
- VR UI must be interactable with the chosen XR interaction system.
- Touchscreen-only controls must not remain the only way to steer/accelerate/brake.
- Test gear and brake interactions on the actual controller hardware.

The source shows InputActionReference fields but the actual serialized binding asset and per-scene assignments must be inspected in the Editor.

### E. Runtime mode flag

`AppMode.UseVR` currently stores a static in-memory bool. Its PlayerPrefs-based implementation is commented out. It defaults to false on a cold launch.

`SceneModeBootstrapper` checks this value after two frames:
- true → initialize XR, enable VR rig, disable non-VR camera and Android controls, enable VR UI.
- false → stop XR subsystems, disable VR rig, enable non-VR camera and Android controls, disable VR UI.

`ModeSwitcher` changes the value and reloads the current scene. A static variable does not persist after the application process exits.

**Quest build requirement:** make sure the Quest entry point sets `AppMode.UseVR = true` before the gameplay scene's bootstrapper and VR-dependent UI scripts run. If this is not already done in the actual scene setup, add an explicit Quest startup configuration. Do not assume selecting Android as the build target sets this variable.

### F. Player Settings and output

Both builds use Android Build Settings. Quest is not built as a Windows desktop app when producing a standalone Quest APK.

Quest generally needs:
- ARM64.
- A Quest-compatible XR provider.
- Correct Android manifest/runtime configuration from the selected provider.
- Unique package identifier.
- Working headset rig and controller input.
- Internet access if Supabase is used.

Use the exact provider/version requirements for the target Quest model and Unity package versions. Do not guess graphics API or Android API levels; test them on hardware.

## 4. What changes for Cardboard compared with Quest?

Cardboard is phone VR, not headset-native Quest VR.

- It runs on a normal Android phone placed inside a viewer.
- It uses the Google Cardboard XR provider and phone tracking.
- `CardboardStartup` handles viewer parameter scanning and viewer button/recenter behavior.
- It does not use Quest controller bindings.
- It needs a Cardboard-compatible stereo camera/rig and scene setup.
- It should not initialize the Quest provider for that target.

A Cardboard APK and Quest APK are both Android outputs, but their XR providers and runtime hardware are different.

## 5. Shared gameplay logic

The mobile-named `GameManagerMobile` includes conditional UI logic for VR:
- Score/violation text switches between mobile and VR UI elements based on `AppMode.UseVR`.
- Violation bar selects the corresponding UI image.
- `DisplayInstructions` selects mobile or VR instruction text/CanvasGroup.
- Shared car physics and safety detector interfaces are used across scene variants.

This means platform parity depends heavily on correct Inspector wiring. The same script may be used in two scenes with different references.

## 6. Backend behavior across targets

The Supabase layer uses UnityWebRequest and is not intrinsically limited to touchscreen Android. Quest is Android too, so it can use the same REST/Storage service code if the service GameObjects are present and network access is enabled.

However, the observed `StartVR.cs` menu path does not show the same user selection and session startup calls present in `StartMobile.cs`. Verify whether the Quest app is expected to record users/sessions. If yes, wire the equivalent user/session flow rather than assuming it happens automatically.

See [Supabase Integration](SUPABASE_INTEGRATION.md).

## 7. Platform test checklist

### Touchscreen
- [ ] App opens `Start_Mobile`.
- [ ] User list loads and user can be created.
- [ ] Basic/Advanced scenes load.
- [ ] Touch steering, accelerator, brake and gear work.
- [ ] Non-VR camera is active.
- [ ] Android controls are active.
- [ ] Graph route, instructions, detectors and completion UI work.
- [ ] Supabase session and violation workflow is tested.

### Quest
- [ ] App opens `Start_Quest`.
- [ ] Quest XR loader initializes.
- [ ] App enters stereo headset view.
- [ ] `AppMode.UseVR` is true before mode bootstrap.
- [ ] VR rig active and flat camera disabled.
- [ ] Android touch controls disabled.
- [ ] Controller bindings and VR UI work.
- [ ] Basic/Advanced scenes load.
- [ ] Route, instructions, detectors, violation gallery and completion work.
- [ ] Supabase behavior is intentionally configured and tested.

### Cardboard
- [ ] Cardboard provider enabled.
- [ ] Cardboard rig/camera present.
- [ ] CardboardStartup attached and runs.
- [ ] Viewer parameters scan/load.
- [ ] Stereo view and phone head tracking work.
- [ ] Gear/recenter/close interactions work.
- [ ] Touch controls do not obstruct the VR view.
- [ ] Cardboard scene is included in Build Settings.

## 8. Official references

- [Meta Unity development overview](https://developers.meta.com/horizon/documentation/unity/unity-development-overview/)
- [Unity XR Plug-in Management documentation](https://docs.unity3d.com/Packages/com.unity.xr.management@4.5/manual/index.html)
- [Google Cardboard Unity quickstart](https://developers.google.com/cardboard/develop/unity/quickstart)
