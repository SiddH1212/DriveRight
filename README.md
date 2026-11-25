# DriveRight — Technical Handover

This branch contains the handover documentation for the DriveRight Unity project. The Unity source is maintained on `siddharth_main`; this branch is intended to hold documentation and should not be treated as the application source branch.

## Documentation

- [Supabase integration — detailed technical guide](docs/SUPABASE_INTEGRATION.md)

## Project scope

The Unity project contains mobile gameplay and a separate Quest-oriented VR flow. The repository also contains Google Cardboard-related package/code references. Treat these as three distinct deployment targets when preparing builds:

1. Android touchscreen
2. Meta Quest headset
3. Android phone running Google Cardboard VR (integration/build readiness must be verified in Unity)

## Source branch and editor

- Source branch: `siddharth_main`
- Unity Editor version recorded in `ProjectSettings/ProjectVersion.txt`: 2022.3.30f1
- Main gameplay and integration scripts: `Assets/Scripts/`
- Supabase integration scripts: `Assets/Scripts/Supabase/`

## Important handover notes

- Open the source branch in Unity, not this documentation branch, unless the documentation has been merged into the source branch.
- Git LFS is used for some project assets/settings. Ensure Git LFS files are downloaded before opening or building.
- Scene wiring, Inspector references, Build Settings, XR settings and Supabase dashboard policies must be checked in the Unity Editor/Supabase project. Source code alone cannot prove those external settings are correct.
- Never put a Supabase secret/service-role key in a Unity client. The app is a public client; use a publishable key with least-privilege database grants and Row Level Security (RLS).

