# SYRAX — Script Overview (Assets/Scripts)

| Script | Role |
|---|---|
| **SyraxConfig.cs** | ScriptableObject with every URL/timeout/tuning value. Asset: `Assets/Settings/SyraxConfig.asset`. |
| **SyraxLogger.cs** | Central `[SYRAX]` logging; silenced via config `Verbose Logging`. Errors always log. |
| **SyraxExperienceManager.cs** | The core client. Loads the session (`GET /api/unity/session`), owns the fixed stage order, sends chat (`POST /api/chat`) and decisions (`POST /api/event`) with retry + backoff, plays transitions, shows completion. Also defines all backend DTOs (SyraxSession, ChatRequest/Response, BehaviorAnalysis, EventRequest/Response). |
| **PhoneCallStageController.cs** | Phone-call stage: answer/reject, AI dialogue loop (STT → chat → TTS), OTP decision. Consumes `ChatResponse.GetDialogue()` (new + legacy backend contracts). |
| **WebsiteStageController.cs** | Website stage: two links, official vs phishing decision, reaction time. |
| **MessageStageController.cs** | WhatsApp stage: chat window, call-mother vs transfer-money decision. |
| **PhoneInteraction.cs** | XR hover on the physical phone starts the answered call; ringtone + hologram + locomotion lock. |
| **VoskBridge.cs** | Gate between VoskSpeechToText and the manager (`StartAcceptingSpeech` / `StopAcceptingSpeech`). |
| **PiperTTSClient.cs** | Sends text to the Piper server, decodes WAV, plays it. Config-driven URL. Startup test speech is opt-in via config. |
| **WavUtility.cs** | WAV byte[] → AudioClip decoder used by Piper. |

## Key public surface (for programmers)
```csharp
// SyraxExperienceManager
event Action<ChatResponse> OnChatResponseReceived; // new AI reply arrived
event Action<bool>  OnBusyChanged;                 // request in flight → loading UI
event Action<string> OnRequestFailed;              // all retries failed → error UI

void SendUserSpeech(string text);                  // STT result → backend chat
void RejectUnknownCall(float rt); …                // decision methods per stage
string GetDashboardApiUrl();

// ChatResponse helpers
string GetDialogue();        // dialogue (new) or reply_text (legacy)
bool IsConversationOver();   // conversationState == "End" (or legacy end)
response.analysis            // BehaviorAnalysis: trustLevel, riskScore, notes[]…
```

## Retry / error behavior
- Session + chat: up to `Max Retries` retries with exponential backoff on
  network errors and 5xx. 4xx fails immediately (a client bug — check logs).
- Decision events: 4-second timeout, then the experience **continues locally**
  (deliberate: the VR demo never blocks on the network).
