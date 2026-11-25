# Architecture and Code Map

## 1. System overview

DriveRight is a Unity driving / road-safety training application. The repository is one Unity project with shared gameplay systems and target-specific scene/UI/input configurations.

The source branch reviewed is `siddharth_main`. Unity Editor version is 2022.3.30f1.

The project has three code/configuration targets to distinguish:

1. Android touchscreen.
2. Meta Quest standalone VR (Android APK with headset XR).
3. Google Cardboard phone VR (plugin/startup code exists; full scene/build wiring requires Editor verification).

The project should not be described as three independently maintained applications. Quest is a target configuration and scene flow inside the same Unity project.

## 2. Runtime architecture

At a high level:

```text
Startup / menu scene
  ├─ Touchscreen: Start_Mobile → user/language selection
  │    ├─ Basic → Mobile2
  │    └─ Advanced → Night_Mobile
  └─ Quest-oriented: Start_Quest
       ├─ Basic → VR
       └─ Advanced → Night_Quest

Gameplay scene
  ├─ CarController + ArcSteering / Input System
  ├─ GameManagerMobile (score, timer, violations, screenshots)
  ├─ Violation detectors
  ├─ RoadGraph → Pathfinder → PathRenderer
  ├─ DisplayInstructions + LanguageTranslator
  └─ Supabase services (device, user, session, violation, screenshot)

Completion
  ├─ LevelCompleteMobile
  ├─ local LastScore PlayerPrefs
  └─ SupabaseSessionService.EndSession
```

The diagram summarizes observed code relationships; actual GameObject references are assigned in scenes and must be checked in the Unity Editor.

## 3. Scene navigation and target scenes

| Scene name | Role inferred from code/repository |
|---|---|
| `Start_Mobile` | Touchscreen app entry/menu |
| `Mobile2` | Basic touchscreen gameplay |
| `Night_Mobile` | Advanced/night touchscreen gameplay |
| `Start_Quest` | Quest-oriented menu/entry scene |
| `VR` | Basic Quest gameplay |
| `Night_Quest` | Advanced/night Quest gameplay |
| `EasyMobile` | Present in repository; not the active Basic mapping in current StartMobile code |
| `SampleScene3` | Test/other scene; purpose requires Editor inspection |

`StartMobile.cs` explicitly maps Basic to `Mobile2` and Advanced to `Night_Mobile`. `StartVR.cs` maps Basic to `VR` and Advanced to `Night_Quest`.

The serialized Build Settings scene list is stored in an LFS-backed file and must be confirmed in Unity after `git lfs pull`.

## 4. Script inventory by subsystem

### 4.1 Entry, scene navigation and mode

| Script | Responsibility |
|---|---|
| `Scene Navigation and Scores Storage/StartMobile.cs` | Mobile menu, user selection/creation, loading UI, language/mode choice, mobile scene navigation |
| `Scene Navigation and Scores Storage/StartVR.cs` | Quest menu and Basic/Advanced scene navigation |
| `Scene Navigation and Scores Storage/SceneBootstrapper.cs` | Class name `SceneModeBootstrapper`; initializes/stops XR and toggles VR/non-VR objects based on `AppMode.UseVR` |
| `Scene Navigation and Scores Storage/AppMode.cs` | Static runtime `UseVR` flag; current implementation is in-memory |
| `Scene Navigation and Scores Storage/ModeSwitcher.cs` | Updates `AppMode.UseVR` and reloads current scene |
| `Scene Navigation and Scores Storage/CardboardStartup.cs` | Google Cardboard viewer parameter scanning, gear/close button, recenter and screen updates |
| `Scene Navigation and Scores Storage/InputMode.cs` | Checks controller/joystick availability |
| `Scene Navigation and Scores Storage/SessionManager.cs` | Legacy session/player state; inspect before reusing |

### 4.2 Gameplay managers and completion

| Script | Responsibility |
|---|---|
| `GameManagerBase.cs` | Abstract manager contract and shared violation record |
| `GameManagerMobile.cs` | Current mobile/VR gameplay manager implementation: score, countdown, violation count, capture, path updates and end-game |
| `GameManager.cs` | Older/legacy gameplay manager implementation |
| `LevelCompleteMobile.cs` | Current mobile-style completion UI, score/rank, violation image browser and session end |
| `LevelComplete.cs` | Older/legacy completion flow |

`GameManagerMobile` is named Mobile but contains branches that choose VR-specific UI elements when `AppMode.UseVR` is true. This is one reason Quest is not a separate gameplay codebase.

### 4.3 Vehicle and controls

| Script | Responsibility |
|---|---|
| `CarController.cs` | WheelCollider vehicle physics, steering, motor torque, braking, gear, speedometer and UI |
| `ArcSteering.cs` | Touch pointer/drag steering control; maps drag direction/distance to a smoothed -1..1 steering value |
| `CarIndicator.cs` | Turn indicator state and UI |
| `SteeringWheelVisualizer.cs` | Steering wheel visual support |
| `EngineAudio.cs` | Engine audio behavior |
| `WheelRotator.cs` | Wheel visual rotation (if used by scene) |
| `Viewer.cs` | Additional view/camera behavior; inspect scene references before changing |

`CarController.GetInput()` reads `arcSteering.steeringValue`, then falls back to legacy `Input.GetAxis("Horizontal")` if the value is approximately zero. Acceleration, brake and gear use Input System action references. The project therefore has both touch-specific input and action-based inputs; check the Input Actions asset and scene bindings for each platform.

### 4.4 Road graph, routing and navigation

| Script | Responsibility |
|---|---|
| `RoadGraph.cs` | Runtime road graph and lane-node access |
| `RoadGraphSerializer.cs` | Serializes/deserializes lane nodes and selects graph data by active scene |
| `Pathfinder.cs` | A* pathfinding over LaneNode outgoing edges |
| `PriorityQueue.cs` | Priority queue used by A* |
| `PathRenderer.cs` | Generates a mesh strip for the current route |
| `DisplayInstructions.cs` | Uses upcoming graph junctions to show straight/left/right instructions |

`RoadGraphSerializer` maps:
- `VR` and `Mobile2` → `graph_data.dat`
- `Night_Quest` and `Night_Mobile` → `graph_data_night.dat`

The graph is read from StreamingAssets. On Android, `UnityWebRequest.Get` reads the StreamingAssets resource and writes it to persistent data because Android StreamingAssets are not ordinary filesystem paths. On non-Android platforms, the code uses File.Exists/File.Copy.

A scene name not present in the serializer switch throws `NotImplementedException` if graph loading asks for a filename. Add new scene mappings whenever a new gameplay scene uses this system.

### 4.5 Traffic and safety detection

| Script | Responsibility |
|---|---|
| `ViolationDetector.cs` | Road/lane direction, turn indicator, lane-change and wrong-way checks |
| `RedLightViolationDetector.cs` | Trigger-based signal crossing detection |
| `SpeedViolationDetector.cs` | Reads speed limit from a sibling TMP object and scores speed at trigger entry |
| `StopViolationDetector.cs` | Tracks minimum speed in stop zone and scores stopping behavior |
| `YieldViolationDetector.cs` | Tracks minimum speed in yield zone and scores slowing |
| `TrafficSignalManager.cs` | Cycles traffic signal state and countdown |
| `LightController.cs` | Sets signal emission materials and exposes Red/Yellow/Green state |
| `HaloOnlyTrafficLightController.cs` | Alternate traffic-light visual controller |
| `CollisionDetector.cs` | Collision-related safety event handling |
| `RoadBehaviour.cs` | Road-specific behavior |
| `RoadFromGraph.cs`, `RoadFromGraphMod.cs` | Road generation/building from graph data |
| `RoadGenerator.cs` | Road geometry and lane data used by violation detection |
| `PedestrianController.cs`, `PedestrianMovement.cs`, `PedestrianStop.cs` | Pedestrian behavior |
| `NPCController.cs`, `NPCSpawner.cs`, `NPCIndicator.cs` | NPC traffic/indicator behavior |

Detector scripts commonly hold direct references to a `GameManagerBase`. The scene must assign those references to the correct manager instance.

### 4.6 Localization and UI

| Script | Responsibility |
|---|---|
| `LanguageSelector.cs` | Populates language dropdown and maps display names to language codes |
| `LanguageTranslator.cs` | Loads `Resources/violations_translation.json`, translates strings and selects TMP font |
| `DisplayInstructions.cs` | Displays route guidance in mobile or VR UI |
| `MinimapCamController.cs`, `MinimapRenderer.cs`, `PlayerMinimapArrow.cs` | Minimap camera, rendering and player marker |
| `ButtonCursor.cs` | UI cursor interaction support |
| `Framerate.cs` | Frame-rate utility |

Language codes currently mapped in `LanguageSelector`: English (en), Hindi (hi), Tamil (ta), Telugu (te), Malayalam (ml), Gujarati (gu), Bengali (bn), Urdu (ur), Kannada (kn). Font assets are assigned in the LanguageTranslator Inspector. Missing translation entries fall back to the English/source string.

### 4.7 Backend and persistence

The Supabase service layer is documented separately in [Supabase Integration](SUPABASE_INTEGRATION.md). It uses direct HTTP via UnityWebRequest and covers users, devices, sessions, violations and screenshots.

Local persistence also uses:
- PlayerPrefs `DEVICE_ID` for a persistent generated device GUID.
- PlayerPrefs `LastScore` for the last completed score.
- `Application.persistentDataPath/Captures` for locally saved violation JPGs.
- `Application.streamingAssetsPath` for road graph data.

## 5. Gameplay lifecycle

### 5.1 Mobile menu

1. `StartMobile.Awake` ensures the device ID exists and clears the local Captures directory.
2. Menu listeners are registered in Start.
3. Selecting Play opens the user/language/mode selection flow.
4. `LoadUsers` calls `SupabaseUserService.FetchUsers`.
5. A user can be created with name/email; the selected user's ID/name/email are stored in `UserSession`.
6. Selecting Basic/Advanced calls `SupabaseSessionService.StartSession`.
7. The menu asynchronously loads `Mobile2` or `Night_Mobile`.

The code starts the session request and scene load without waiting for the remote session insert to complete.

### 5.2 Gameplay initialization

`GameManagerMobile.Start`:
1. Sets gameplay inactive.
2. Initializes score to `initialScore` (default 100), elapsed time and violation count.
3. Clears UI and resets the violation bar.
4. Enables the back-to-menu Input Action.
5. Waits for a ready Pathfinder/RoadGraph.
6. Computes the route and renders it.
7. Runs a five-second countdown plus “Go!” display.
8. Enables violations and gameplay, then starts the route update loop.

### 5.3 Score and violation logic

- Red light: -20.
- Yellow light: 0.
- Green/other light: +5.
- Negative score changes after the countdown count as violations.
- Positive score events after the countdown are also captured as evidence by the current manager.
- Default maximum violation count is 10; reaching it triggers the failure completion flow.
- The exact scoring for road, lane, stop/yield, collision and speed events is defined in their detector scripts.

The code stores score changes and violation evidence in memory during the run. Relevant screenshots are saved to disk as JPGs when needed for the completion UI. Supabase reporting is attempted when the service singleton exists.

### 5.4 Completion

`LevelCompleteMobile` handles destination trigger or failure:
- Displays success/failure title, score and rank.
- Saves `LastScore` to PlayerPrefs.
- Calls `SupabaseSessionService.EndSession(score)` if the service exists.
- Displays violation evidence images by loading JPGs from persistent data.
- Pauses gameplay using `Time.timeScale = 0`.

## 6. Critical shared state and coupling

- `AppMode.UseVR`: static in-memory bool controlling which camera/UI objects are enabled and which UI fields are used. Defaults false on cold launch.
- `UserSession`: static selected user identity used by session creation.
- `LanguageTranslator.SelectedLanguage`: static selected language code.
- Persistent singleton services: Supabase services use `DontDestroyOnLoad`.
- Scene wiring: many scripts use serialized public/private fields assigned in Inspector. Renaming objects or scripts can break serialized references or Find/Transform lookups.
- Scene names: serializer uses exact string matching. Renaming scenes requires updating graph filename mapping and menu navigation.

## 7. Code-level cautions found during review

1. **AppMode is not persisted.** A Quest cold launch must explicitly set VR mode before the bootstrapper runs; do not assume the previous in-memory value survives restart.
2. **StartVR navigation is separate from StartMobile.** Quest scenes are entered through `StartVR`; its source does not itself show Supabase user/session startup.
3. **Session insert is asynchronous.** Gameplay can start before the session row is confirmed.
4. **Violation persistence has two implementation defects.** The current `SupabaseViolationService` serializes an anonymous object with `JsonUtility` and calls a coroutine without starting it. See the Supabase guide.
5. **First-seen timestamp may be overwritten.** Device POST sets first_seen and last_seen to now each run.
6. **Pathfinder A* implementation should be tested on graph edge cases.** It uses a priority queue and only enqueues a neighbor when it is not already in the open set; verify decrease-key behavior if route quality appears wrong.
7. **SceneModeBootstrapper null XR loader.** If XR provider initialization fails, the VR rig remains uninitialized; inspect provider settings and device logs.
8. **Some script fields are scene-dependent.** Missing TMP, CanvasGroup, InputActionReference, WheelCollider, Transform or manager references can cause null-reference exceptions.
9. **Legacy scripts remain.** `GameManager.cs` and `LevelComplete.cs` coexist with mobile-named versions; confirm which components are attached to a scene before editing.

## 8. What source review cannot establish

The repository source does not fully establish:
- Full scene GameObject hierarchy and serialized Inspector assignments.
- Exact Build Settings scene list (LFS-backed).
- Which Android XR provider is enabled in Project Settings.
- Whether the Quest rig and controllers are correctly configured.
- Whether the Cardboard startup script is attached to a working Cardboard scene.
- Actual Supabase database schema, RLS policies, Storage bucket visibility or Auth settings.
- Hardware-specific build/performance settings.

These must be checked in the Unity Editor, on hardware, and in the Supabase dashboard. Record the findings in this handbook as the project is validated.
