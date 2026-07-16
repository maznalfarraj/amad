using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Desktop testing helper — lets you drive the whole SYRAX flow on PC
/// without a headset. Compiled ONLY into the editor and development builds;
/// it does nothing in release builds.
///
/// Keys (Game view must have focus):
///   Space  — answer the ringing phone (simulates the XR hover)
///   1      — make the SAFE decision for the current stage
///   2      — make the UNSAFE decision for the current stage
///   H      — print this help to the console
/// </summary>
public class SyraxDebugHotkeys : MonoBehaviour
{
    [Header("References (auto-assigned by SYRAX/Auto Assign References)")]
    [SerializeField] private SyraxExperienceManager manager;
    [SerializeField] private PhoneInteraction phoneInteraction;
    [SerializeField] private PhoneCallStageController phoneCallStageController;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Start()
    {
        SyraxLogger.Log(
            "Debug hotkeys active — Space: answer phone | 1: safe decision | 2: unsafe decision | H: help"
        );
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.spaceKey.wasPressedThisFrame) AnswerPhone();
        else if (kb.aKey.wasPressedThisFrame) StartCall();
        else if (kb.digit1Key.wasPressedThisFrame) MakeDecision(true);
        else if (kb.digit2Key.wasPressedThisFrame) MakeDecision(false);
        else if (kb.hKey.wasPressedThisFrame) PrintHelp();
#else
        if (Input.GetKeyDown(KeyCode.Space)) AnswerPhone();
        else if (Input.GetKeyDown(KeyCode.A)) StartCall();
        else if (Input.GetKeyDown(KeyCode.Alpha1)) MakeDecision(true);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) MakeDecision(false);
        else if (Input.GetKeyDown(KeyCode.H)) PrintHelp();
#endif
    }

    private void AnswerPhone()
    {
        if (phoneInteraction == null)
        {
            SyraxLogger.Warn("Debug: PhoneInteraction not assigned.");
            return;
        }

        SyraxLogger.Log("Debug: simulating phone answer (XR hover).");
        phoneInteraction.AnswerNow();
    }

    private void StartCall()
    {
        if (phoneCallStageController == null)
        {
            SyraxLogger.Warn("Debug: PhoneCallStageController not assigned.");
            return;
        }

        SyraxLogger.Log("Debug: pressing the Answer Call button.");
        phoneCallStageController.AnswerCall();
    }


    private void MakeDecision(bool safe)
    {
        if (manager == null)
        {
            SyraxLogger.Warn("Debug: manager not assigned.");
            return;
        }

        string stage = manager.CurrentStageType;

        SyraxLogger.Log(
            $"Debug: forcing {(safe ? "SAFE" : "UNSAFE")} decision for stage '{stage}'."
        );

        const float debugReactionTime = 5f;

        switch (stage)
        {
            case "PhoneCall":
                if (safe) manager.EndCallWithoutSharingOtp(debugReactionTime);
                else manager.ShareOtp(debugReactionTime);
                break;

            case "Website":
                if (safe) manager.SelectOfficialBankLink(debugReactionTime);
                else manager.SelectPhishingLink(debugReactionTime);
                break;

            case "Message":
                if (safe) manager.CallMotherToVerify(debugReactionTime);
                else manager.TransferMoney(debugReactionTime);
                break;

            default:
                SyraxLogger.Warn("Debug: experience already finished.");
                break;
        }
    }

    private void PrintHelp()
    {
        SyraxLogger.Log(
            "Debug hotkeys — Space: answer phone | 1: safe decision | 2: unsafe decision. " +
            $"Current stage: '{(manager != null ? manager.CurrentStageType : "?")}'."
        );
    }
#endif
}
