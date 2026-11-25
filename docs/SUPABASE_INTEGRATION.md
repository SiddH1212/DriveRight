# Supabase Integration — Technical Documentation

> **Source reviewed:** `siddharth_main`, primarily `Assets/Scripts/Supabase/` and the callers in the Unity gameplay/menu code.
>
> **Purpose:** Explain how DriveRight currently communicates with Supabase, what data it sends, where it is stored, how to configure and test the integration, and what a future maintainer must verify before changing or deploying it.
>
> **Important distinction:** Sections marked **Observed in code** describe the implementation found in the repository. Sections marked **Dashboard verification** or **Recommended improvement** are not claims about the current Supabase project; they are checks or changes a maintainer should perform.

## 1. Overview

DriveRight uses Supabase as a remote persistence layer for user records, device records, gameplay sessions, violation metadata and violation screenshots.

The Unity client does not use the Supabase .NET SDK. It sends HTTP requests directly using Unity's `UnityWebRequest` API and Supabase's REST endpoints.

The integration is implemented in:

| File | Responsibility |
|---|---|
| `Assets/Scripts/Supabase/SupabaseConfig.cs` | Supabase project URL and client API key |
| `Assets/Scripts/Supabase/SupabaseHttp.cs` | Shared POST and PATCH request helpers |
| `Assets/Scripts/Supabase/DeviceManager.cs` | Creates/loads a persistent local device UUID |
| `Assets/Scripts/Supabase/SupabaseDeviceService.cs` | Registers/updates device metadata |
| `Assets/Scripts/Supabase/SupabaseUserService.cs` | Fetches and creates user records |
| `Assets/Scripts/Supabase/SupabaseSessionService.cs` | Starts and ends gameplay sessions |
| `Assets/Scripts/Supabase/SupabaseStorageService.cs` | Uploads JPG screenshots to Storage |
| `Assets/Scripts/Supabase/SupabaseViolationService.cs` | Uploads evidence and inserts violation metadata |
| `Assets/Scripts/Supabase/DeviceData.cs` | Serializable device data model |

Related callers include the mobile start/menu flow, gameplay manager and level-completion flow. In particular, the mobile menu obtains users and selects/creates a user before starting a session; the gameplay flow reports violations; and the completion flow ends the session.

### What the integration currently does

1. Creates a device UUID once and stores it in Unity PlayerPrefs.
2. Sends a device record to the Supabase `devices` REST endpoint.
3. Retrieves user records for the selection UI.
4. Creates a user record when a new user is entered.
5. Creates a session record when a selected user starts a level.
6. On a violation, encodes a screenshot as JPEG, attempts to upload it to Supabase Storage, then prepares a violation record containing the screenshot URL.
7. At level completion, updates the session with its end timestamp and final score.

The client uses UTC ISO-8601 timestamps (the .NET round-trip `"o"` format) for `first_seen`, `last_seen`, `started_at`, and `ended_at`.

## 2. Supabase project configuration

### 2.1 Unity-side configuration

**Observed in code:** `SupabaseConfig.cs` defines two constants:

- `ProjectUrl`: the Supabase project base URL.
- `AnonKey`: despite the legacy variable name, the checked-in value uses the `sb_publishable_` key format.

The project URL and key are concatenated into REST and Storage request URLs/headers by the service classes.

To change projects, update the constants in `SupabaseConfig.cs` to the values for the target Supabase project. Do not copy a key from another environment (development, staging, production) accidentally.

**Do not commit credentials for a different project.** The current key is a publishable client key, not a service-role/secret key. A publishable key is expected to be visible in a distributed client application; it does not replace authorization. Never place a Supabase `service_role` or `sb_secret_` key in Unity, a mobile APK, a Quest build, source control, or a client-side configuration asset.

Recommended future cleanup: rename `AnonKey` to `PublishableKey` so the code matches the key format and current Supabase terminology. Update every reference at the same time.

### 2.2 Supabase dashboard values that must be checked

In the Supabase Dashboard for the intended project, verify:

1. The Project URL matches `SupabaseConfig.ProjectUrl`.
2. The key configured in Unity is the project's publishable key (or a legacy anon key if the project has not migrated).
3. The Data API is enabled for the tables accessed through `/rest/v1/`.
4. The tables and columns listed below exist with compatible names and types.
5. Database grants and RLS policies permit only the intended operations.
6. The Storage bucket named `violations-screenshots` exists.
7. The bucket's public/private setting matches the code's public-URL behavior.
8. Storage object policies permit the intended uploads.
9. The project is not relying on a service-role key in a client build.

The repository does not contain a complete export of the Supabase database schema, RLS policies, Storage policies, Auth settings or bucket configuration. These settings cannot be confirmed from Unity source code and must be inspected in the actual Supabase project.

## 3. HTTP communication layer

### 3.1 `SupabaseHttp.cs`

This class is a static helper for REST write operations. It exposes two coroutine methods:

- `Post(endpoint, json, callback)`
- `Patch(endpoint, json, callback)`

Both methods:

1. Build a URL by joining `SupabaseConfig.ProjectUrl` and the endpoint.
2. Create a `UnityWebRequest` using the HTTP method.
3. Encode the JSON body as UTF-8.
4. Attach an `UploadHandlerRaw` and `DownloadHandlerBuffer`.
5. Set `Content-Type: application/json`.
6. Set the `apikey` header to the configured publishable key.
7. Set `Authorization: Bearer <publishable-key>`.
8. Yield until the request completes.
9. Treat `UnityWebRequest.Result.Success` as success and invoke the callback with the success flag and response body.

The class currently does not expose a GET helper. `SupabaseUserService.FetchUsers` implements its own GET request.

### 3.2 POST behavior and upsert header

**Observed in code:** POST sets:

```http
Prefer: resolution=merge-duplicates
```

This asks PostgREST to merge on conflict, but a merge only occurs when the database has a matching unique constraint/index and the request's conflict target is appropriate. The device table's unique key/constraint must therefore be checked. If the desired behavior is explicitly an upsert on `device_id`, verify whether the endpoint should include `?on_conflict=device_id` and whether the database has a unique constraint on that column.

The current POST helper applies the merge-duplicates preference to every POST, including user, session and violation creation. That is broader than a purpose-built API design. Review this before changing database uniqueness constraints; otherwise duplicate submissions may be merged unexpectedly or inserts may fail.

### 3.3 PATCH behavior

PATCH sets:

```http
Prefer: return=minimal
```

This asks PostgREST not to return the updated row. Consequently, the callback response body may be empty even when the update succeeds. The code uses the HTTP result flag rather than parsing a returned row.

### 3.4 Error handling limitations

The helper passes the response body to the callback but does not currently log or expose all useful request diagnostics (HTTP status code, response headers, elapsed time, endpoint, retry count). There is no retry/backoff, timeout policy, cancellation flow, offline queue or durable request queue in this helper.

For debugging, inspect the Unity Console response body and the Supabase Logs/API logs. Do not log publishable keys, bearer headers, private user data or full screenshot payloads.

**Recommended improvement:** centralize GET/POST/PATCH in one request layer that records method, endpoint, status code, response body (with sensitive values redacted), and a request/correlation ID. Add bounded timeouts and retry only transient network/server failures. Do not automatically retry non-idempotent inserts unless they use stable IDs or an idempotency strategy.

## 4. Device identity and device registration

### 4.1 Local device identity — `DeviceManager.cs`

The app uses the PlayerPrefs key:

```text
DEVICE_ID
```

The `DeviceId` getter checks an in-memory cache first. If the cache is empty, it calls `LoadOrCreate()`:

- If PlayerPrefs already contains `DEVICE_ID`, that value is reused.
- Otherwise, a new GUID is generated.
- The GUID is saved with `PlayerPrefs.SetString` and `PlayerPrefs.Save`.

`EnsureExists()` simply forces the getter to run, ensuring the ID exists.

This is an application-generated identifier, not a hardware serial number. It normally remains stable across app launches while PlayerPrefs remains intact. Clearing app data or reinstalling may result in a new identifier, depending on platform backup/restore behavior.

### 4.2 Remote device registration — `SupabaseDeviceService.cs`

This is a `MonoBehaviour` singleton:

- The first instance becomes `Instance` and calls `DontDestroyOnLoad`.
- Duplicate instances are destroyed.
- During `Awake()`, it ensures the local ID exists and calls `WriteDevice()`.

The request posts to:

```http
POST /rest/v1/devices
```

Payload fields:

| Field | Value source | Meaning |
|---|---|---|
| `device_id` | `DeviceManager.DeviceId` | Locally generated persistent GUID |
| `platform` | `Application.platform.ToString()` | Unity runtime platform identifier |
| `first_seen` | `DateTime.UtcNow.ToString("o")` | Timestamp sent on this registration call |
| `last_seen` | `DateTime.UtcNow.ToString("o")` | Timestamp sent on this registration call |

**Important semantic issue:** both `first_seen` and `last_seen` are set to the current time every time `WriteDevice()` runs. If the POST is an upsert, this may overwrite the original `first_seen` value on subsequent launches. If `first_seen` is intended to mean the first-ever registration time, preserve it on conflict and update only `last_seen` (for example, with a dedicated PATCH/upsert strategy or a database-side function).

The callback logs a success or failure message. The device registration is asynchronous; the app does not wait for it before continuing.

## 5. User records

### 5.1 User model

`SupabaseUserService.User` contains:

| Field | Type in Unity model | Purpose |
|---|---|---|
| `id` | string | GUID generated by the client |
| `display_name` | string | User-entered display name |
| `email` | string | User-entered email |

The model is marked `[Serializable]` so Unity's `JsonUtility` can serialize it.

### 5.2 Fetch users

Method: `FetchUsers(Action<List<User>> callback)`

Request:

```http
GET /rest/v1/users?select=id,display_name,email&order=created_at.asc
```

Headers:

- `apikey`: configured publishable key
- `Authorization`: Bearer publishable key

Supabase returns a JSON array. Unity's `JsonUtility` expects a serializable object wrapper for this use case, so the implementation wraps the array as:

```json
{"items":[ ... ]}
```

It then deserializes the wrapper and returns `wrapper.items`.

If the request fails, the method logs the response and calls back with an empty list. An empty list therefore has two possible meanings: no users exist, or the request failed. The current callback shape does not distinguish those cases. Consider changing it to return `success`, `users`, and `error` separately.

### 5.3 Create user

Method: `CreateUser(string name, string email, Action<bool, User> callback)`

Validation currently checks only that name and email are non-empty. It does not perform email-format validation or check for duplicate email addresses locally.

The client generates a GUID and posts the serialized `User` object to:

```http
POST /rest/v1/users
```

If the request succeeds, the callback returns the locally constructed payload as the created user. Because the POST helper does not request a representation, the returned object is not a database-returned row; database-generated/default/trigger-modified values are not read back.

**Dashboard verification:** check whether `users.id` is a UUID primary key, whether `created_at` exists and has a default, whether email uniqueness is enforced, and which roles may select/insert users.

### 5.4 Identity and privacy distinction

The code shown uses a user table and a selected `UserSession.CurrentUserId`. It does not, by itself, demonstrate Supabase Auth sign-in or prove that a row in `public.users` is linked to `auth.users`.

Do not describe the app's selected user as an authenticated Supabase Auth user unless a separate Auth flow is implemented and verified. The configured publishable key identifies the application/project; it does not authenticate a person.

Because the current client fetches the user list and creates user rows directly, RLS and least-privilege policies are especially important. A public client must not be able to enumerate or alter all user data unless that is an intentional and approved product requirement.

## 6. Gameplay sessions

### 6.1 Singleton and active session ID

`SupabaseSessionService` is a persistent singleton. It stores:

- `currentSessionId`
- `CurrentSessionId` read-only property exposing the current value

The current session ID is held in memory; this class does not persist it to PlayerPrefs.

### 6.2 Starting a session

Method:

```csharp
StartSession(string level, string language, bool isVR)
```

Before sending a request, it checks that `UserSession.CurrentUserId` is not empty. If no user is selected, it logs an error and returns without creating a session.

It creates a GUID for the session and builds this payload:

| Field | Source | Description |
|---|---|---|
| `id` | `Guid.NewGuid()` | Client-generated session UUID |
| `device_id` | `DeviceManager.DeviceId` | Device running the app |
| `user_id` | `UserSession.CurrentUserId` | Selected application user |
| `level` | Method argument | Level/mode label passed by caller |
| `language` | Method argument | Language passed by caller |
| `is_vr` | Method argument | Whether caller classifies this run as VR |
| `started_at` | UTC round-trip timestamp | Session start time |

Request:

```http
POST /rest/v1/sessions
```

The code assigns `currentSessionId = payload.id` before the network request completes. The session ID is therefore available immediately to later gameplay code, even if the remote insert subsequently fails.

**Important reliability issue:** the scene/session flow is asynchronous. A caller may load the gameplay scene while the session insert is still in flight. If the insert fails, later violation records or end-session updates may reference a session that does not exist remotely. A robust design should await/confirm session creation before treating the session as remotely active, or queue dependent events until creation succeeds.

### 6.3 Ending a session

Method:

```csharp
EndSession(int finalScore)
```

If `currentSessionId` is empty, it logs an error and returns.

Otherwise it sends a PATCH to:

```http
PATCH /rest/v1/sessions?id=eq.<currentSessionId>
```

Payload:

| Field | Value |
|---|---|
| `ended_at` | Current UTC timestamp in `"o"` format |
| `final_score` | Score supplied by the caller |

The PATCH uses `return=minimal`, so the client does not read the updated row. The method does not clear `currentSessionId` after completion, and there is no explicit guard against ending the same session multiple times. Review those semantics if sessions can be retried or if a user can restart without returning through the expected menu path.

## 7. Violation reporting and screenshot storage

### 7.1 Call sequence

`SupabaseViolationService.LogViolation(...)` accepts:

- violation number
- message
- score delta
- screenshot `Texture2D`

The intended sequence is:

1. Confirm `SupabaseSessionService.Instance` exists.
2. Read the current session ID.
3. Encode the screenshot as JPEG at quality 60.
4. Destroy the passed `Texture2D`.
5. Upload the JPEG to Supabase Storage.
6. If upload succeeds, use the returned public object URL; if upload fails, log the error and set screenshot URL to null.
7. Insert a violation metadata record referencing the session and screenshot URL.

### 7.2 Screenshot encoding

The current implementation uses:

```csharp
byte[] jpgBytes = screenshot.EncodeToJPG(60);
Destroy(screenshot);
```

The PNG alternative is commented out. JPEG quality 60 reduces file size compared with a lossless PNG, but screenshot readability should be checked against the actual violation evidence. If text/small indicators need to remain legible, compare quality settings and file sizes on target devices.

The caller transfers ownership of the screenshot texture to this method because the method destroys it after encoding. Do not pass a texture that another component still needs.

### 7.3 Storage bucket and object naming

**Observed in code:**

- Bucket constant: `violations-screenshots`
- Upload endpoint pattern:

```text
/storage/v1/object/violations-screenshots/session_<sessionId>/event_<violationNumber>.jpg
```

Example object path (illustrative):

```text
session_6f.../event_1.jpg
```

The content type is `image/jpeg`. The request uses POST with the publishable key in both `apikey` and Bearer Authorization headers.

After a successful upload, the code constructs a public URL using:

```text
/storage/v1/object/public/violations-screenshots/<object-path>
```

It does not request a signed URL. This means the current implementation expects the object to be retrievable through the public-object URL. **Dashboard verification:** check whether the bucket is public. If the bucket is private, this constructed URL will not provide public access; use authenticated downloads or signed URLs instead.

### 7.4 Violation row payload

The intended JSON fields are:

| Field | Meaning |
|---|---|
| `id` | Client-generated GUID |
| `session_id` | Current session UUID |
| `violation_number` | Violation sequence number within the run |
| `message` | Human-readable violation description |
| `delta_score` | Score change associated with the violation |
| `screenshot_url` | Public screenshot URL, or null if upload failed |

The endpoint is:

```http
POST /rest/v1/violations
```

A screenshot upload failure does not prevent the code from attempting to create the violation row; it sets `screenshot_url` to null. This is a useful fallback in principle, but the insert implementation has a critical issue described next.

### 7.5 Critical implementation issues to fix

The current source contains two problems in the violation insert path that should be addressed before relying on cloud violation records:

**Issue A — anonymous object serialization.** The code builds the violation payload with an anonymous C# object and passes it to `JsonUtility.ToJson(payload)`. Unity's `JsonUtility` is designed around supported serializable classes/structs and does not reliably serialize anonymous objects. The resulting JSON may be empty or missing the expected fields. Replace it with a dedicated `[Serializable]` class with public fields (for example, `ViolationPayload`).

**Issue B — the POST coroutine is not started.** Inside the screenshot-upload callback, the code calls `SupabaseHttp.Post(...)` but does not start or yield that returned `IEnumerator`. Returning an IEnumerator does not execute a Unity coroutine. The violation insert request therefore will not run through the normal coroutine mechanism. Start it through a valid MonoBehaviour coroutine runner, or refactor the HTTP layer to use an async/task pattern consistently.

These are source-level findings, not guesses about the Supabase dashboard. Test the complete flow after fixing them: upload object, verify object URL, then verify the corresponding row appears in `violations`.

### 7.6 Object overwrite and uniqueness

The object name is deterministic for a given session and violation number. If the same violation number is uploaded again for the same session, it targets the same path. The current upload request does not set an explicit upsert header. Confirm the intended duplicate behavior: reject duplicate object names, overwrite existing evidence, or generate a unique object name and store it in the row.

## 8. Database model / relationship map

The following is the logical model implied by the client payloads. It is **not a verified schema dump**. Confirm exact types, constraints, defaults, nullability and foreign keys in the Supabase Table Editor or SQL schema before using it for migrations.

```text
users
  id (UUID, client-generated)
  display_name
  email
  created_at (queried by client; expected to exist)

devices
  device_id (UUID-like string, generated and persisted locally)
  platform
  first_seen
  last_seen

sessions
  id (UUID, client-generated)
  device_id  -> devices.device_id (logical relationship)
  user_id    -> users.id (logical relationship)
  level
  language
  is_vr
  started_at
  ended_at
  final_score

violations
  id (UUID, client-generated)
  session_id -> sessions.id (logical relationship)
  violation_number
  message
  delta_score
  screenshot_url
```

Relationship summary:

- One application user may have many sessions.
- One device may have many sessions.
- One session may have many violation records.
- Each violation may point to one screenshot object URL.

Potential constraints to verify:

- Primary key on each `id` field.
- Unique constraint on `devices.device_id`.
- Foreign key `sessions.user_id -> users.id`.
- Foreign key `sessions.device_id -> devices.device_id`.
- Foreign key `violations.session_id -> sessions.id`.
- Indexes on `sessions.user_id`, `sessions.device_id`, `violations.session_id`, and any fields used for filtering/reporting.
- A uniqueness rule for `(session_id, violation_number)` if violation numbering is unique within a session.
- A default timestamp for `users.created_at` if the UI relies on ordering by it.

## 9. Security model and RLS

### 9.1 What the Unity client sends

The client sends the publishable key as both `apikey` and Bearer token. This is expected for a public client integration. It is not a user login token and does not establish which human is using the app.

All authorization must therefore be enforced by Postgres grants/RLS and Storage object policies. Never rely on hiding the project URL or publishable key in a Unity build.

### 9.2 Tables to inspect

For each of `users`, `devices`, `sessions`, and `violations`, inspect:

- Is RLS enabled?
- Which roles have table grants?
- Which SELECT/INSERT/UPDATE/DELETE policies exist?
- Can a public unauthenticated client read all users?
- Can a client create arbitrary users or sessions?
- Can a client update sessions belonging to another device/user?
- Can a client insert violations for a session it does not own?
- Are there column-level or server-side validations where needed?

The current code's lack of a visible Supabase Auth user session means policies based on `auth.uid()` will not automatically identify the selected row in `public.users`. If per-user authorization is required, implement a real authentication/authorization design rather than trusting a client-supplied `user_id`.

### 9.3 Storage policies

Supabase Storage uses policies on `storage.objects`. The app uploads to the `violations-screenshots` bucket. Verify that the client role has only the necessary insert permission and that the policy restricts the bucket and object paths as tightly as the identity model allows.

The code constructs a public URL. Public buckets allow public retrieval by URL; this is convenient but means anyone with the URL can potentially view the evidence. If screenshots may contain personal or sensitive information, consider a private bucket with authenticated access or short-lived signed URLs, and change the client accordingly.

Do not solve upload errors by making the entire Storage schema or every bucket publicly writable.

### 9.4 Key handling

- Publishable key: acceptable in a mobile/VR client when RLS and grants are correctly configured.
- Secret/service-role key: server-only; never ship in Unity.
- If a secret key has ever been committed or included in a build, rotate it immediately and review access logs.
- A publishable key is not a password, but still avoid unnecessarily reproducing it in documentation, screenshots or issue reports.

## 10. Reproducing the integration in a fresh environment

This is a checklist for a developer who has access to the intended Supabase project.

1. Open the correct Supabase project.
2. Copy its Project URL and publishable key.
3. Update `SupabaseConfig.cs` in the Unity source branch.
4. Confirm the REST Data API is enabled.
5. Confirm the four expected tables exist and match the field names/payloads in this document.
6. Confirm `users.created_at` exists because the fetch query orders by it.
7. Confirm key constraints and foreign keys.
8. Enable RLS on exposed tables and configure explicit least-privilege grants/policies.
9. Create or verify the `violations-screenshots` Storage bucket.
10. Configure Storage object policies for the intended upload and read behavior.
11. Decide whether screenshots are public or private. If private, replace the current public URL strategy.
12. Build and run the app on a supported target.
13. Create/select a user and verify that it appears in `users`.
14. Start a session and verify its row in `sessions`.
15. Trigger a test violation and verify both the Storage object and `violations` row.
16. Finish the level and verify `ended_at` and `final_score` update.
17. Repeat with network disabled, invalid policy, and upload failure to confirm graceful failure behavior.

Do not use production data for destructive tests. Prefer a separate development Supabase project and test users.

## 11. End-to-end test matrix

| Test | Expected client behavior | Expected Supabase result |
|---|---|---|
| First launch on clean app data | Generates and saves a device GUID | Device row created |
| Relaunch | Reuses saved GUID | Same device row updated/upserted |
| Fetch users | Populates user selection list | GET returns permitted user rows |
| Fetch users with no rows | Empty list | Empty array |
| Fetch users with denied access/network off | Logs failure and returns empty list (current behavior) | No successful query |
| Create user | Callback reports success/failure | User row inserted on success |
| Start with no selected user | Logs and does not send request | No session row |
| Start with selected user | Generates session ID and sends POST | Session row inserted |
| Trigger violation | Encodes JPG and attempts upload | Object stored in bucket |
| Violation insert | Stores session/score/message/screenshot URL | Violation row inserted |
| Screenshot upload fails | Attempts violation row with null screenshot URL | Violation row only if insert is allowed and working |
| Complete level | Sends PATCH | Session end time and final score updated |
| App offline during session | Current code logs request failure; no durable retry queue | Some events may be missing |

The violation test is not expected to pass reliably until the serialization and coroutine issues in §7.5 are fixed.

## 12. Troubleshooting

### 401 / 403 response

Check that the Project URL and publishable key belong to the same project. Then inspect grants and RLS policies. A valid publishable key does not grant unrestricted access.

### 404 or endpoint/table not found

Check the REST endpoint spelling, table name, Data API exposure and schema. The current client assumes the tables are in the `public` schema and calls `/rest/v1/<table>`.

### Insert fails with a permission error

Check table grants first, then the applicable INSERT RLS policy. Also check required columns, foreign keys, NOT NULL constraints and data types.

### Upsert does not update the device row

Check the unique constraint and the `on_conflict` target. The current request sets `resolution=merge-duplicates`, but does not explicitly specify a conflict target in the URL.

### User list is always empty

The current code returns an empty list both when there are no users and when the request fails. Inspect the Console error and Supabase API logs; improve the callback contract so failure is distinguishable from an empty successful result.

### Screenshot upload fails

Check that the bucket exists, the object path is valid, the request has the expected content type, and Storage RLS permits INSERT for this client. Confirm whether the bucket is public/private and whether the request's Authorization identity is appropriate.

### Screenshot uploads but no violation row appears

Inspect §7.5. The current code's anonymous-object serialization and missing coroutine start can prevent the row POST from running correctly.

### Session end does not update

Confirm that the current session ID matches a row in `sessions`, that the PATCH filter uses the correct UUID, and that UPDATE grants/RLS allow the operation.

## 13. Recommended implementation improvements

Prioritized recommendations for a reliable handover/production implementation:

1. **Fix violation serialization and coroutine execution** (critical; see §7.5).
2. **Make session creation awaitable.** Do not emit dependent violation rows until the session insert is confirmed or queued.
3. **Use a dedicated serializable payload class for every request.** Avoid anonymous objects with `JsonUtility`.
4. **Separate POST insert and upsert behavior.** Only use merge-duplicates where a stable unique key and explicit conflict semantics are intended.
5. **Preserve `first_seen`.** Update `last_seen` without resetting the original first-seen timestamp.
6. **Return structured errors.** Distinguish empty data from request failure.
7. **Add a GET method to `SupabaseHttp`.** Keep header construction, timeout, logging and error handling consistent.
8. **Add retry/offline queuing for important events.** Store unsent session/violation events locally and retry safely with stable IDs.
9. **Define screenshot privacy.** Choose public bucket vs private bucket/signed URLs based on the sensitivity of captured screens.
10. **Use a real user identity model if user-level isolation is required.** A selected row in `public.users` is not equivalent to Supabase Auth.
11. **Move schema and RLS policies into migrations.** This makes a fresh environment reproducible and reviewable.
12. **Add integration tests.** Test allowed and denied table/storage operations, not only the happy path.
13. **Avoid logging personal data.** Log IDs/status codes and redact emails, tokens and request bodies where possible.

## 14. Suggested future payload classes

These are design examples, not existing source files. They illustrate the shape of serializable models that are easier to maintain with Unity `JsonUtility`.

```csharp
[System.Serializable]
public class ViolationPayload
{
    public string id;
    public string session_id;
    public int violation_number;
    public string message;
    public int delta_score;
    public string screenshot_url;
}
```

Similarly, keep separate serializable classes for device, session-start and session-end payloads. This makes the JSON contract explicit and easier to compare against the database schema.

## 15. Source files to open when maintaining Supabase

Start with these files in this order:

1. `Assets/Scripts/Supabase/SupabaseConfig.cs` — target project and publishable key.
2. `Assets/Scripts/Supabase/SupabaseHttp.cs` — shared request behavior and headers.
3. `Assets/Scripts/Supabase/DeviceManager.cs` — local device identity.
4. `Assets/Scripts/Supabase/SupabaseDeviceService.cs` — device registration.
5. `Assets/Scripts/Supabase/SupabaseUserService.cs` — user retrieval and creation.
6. `Assets/Scripts/Supabase/SupabaseSessionService.cs` — session lifecycle.
7. `Assets/Scripts/Supabase/SupabaseStorageService.cs` — screenshot upload and public URL.
8. `Assets/Scripts/Supabase/SupabaseViolationService.cs` — violation pipeline.
9. `Assets/Scripts/Supabase/DeviceData.cs` — device data model.
10. The mobile menu/gameplay/level-completion callers — determine when these services are invoked and which values are passed.

When changing a field name, update all three sides together: Unity payload class, database column/schema, and RLS/constraints or server-side logic that depends on the field.

## 16. External references

Use the official Supabase documentation for current dashboard/API behavior:

- [API keys](https://supabase.com/docs/guides/getting-started/api-keys)
- [Securing the Data API](https://supabase.com/docs/guides/api/securing-your-api)
- [Postgres Row Level Security](https://supabase.com/docs/guides/database/postgres/row-level-security)
- [Storage access control](https://supabase.com/docs/guides/storage/security/access-control)

Supabase changes its platform guidance over time. Re-check the current official docs when configuring a new project, especially around publishable/secret key migration, RLS, and Storage access.
