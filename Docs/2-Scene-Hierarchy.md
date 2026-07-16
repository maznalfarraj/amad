# SYRAX — Scene Hierarchy (AmadMazen)

```
AmadMazen.unity
├── Global Volume                  (URP post-processing)
├── Directional Light / area_light (lighting)
├── open_concept_apartment_unity   (the environment — 172 children)
├── Reflection Probe (x2)
├── XR Origin (XR Rig)             (player: XROrigin, CharacterController,
│                                   InputActionManager, XRInputModalityManager)
├── EventSystem                    (InputSystemUIInputModule)
└── SYRAX SYSTEM                   ← everything gameplay lives here
    ├── SyraxExperienceManager     [SyraxExperienceManager] the brain-client
    ├── smart_phone                [PhoneInteraction, XRSimpleInteractable, Rigidbody]
    │                               physical phone the player grabs/hovers
    ├── Phone                      Phone-call stage UI (PhoneCallStageController)
    ├── website                    Website stage UI (WebsiteStageController)
    ├── message                    WhatsApp stage UI (MessageStageController)
    ├── completion panel           [Canvas] end-of-experience panel
    ├── Transition                 [Canvas + CanvasGroup] fade between stages
    ├── Vosk System                [VoskBridge, VoskSpeechToText, VoiceProcessor] STT
    └── Piper System               [PiperTTSClient, AudioSource] TTS
```

## Flow ownership
- **Unity owns the scenario order**: PhoneCall → Website → Message → Completion
  (`fixedStageOrder` in SyraxExperienceManager).
- Stage GameObjects are enabled/disabled by the manager; only one stage is
  active at a time. The Transition canvas fades between them.

## Rules
- Do NOT create a second manager or duplicate any system.
- New UI goes INSIDE the existing stage objects (Phone/website/message).
- Everything gameplay-related stays under SYRAX SYSTEM.
