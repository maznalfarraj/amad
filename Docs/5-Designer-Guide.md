# SYRAX — Designer Guide

**You never need to write code, connect APIs, or assign scripts.**
Everything technical is wired. Your work is purely visual/audio.

## What you CAN change freely
### UI & layout
- Everything inside `SYRAX SYSTEM/Phone`, `SYRAX SYSTEM/website`,
  `SYRAX SYSTEM/message`, `completion panel`, `Transition`:
  move, resize, recolor, restyle, swap fonts (TMP), replace sprites and icons.
- **Rule: restyle or reparent visuals, but do not DELETE objects that have a
  script reference** (buttons, text fields, windows). If unsure, rename —
  never delete.

### Audio
- Ringtone: `smart_phone → PhoneInteraction → Ringtone Audio Source → AudioClip`.
- Website alert + WhatsApp notification: `SyraxExperienceManager → Stage Audio` clips.
- AI voice volume/3D settings: `Piper System → AudioSource`.

### Models & environment
- The apartment (`open_concept_apartment_unity`), lighting, reflection probes.
- The phone model under `smart_phone` (keep the collider + XRSimpleInteractable).
- The hologram visual.

### Feel
- Transition timing: `SyraxExperienceManager → Fade Duration / Hold / Entrance`.
- Speaking speed: `Assets/Settings/SyraxConfig → Piper Length Scale`.

## Arabic text — how it works
- **Runtime texts** (AI dialogue, WhatsApp message, OTP, transitions,
  completion) are shaped automatically — you do nothing.
- **Static texts you type in the editor**: TMP shows raw Arabic disconnected.
  Either type the text through an Arabic fixer tool first, use an image, or
  ask a programmer to route it through `SyraxArabicText.Fix()`.
- **Font**: `Assets/Fonts/Tahoma SDF` is the global Arabic fallback — every
  TMP text can render Arabic without changing its font. Want a prettier
  Arabic font (Cairo, Noto Naskh…)? Import the .ttf, create a TMP Font Asset
  (dynamic), and add it to TMP Settings > Fallback Font Assets — or swap it
  on specific texts directly.
- Prefer **right alignment** on Arabic text fields for a natural look.

## What you MUST NOT touch
- `SYRAX SYSTEM/SyraxExperienceManager`, `Vosk System`, `Piper System` components.
- Any field marked "no" in `4-Inspector-Documentation.md`.
- `Assets/StreamingAssets/` (the speech model lives there).
- Scene name/location — everything stays in `AmadMazen.unity`.

## After every editing session — 10-second self-check
1. Menu **SYRAX → Validate Scene** → Console must say "All good".
2. If it lists NULL REFs: menu **SYRAX → Auto Assign References**, validate again.
   Still red? Undo your last change or ask a programmer.
3. Press Play with the backend running (**SYRAX → Backend Health Check** first):
   phone should ring → answer → AI speaks.

## Where things are
| Thing | Path |
|---|---|
| The only scene | `Assets/Scenes/AmadMazen.unity` |
| All settings | `Assets/Settings/SyraxConfig.asset` (menu SYRAX → Select Config) |
| Docs | `Docs/` folder in the project root |
