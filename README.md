# DriveRight — Complete Technical Handover

This branch contains the technical handover documentation for the DriveRight Unity project.

- **Unity source branch:** `siddharth_main`
- **Documentation branch:** `handover/driveright-documentation`
- **Unity Editor:** 2022.3.30f1

> Open the source branch in Unity. This documentation branch is for the guides; it is not the source branch to build from.

## What is this project?

DriveRight is a Unity driving / road-safety training application. The source repository contains multiple target experiences in one Unity project:

1. Android touchscreen app.
2. Meta Quest standalone headset VR app.
3. Google Cardboard phone VR integration (plugin and startup script are present; complete scene/build wiring must be verified).

The Quest app is not a separate codebase. It uses the same Unity project but requires the Quest scene flow, Android build target, XR provider, headset rig, controller input and VR UI to be configured. See the build guide before attempting a Quest build.

## Documentation index

### Start here

- [Installation and build manual](docs/SETUP_AND_BUILD.md) — clone, Git LFS, Unity setup, packages, Android touchscreen build, Quest build, Cardboard build and release checklist.
- [Architecture and code map](docs/ARCHITECTURE_AND_CODE_MAP.md) — scene flows, script responsibilities, gameplay data flow, localization, road graph and service structure.
- [Mobile, Quest and Cardboard modes](docs/MOBILE_AND_VR_MODES.md) — what differs between target versions and exact Quest-vs-mobile build considerations.
- [Supabase integration](docs/SUPABASE_INTEGRATION.md) — detailed REST/Storage design, data contracts, database model, security, setup, testing and known implementation defects.
- [Developer guide](docs/DEVELOPMENT_GUIDE.md) — how to extend the project, inspect scene wiring, debug gameplay, work with graph data and maintain the codebase.

## Repository notes

- Source branch reviewed: `siddharth_main`.
- Unity version: `2022.3.30f1`.
- Important packages include URP 14.0.11, XR Interaction Toolkit 2.5.4, XR Plug-in Management 4.5.3, Oculus XR Plugin 4.2.0, OpenXR 1.10.0 and Google Cardboard XR Plugin v1.29.0.
- Git LFS is used for some assets and settings. Run `git lfs pull` before opening the project.
- The actual scene hierarchy, Inspector references, XR provider activation and Build Settings scene list must be verified in Unity because relevant serialized files are LFS-backed.

## Important verification boundaries

The guides separate code findings from assumptions:
- Script behavior and package dependencies were inspected from the source branch.
- Scene names and scene navigation mappings are documented where visible in code/repository.
- Scene object hierarchy, camera rig wiring, build profiles, Player Settings, database schema, RLS and Storage policies need direct verification in Unity or the Supabase dashboard.
- The presence of Cardboard code/package is not proof of a working Cardboard build.
- The presence of Oculus/OpenXR packages is not proof of which Quest XR loader is currently active.

Never ship a Supabase service-role/secret key in a Unity client. Use a publishable key with restrictive database grants and RLS.
