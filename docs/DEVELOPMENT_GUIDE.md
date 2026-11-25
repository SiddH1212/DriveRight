# Developer Guide and Maintenance Handover

This guide is for a developer taking over DriveRight without the original author available. It describes a safe workflow for understanding the project, changing features, testing the three deployment targets, and diagnosing common failures.

## 1. Before making changes

1. Check out `siddharth_main`, not the documentation branch.
2. Confirm Unity 2022.3.30f1.
3. Run `git lfs pull`.
4. Open the project and resolve compile errors before editing.
5. Inspect Build Settings and XR Plug-in Management.
6. Open each relevant scene and inspect its GameObjects/Inspector references.
7. Identify whether the scene is touchscreen, Quest, or Cardboard before changing input/camera/UI behavior.
8. Create a feature branch and commit small changes.

Do not assume a script is active just because it exists. Confirm that it is attached to a GameObject in the target scene or instantiated by code.

## 2. Three target configurations — do not mix them up

### Android touchscreen
- Entry/menu: `Start_Mobile`.
- Gameplay: `Mobile2`, `Night_Mobile`.
- Controls: touchscreen UI including `ArcSteering`, plus Input System actions for acceleration/brake/gear.
- Camera: non-VR camera.
- XR: should not initialize Quest XR.
- Build: Android APK.

### Meta Quest
- Entry/menu: `Start_Quest`.
- Gameplay: `VR`, `Night_Quest`.
- Controls: headset rig and VR/controller UI; confirm Input Action bindings in scene.
- Camera: XR rig/camera.
- XR: Quest-compatible provider enabled for Android in XR Plug-in Management.
- Build: Android ARM64 APK installed on Quest.
- The code uses `AppMode.UseVR`; make sure it is true before SceneModeBootstrapper and VR-specific UI logic execute. Current implementation is static in-memory and defaults false on cold start.

### Google Cardboard
- Android phone in viewer; not Quest hardware.
- Google Cardboard XR Plugin v1.29.0 and `CardboardStartup.cs` are present.
- Verify the Cardboard provider, stereo rig, scene wiring and input flow. Do not equate package presence with a validated build.
- Build: Android APK for a phone, with Cardboard XR provider/configuration.

For exact build steps, see [Installation and Build Manual](SETUP_AND_BUILD.md) and [Mode Comparison](MOBILE_AND_VR_MODES.md).

## 3. How to add or change a gameplay violation

1. Find the appropriate detector (for example, `RedLightViolationDetector`, `SpeedViolationDetector`, `StopViolationDetector`, `YieldViolationDetector`, or `ViolationDetector`).
2. Determine the event condition and score delta.
3. Call the shared `GameManagerBase` interface rather than coupling the detector to a specific concrete manager.
4. Confirm the detector's GameManager reference is assigned in each gameplay scene.
5. Confirm the message exists in `Resources/violations_translation.json` if it must be localized.
6. Test the event on touchscreen and Quest. Both may use `GameManagerMobile`, but their UI references differ.
7. Verify screenshot capture, local completion gallery and Supabase persistence.

Keep score changes and user-facing messages consistent. A detector that is present in one scene but not another creates different training rules between platforms.

## 4. How to change vehicle controls

The car is driven through `CarController` and WheelColliders.

- Steering reads `ArcSteering.steeringValue` first.
- If the value is approximately zero, the code falls back to `Input.GetAxis("Horizontal")`.
- Acceleration, brake and gear are Input System actions.
- Motor torque is applied to rear WheelColliders.
- Steering angle is applied to front WheelColliders.
- Wheel visual transforms are updated from WheelCollider world poses.

When changing controls:
1. Inspect the Input Actions asset and serialized InputActionReference assignments.
2. Confirm touchscreen controls exist and are active in mobile scene.
3. Confirm Quest controller actions are bound for the Quest scene.
4. Check the gear UI and `SetReverseFromUIToggle` hookup.
5. Test steering smoothing and dead zone at low and high speed.
6. Test device hardware; Editor keyboard fallback does not prove mobile or Quest input works.

## 5. How to change route navigation

The route pipeline is:

1. `RoadGraph` provides lane nodes.
2. `RoadGraphSerializer` loads graph data based on exact active scene name.
3. `Pathfinder.GetPath` selects nearby start/end candidates and runs A*.
4. `GameManagerMobile` initializes route and periodically checks deviation.
5. `PathRenderer` creates a mesh strip along the route.
6. `DisplayInstructions` checks upcoming junction branching and displays translated directions.

When adding a new gameplay scene:
- Add its graph filename mapping in `RoadGraphSerializer`.
- Put the corresponding graph file in StreamingAssets.
- Confirm node positions align with the scene geometry.
- Confirm RoadGraph reports IsReady before gameplay initialization proceeds.
- Assign player, destination, Pathfinder, PathRenderer and DisplayInstructions references.
- Test rerouting after deliberately deviating from the route.

Current graph mapping:
- VR / Mobile2 → `graph_data.dat`
- Night_Quest / Night_Mobile → `graph_data_night.dat`

If a new scene is not mapped, graph loading can throw `NotImplementedException`.

## 6. How to change localization

1. Add the language code to `LanguageSelector.languageMap` if adding a language.
2. Add the appropriate TMP font asset field/reference to `LanguageTranslator`.
3. Add translation entries to `Resources/violations_translation.json`.
4. Ensure the source English message exactly matches the message passed by gameplay code (the translator trims whitespace but otherwise uses dictionary key matching).
5. Test all relevant strings in mobile and VR UI; text overflow and world-space font sizing can differ.

Currently mapped language codes: en, hi, ta, te, ml, gu, bn, ur and kn.

## 7. How to maintain Supabase

Read [Supabase Integration](SUPABASE_INTEGRATION.md) before changing backend calls. It documents endpoints, payloads, schema assumptions, storage paths, RLS, error handling and known defects.

Rules:
- Never place a service-role/secret key in Unity.
- Keep payload fields aligned with database columns.
- Keep RLS and Storage policies restrictive.
- Test in a development project before production.
- Do not assume a client callback success means the entire multi-step workflow completed.
- Fix the violation coroutine and serialization defects before relying on violation rows.
- Consider a local retry queue for sessions/violations if offline use is expected.

## 8. Scene/Inspector checklist

For each target scene, inspect:

- Correct camera / XR rig and active state.
- SceneModeBootstrapper references: VR rig, non-VR camera, Android controls, VR panel, gear and steering.
- Correct GameManager implementation and UI references.
- CarController WheelCollider and wheel mesh references.
- InputActionReference bindings.
- ArcSteering knob and center references for touchscreen.
- Player Rigidbody, collider, tag and transform.
- RoadGraph and Pathfinder readiness.
- PathRenderer mesh/material/layer.
- Destination trigger and LevelCompleteMobile references.
- DisplayInstructions mobile/VR text and CanvasGroups.
- Violation detectors' GameManager and traffic signal references.
- LanguageTranslator font assets.
- Supabase singleton objects in menu/gameplay scenes (duplicates are destroyed, but intended bootstrapping should still be understood).

## 9. Debugging checklist

### NullReferenceException
Check serialized Inspector references first. Common dependencies include Canvas, TMP labels, buttons, InputActionReference, WheelCollider, Rigidbody, GameManager, Pathfinder and destination.

### XR does not start
Check Android XR provider selection, loader configuration, supported device, package version and runtime logs. Confirm the bootstrapper sees `XRGeneralSettings.Instance.Manager`.

### Quest launches in flat mode
Check the Quest entry scene, `AppMode.UseVR` initialization, active XR loader, rig activation and camera setup. The in-memory AppMode flag defaults false on cold launch.

### Touch steering does not work
Check EventSystem, GraphicRaycaster, ArcSteering pointer events, knob/center references and CarController.arcSteering. Confirm Android controls are active.

### Route is missing
Check StreamingAssets graph file, scene-name switch in RoadGraphSerializer, RoadGraph.IsReady, graph node connectivity and destination/player positions.

### Wrong violation messages/score
Inspect the trigger conditions, tags/object names, GameManagerBase reference, score delta and language translation key.

### Supabase requests fail
Inspect the response body and Supabase API logs. Check project URL, publishable key, grants, RLS, Storage bucket and network access.

## 10. Code documentation/comment standard

When adding or modifying code, document:
- What the component owns.
- Which scene/platform uses it.
- Required Inspector references.
- Input/output and units.
- Important lifecycle assumptions.
- Network/database side effects.
- Failure behavior and fallback.
- Any exact scene names, PlayerPrefs keys, StreamingAssets filenames or JSON field contracts.

Prefer XML summaries for public classes/methods and comments that explain why a non-obvious choice exists. Avoid comments that merely restate the next line.

Example:

```csharp
/// <summary>
/// Selects the road graph file associated with the currently loaded gameplay scene.
/// Keep this mapping synchronized with the scene names used by the menu scripts.
/// </summary>
private static string fileName { get { /* scene mapping */ } }
```

## 11. Known technical debt / handover risks

- AppMode is not persisted and may default to non-VR after cold launch.
- Quest menu source path does not show the same Supabase user/session onboarding as StartMobile; verify intended backend behavior for Quest.
- The session request is asynchronous and gameplay can begin before the row is confirmed.
- Violation row serialization and coroutine start require fixes.
- Device registration currently rewrites first_seen each launch.
- User fetch failure is indistinguishable from a genuinely empty user list in its callback.
- No durable offline retry queue is visible in the inspected client code.
- Legacy GameManager and LevelComplete coexist with newer Mobile-named classes.
- Cardboard plugin/startup code exists, but complete scene/build wiring is unverified.
- Scene hierarchy, serialized references, XR provider selection and Build Settings need Editor inspection.

## 12. Handover completion checklist

A future maintainer should leave the project in a state where another developer can reproduce it:

- [ ] Record the source commit SHA and Unity editor version.
- [ ] Confirm Git LFS objects are downloaded.
- [ ] Document all scenes and their purpose from the Editor.
- [ ] Capture the final Build Settings scene list.
- [ ] Capture Android Player Settings for each target profile.
- [ ] Capture XR Plug-in Management provider settings.
- [ ] Verify Quest rig/controller bindings on hardware.
- [ ] Verify Cardboard rig and viewer flow on hardware.
- [ ] Verify all critical Inspector references.
- [ ] Export/commit Supabase schema migrations and RLS/Storage policies (without secrets).
- [ ] Test user → session → violation → screenshot → session-end flow.
- [ ] Document known bugs, workarounds and tested device models.
