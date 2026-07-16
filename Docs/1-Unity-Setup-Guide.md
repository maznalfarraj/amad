# SYRAX — Unity Setup Guide

## Requirements
- Unity project: `amad` (this repo). Open scene **`Assets/Scenes/AmadMazen.unity`** — the ONLY scene.
- SYRAX backend running: `node server.js` in the backend repo (port 3000).
- Piper TTS server running on port 5000: double-click **`C:\Users\imzn7\syrax-piper\start_piper.bat`**
  (Python server, English + Arabic voices, auto-picks the voice from the text language;
  health check: `http://localhost:5000/health`).
- Vosk model zip lives in `Assets/StreamingAssets/vosk-model-small-en-us-0.15.zip` (already placed).

## First-time setup checklist
1. Open the `AmadMazen` scene.
2. Menu **SYRAX → Select Config** → set `Backend Base Url`:
   - Editor testing: `http://localhost:3000`
   - Quest 3 on Wi-Fi: `http://<PC-LAN-IP>:3000` (PC and headset on the same network; allow Node through Windows Firewall).
   - Same for `Piper Synthesize Url`.
3. Menu **SYRAX → Backend Health Check** → console must print `Backend ONLINE`.
4. Menu **SYRAX → Validate Scene** → must report no issues.
5. Create a session from the website questionnaire (`POST /api/analyze`), then press Play.

## Testing on PC (no headset)
Answering the phone normally requires an XR controller hover — impossible with
a mouse. `SYRAX SYSTEM/Debug Hotkeys` (editor + development builds only)
simulates it. With the **Game view focused**:

| Key | Action |
|---|---|
| **Space** | Answer the ringing phone (simulates the XR hover → phone UI appears) |
| **A** | Press the "Answer Call" button (starts the live AI conversation; speak into your PC mic) |
| **1** | Make the SAFE decision for the current stage |
| **2** | Make the UNSAFE decision for the current stage |
| **H** | Print help + current stage to the console |

Full PC smoke test: website "Begin Experience" → Play → Space → A → talk (or 1/2)
→ 1 → 1 → completion panel. This component compiles out of release builds.

## Build (Meta Quest 3)

**One-time prerequisite (NOT yet done):** this editor is missing the Android
module. In **Unity Hub → Installs → (this Unity version) → Add Modules →
check "Android Build Support" + "Android SDK & NDK Tools" + "OpenJDK"** →
install → restart the editor. Then File > Build Settings > Android >
**Switch Platform** (one-time re-import, can take a while).

**Before every build, run `SYRAX → Validate Quest Build`** — it checks all of
the following automatically:
- Platform = Android, IL2CPP, ARM64 ✅ (already configured)
- AmadMazen is the only enabled scene in Build Settings ✅ (already set —
  the old `Amad.unity` was first in the list and has been disabled)
- OpenXR + Meta Quest Support feature ✅ (already enabled)
- Vosk model in StreamingAssets ✅
- SyraxConfig URLs are a LAN IP, not localhost ✅ (currently
  `http://192.168.8.175:3000` — re-check on the demo network with `ipconfig`)

Quest-specific behavior already handled in code:
- **Mic permission**: `SyraxPermissions` (on Vosk System) requests RECORD_AUDIO
  on first launch and starts speech recognition only after it is granted.
- **Mic device**: the PC uses mic index 2 (A50 headset); on Quest the index is
  clamped to the only available device automatically.
- **Debug hotkeys**: compiled out of ALL Android builds (even development
  builds) — desktop/editor only.
- First run on headset: the Vosk model (~40 MB) unzips — expect a few seconds
  before STT is ready.
- Windows Firewall: when Windows asks, ALLOW Node.js and Python on private
  networks — otherwise the headset cannot reach the servers.

## Configuration (SyraxConfig asset)
Everything tunable lives in `Assets/Settings/SyraxConfig.asset` — no URLs or keys in code:
| Field | Meaning |
|---|---|
| Backend Base Url | SYRAX Node server, no trailing slash |
| Request Timeout Seconds | AI calls can take seconds; default 60 |
| Max Retries / Retry Base Delay | Auto-retry on network/5xx errors (exponential backoff) |
| Piper Synthesize Url / Length Scale | TTS server + speaking speed |
| Points Per Correct / Maximum Score | Display values (backend is authoritative) |
| Verbose Logging | Master switch for [SYRAX] console logs |
| Piper Startup Test | Opt-in "TTS ready" spoken test on scene start |
