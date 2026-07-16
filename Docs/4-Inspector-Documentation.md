# SYRAX — Inspector Documentation

Every SYRAX component's serialized fields, what they do, and whether a
designer may touch them. **All references are already assigned — run
SYRAX → Validate Scene after any change.**

## SyraxExperienceManager (SYRAX SYSTEM/SyraxExperienceManager)
| Field | What it is | Designer? |
|---|---|---|
| Config | The SyraxConfig asset | leave assigned |
| Server Base Url | legacy fallback URL (config wins) | no |
| Phone Call / Website / Message Object | the three stage roots | no |
| Phone Interaction | physical-phone script | no |
| Stage Audio Source + Website Alert / Message Notification Sound | stage SFX | **yes — replace AudioClips** |
| Completion Panel / Message Text / Final Score Text | end panel refs | restyle freely, keep refs |
| Transition Canvas Group / Title Text | fade overlay | restyle freely, keep refs |
| Fade / Hold / Entrance durations | transition timing | **yes — tune** |

## PhoneInteraction (SYRAX SYSTEM/smart_phone)
| Field | What it is | Designer? |
|---|---|---|
| Manager / Phone Interactable | wiring | no |
| Hologram | attention visual on the ringing phone | **yes — replace model/FX** |
| Ringtone Audio Source | ringtone | **yes — replace clip** |
| Locomotion Objects | disabled while on the call | no |

## Stage controllers (Phone / website / message objects)
- All Window/Button/Text references: keep assigned; **restyle the visuals freely**
  (sprites, fonts, colors, layout).
- **WebsiteStageController (static, no AI)** — two designer-owned buttons:
  `Official Link Button` (real bank link) and `Phishing Link Button`
  (the look-alike). Build any message visuals you want, then either drag
  your buttons into these two fields, or wire your Button's OnClick to
  `OnOfficialLinkClicked()` / `OnPhishingLinkClicked()` on the
  "Website Stage" object. Correct pick = +points; phishing pick = 0 points
  and the mistake is flagged in the backend. Both advance WITHOUT showing
  the result.
- MessageStageController → `On Speak Message` UnityEvent: feeds TTS; leave wired.
- **MessageStageController — Designer Events (hook anything, no code):**
  - `On Call Mother Chosen`: fires when the player calls to verify — hook the
    mother's real voice line ("لا، أنا ما طلبت أي فلوس!"), animations, FX.
    (The built-in `Mother Audio Source` clip also still plays if assigned.)
  - `On Verified Call Finished`: fires when the verification call ends,
    right before the experience advances.
  - `On Transfer Money Chosen`: fires when the player transfers WITHOUT
    verifying — hook alarm/regret effects or a custom ending sequence.
    `Transfer Effect Duration` = seconds to wait after this event before
    the experience advances (give your effects time to play).
- **PhoneCallStageController — OTP message (designers own the container):**
  - The verification code is RANDOM each run (`Otp Digit Count`, 4–6 digits).
  - The OTP notification arrives the moment the AI asks for the code
    (keyword detection on the dialogue), or after `Otp Delay` seconds as a
    fallback.
  - `Otp Message Format`: the notification text; `{0}` is replaced by the
    code. Edit freely (designers).
  - `Otp Window` / `Otp Text`: the visual container — restyle freely, keep
    the references assigned.
  - If the player SPEAKS the code aloud (digits or number words, AR/EN),
    it is detected locally and recorded as the unsafe "shared OTP" decision.

## Vosk System
| Field | Designer? |
|---|---|
| Model Path (`vosk-model-small-en-us-0.15.zip`) | no — must match the zip in StreamingAssets |
| Vosk Bridge / manager refs | no |

## Piper System (PiperTTSClient)
| Field | Designer? |
|---|---|
| Config | leave assigned |
| Output Audio Source | **yes — tune volume/spatial settings** |
| Piper Server Url / Length Scale | via SyraxConfig only |

## SyraxConfig asset (Assets/Settings/SyraxConfig.asset)
See `1-Unity-Setup-Guide.md` — URLs, timeouts, retries, scoring display,
logging, TTS test toggle. This is the ONLY place URLs live.
