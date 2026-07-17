using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhoneCallStageController : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField]
    private SyraxExperienceManager manager;

    [SerializeField]
    private PiperTTSClient piperTTSClient;

    [SerializeField]
    private VoskBridge voskBridge;

    [Header("Windows")]
    [SerializeField]
    private GameObject incomingCallWindow;

    [SerializeField]
    private GameObject activeCallWindow;

    [SerializeField]
    private GameObject beforeOtpWindow;

    [SerializeField]
    private GameObject afterOtpWindow;

    [SerializeField]
    private GameObject otpWindow;

    [Header("Texts")]
    [SerializeField]
    private TMP_Text incomingDialogueText;

    [SerializeField]
    private TMP_Text activeDialogueText;
[SerializeField]
private TMP_Text beforeOtpTimerText;

[SerializeField]
private TMP_Text afterOtpTimerText;

    [SerializeField]
    private TMP_Text otpText;

    [Header("Buttons")]
    [SerializeField]
    private Button answerButton;

    [SerializeField]
    private Button rejectButton;

    [SerializeField]
    private Button endCallButton;

    [Header("OTP Timing")]
    [Tooltip("Fallback: the OTP message arrives after this delay even if the AI never explicitly asks for the code.")]
    [SerializeField]
    private float otpDelay = 6f;

    [SerializeField]
    private float otpVisibleDuration = 4f;

    [Header("OTP Message (designers style the container via otpWindow/otpText)")]
    [Tooltip("The OTP notification text. {0} is replaced by the generated code.")]
    [SerializeField, TextArea]
    private string otpMessageFormat =
        "رمز التحقق الخاص بك هو: {0}\nلا تشارك هذا الرمز مع أي شخص.";

    [Tooltip("How many digits the generated verification code has.")]
    [SerializeField, Range(4, 6)]
    private int otpDigitCount = 4;

    private float stageStartTime;
    private float callStartTime;

    private bool callActive;
    private bool decisionSent;
    private bool otpAppeared;

    private string currentOtpCode = "";

    private Coroutine otpCoroutine;
    private bool openingTextPlaying;
private bool pendingYes;

    // Keywords that mean the AI is asking for the verification code —
    // when detected in the AI's dialogue, the OTP message arrives instantly.
    private static readonly string[] OtpRequestKeywords =
    {
        "رمز", "الرمز", "كود", "الكود",
        "code", "otp", "verification"
    };

    private void Awake()
    {
        if (answerButton != null)
        {
            answerButton.onClick.AddListener(
                AnswerCall
            );
        }

        if (rejectButton != null)
        {
            rejectButton.onClick.AddListener(
                RejectCall
            );
        }

        if (endCallButton != null)
        {
            endCallButton.onClick.AddListener(
                EndCall
            );
        }
    }

    private void OnEnable()
    {
        if (manager != null)
        {
            manager.OnChatResponseReceived +=
                HandleChatResponse;

            manager.OnUserSpeechRecognized +=
                HandleUserSpeech;
        }

        ResetStage();
        LoadStageText();

        stageStartTime = Time.time;
    }

   private void Update()
{
    if (!callActive)
    {
        return;
    }

    float elapsed = Time.time - callStartTime;

    int minutes = Mathf.FloorToInt(elapsed / 60f);
    int seconds = Mathf.FloorToInt(elapsed % 60f);

    string timer = $"{minutes:00}:{seconds:00}";

    if (beforeOtpTimerText != null)
    {
        beforeOtpTimerText.text = timer;
    }

    if (afterOtpTimerText != null)
    {
        afterOtpTimerText.text = timer;
    }
}
    private void ResetStage()
    {
        decisionSent = false;
        callActive = false;
        otpAppeared = false;

        // Fresh random verification code for this run.
        currentOtpCode = "";
        for (int i = 0; i < otpDigitCount; i++)
        {
            currentOtpCode +=
                Random.Range(i == 0 ? 1 : 0, 10).ToString();
        }

        if (incomingCallWindow != null)
        {
            incomingCallWindow.SetActive(true);
        }

        if (activeCallWindow != null)
        {
            activeCallWindow.SetActive(false);
        }

        if (beforeOtpWindow != null)
        {
            beforeOtpWindow.SetActive(false);
        }

        if (afterOtpWindow != null)
        {
            afterOtpWindow.SetActive(false);
        }

        if (otpWindow != null)
        {
            otpWindow.SetActive(false);
        }

        if (endCallButton != null)
        {
            endCallButton.gameObject.SetActive(false);
        }

       if (beforeOtpTimerText != null)
{
    beforeOtpTimerText.text = "00:00";
}

if (afterOtpTimerText != null)
{
    afterOtpTimerText.text = "00:00";
}
    }

    private void LoadStageText()
    {
        SyraxStage stage =
            manager?.CurrentSession?.current_stage;

        // Guard against showing another stage's content if the session's
        // current_stage is stale (e.g. a failed event request).
        if (stage != null && stage.type != "PhoneCall")
        {
            stage = null;
        }

        if (stage == null)
        {
            if (incomingDialogueText != null)
            {
                incomingDialogueText.text =
                    "Incoming call";
            }

            if (activeDialogueText != null)
            {
                activeDialogueText.text =
                    "Loading call...";
            }

            return;
        }

        if (incomingDialogueText != null)
        {
            incomingDialogueText.text =
                SyraxArabicText.Fix(
                    string.IsNullOrWhiteSpace(
                        stage.title
                    )
                        ? "Incoming call"
                        : stage.title
                );
        }

        if (activeDialogueText != null)
        {
            activeDialogueText.text =
                SyraxArabicText.Fix(
                    stage.opening_text
                );
        }
    }

    // Public so SyraxDebugHotkeys can start the call during desktop testing.
    public void AnswerCall()
    {
        if (
            decisionSent ||
            callActive
        )
        {
            return;
        }

        manager?.StopStageAudio();

        if (incomingCallWindow != null)
        {
            incomingCallWindow.SetActive(false);
        }

        if (activeCallWindow != null)
        {
            activeCallWindow.SetActive(true);
        }

        // أول نافذة أثناء المكالمة بدون زر إغلاق.
        if (beforeOtpWindow != null)
        {
            beforeOtpWindow.SetActive(true);
        }

        if (afterOtpWindow != null)
        {
            afterOtpWindow.SetActive(false);
        }

        if (otpWindow != null)
        {
            otpWindow.SetActive(false);
        }

        if (endCallButton != null)
        {
            endCallButton.gameObject.SetActive(false);
        }

        callStartTime = Time.time;
        callActive = true;

        voskBridge?.StartAcceptingSpeech();

        string openingText =
            manager?.CurrentSession
                ?.current_stage
                ?.opening_text;

        if (!string.IsNullOrWhiteSpace(
            openingText
        ))
        {
            if (activeDialogueText != null)
            {
                activeDialogueText.text =
                    openingText;
            }

            StartCoroutine(PlayOpeningText(openingText));
        }

        otpCoroutine =
            StartCoroutine(
                ShowOtpAfterDelay()
            );
    }
    private IEnumerator PlayOpeningText(string openingText)
{
    openingTextPlaying = true;
    pendingYes = false;

    piperTTSClient?.Speak(openingText);

    // ينتظر إلى أن ينتهي صوت Piper.
    while (piperTTSClient != null &&
           piperTTSClient.IsSpeaking)
    {
        yield return null;
    }

    openingTextPlaying = false;

    // لو اللاعب قال نعم أثناء كلام المحتال، نرسلها بعد انتهاء الكلام.
    if (pendingYes && callActive && !decisionSent)
    {
        pendingYes = false;
        manager?.SendUserSpeech("نعم");
    }
}

    private IEnumerator ShowOtpAfterDelay()
    {
        // Fallback timer — the OTP also arrives instantly the moment the AI
        // asks for the code (see HandleChatResponse).
        yield return new WaitForSeconds(
            otpDelay
        );

        TriggerOtpMessage();
    }

    /// <summary>
    /// Makes the OTP message arrive now (idempotent). Called by the fallback
    /// timer or immediately when the AI asks for the verification code.
    /// </summary>
    private void TriggerOtpMessage()
    {
        if (
            otpAppeared ||
            decisionSent ||
            !callActive
        )
        {
            return;
        }

        otpAppeared = true;

        if (otpCoroutine != null)
        {
            StopCoroutine(otpCoroutine);
            otpCoroutine = null;
        }

        StartCoroutine(ShowOtpMessage());
    }

    private IEnumerator ShowOtpMessage()
    {
        if (beforeOtpWindow != null)
        {
            beforeOtpWindow.SetActive(false);
        }

        // النافذة الثانية فيها زر الإغلاق.
        if (afterOtpWindow != null)
        {
            afterOtpWindow.SetActive(true);
        }

        if (otpText != null)
        {
            otpText.text = SyraxArabicText.Fix(
                string.Format(
                    otpMessageFormat,
                    currentOtpCode
                )
            );
        }

        if (otpWindow != null)
        {
            otpWindow.SetActive(true);
        }

        if (endCallButton != null)
        {
            endCallButton.gameObject.SetActive(true);
        }

        SyraxLogger.Log(
            $"OTP message arrived (code {currentOtpCode})."
        );

        yield return new WaitForSeconds(
            otpVisibleDuration
        );

        // إشعار OTP يختفي، لكن النافذة الثانية وزر الإغلاق يبقون.
        if (otpWindow != null)
        {
            otpWindow.SetActive(false);
        }

        if (afterOtpWindow != null)
        {
            afterOtpWindow.SetActive(true);
        }

        if (endCallButton != null)
        {
            endCallButton.gameObject.SetActive(true);
        }
    }

    private void HandleChatResponse(
        ChatResponse response
    )
    {
        // GetDialogue() supports both the new (dialogue) and legacy
        // (reply_text) backend contracts.
        string dialogue = response?.GetDialogue();

        if (
            response == null ||
            decisionSent ||
            !callActive ||
            string.IsNullOrWhiteSpace(dialogue)
        )
        {
            return;
        }

        if (activeDialogueText != null)
        {
            // Fixed (shaped) text for DISPLAY only — TTS gets the raw string.
            activeDialogueText.text =
                SyraxArabicText.Fix(dialogue);
        }

        piperTTSClient?.Speak(dialogue);

        // The scammer asked for the verification code → the OTP message
        // arrives on the phone right now.
        if (!otpAppeared && DialogueAsksForCode(dialogue))
        {
            TriggerOtpMessage();
        }

        if (
            response.conversationState == "PlayerUnsafe" ||
            response.conversation_status == "user_unsafe"
        )
        {
            PlayerSharedOtp();
        }
    }

    private static bool DialogueAsksForCode(string dialogue)
    {
        string lower = dialogue.ToLowerInvariant();

        foreach (string keyword in OtpRequestKeywords)
        {
            if (lower.Contains(keyword))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Local, definitive detection: if the player SPEAKS the code that is on
    /// screen, that is the unsafe decision — no AI judgement needed.
    /// Handles both digits ("4821") and spoken number words
    /// ("four eight two one" / "أربعة ثمانية اثنين واحد").
    /// </summary>
    private void HandleUserSpeech(string transcript)
    {
        string normalizedTranscript =
    transcript
        .Trim()
        .ToLowerInvariant();

if (openingTextPlaying)
{
    bool saidYes =
        normalizedTranscript.Contains("نعم") ||
        normalizedTranscript.Contains("ايوه") ||
        normalizedTranscript.Contains("أيوه") ||
        normalizedTranscript.Contains("ايه") ||
        normalizedTranscript.Contains("yes");

    if (saidYes)
    {
        pendingYes = true;

        SyraxLogger.Log(
            "Player said YES during opening text. It will be processed after the opening finishes."
        );
    }

    // لا نسمح لأي كلام أن يقطع صوت البداية.
    return;
}
        if (
            decisionSent ||
            !callActive ||
            !otpAppeared ||
            string.IsNullOrWhiteSpace(currentOtpCode)
        )
        {
            return;
        }

        if (TranscriptContainsCode(transcript, currentOtpCode))
        {
            SyraxLogger.Log(
                "Player spoke the OTP code aloud — recording unsafe decision."
            );

            PlayerSharedOtp();
        }
    }

    private static bool TranscriptContainsCode(
        string transcript,
        string code
    )
    {
        string digits = NormalizeToDigits(transcript);
        return digits.Contains(code);
    }

    // Converts a transcript into a bare digit string:
    // "four eight two one" -> "4821", "أربعة ٨ two 1" -> "4821".
    private static string NormalizeToDigits(string text)
    {
        var sb = new System.Text.StringBuilder();

        string[] tokens = text
            .ToLowerInvariant()
            .Split(
                new[] { ' ', ',', '.', '،', '؟', '?', '!', '-' },
                System.StringSplitOptions.RemoveEmptyEntries
            );

        foreach (string token in tokens)
        {
            switch (token)
            {
                case "zero": case "oh": case "صفر": sb.Append('0'); break;
                case "one": case "واحد": case "وحده": sb.Append('1'); break;
                case "two": case "اثنين": case "اثنان": case "إثنين": sb.Append('2'); break;
                case "three": case "ثلاثة": case "ثلاثه": sb.Append('3'); break;
                case "four": case "for": case "أربعة": case "اربعة": case "اربعه": sb.Append('4'); break;
                case "five": case "خمسة": case "خمسه": sb.Append('5'); break;
                case "six": case "ستة": case "سته": sb.Append('6'); break;
                case "seven": case "سبعة": case "سبعه": sb.Append('7'); break;
                case "eight": case "ثمانية": case "ثمانيه": case "ثمنية": sb.Append('8'); break;
                case "nine": case "تسعة": case "تسعه": sb.Append('9'); break;

                default:
                    // Keep any literal digits inside the token (incl. Arabic-Indic).
                    foreach (char c in token)
                    {
                        if (c >= '0' && c <= '9') sb.Append(c);
                        else if (c >= '٠' && c <= '٩') sb.Append((char)('0' + (c - '٠')));
                    }
                    break;
            }
        }

        return sb.ToString();
    }

    private void RejectCall()
    {
        if (decisionSent)
        {
            return;
        }

        decisionSent = true;

        StopVoiceSystems();

        manager?.StopStageAudio();

        manager?.RejectUnknownCall(
            GetReactionTime()
        );
    }

    private void EndCall()
    {
        if (
            decisionSent ||
            !otpAppeared
        )
        {
            return;
        }

        decisionSent = true;
        callActive = false;

        StopVoiceSystems();

        manager?.EndCallWithoutSharingOtp(
            GetReactionTime()
        );
    }

    public void PlayerSharedOtp()
    {
        if (decisionSent)
        {
            return;
        }

        decisionSent = true;
        callActive = false;

        StopVoiceSystems();

        manager?.ShareOtp(
            GetReactionTime()
        );
    }

    private void StopVoiceSystems()
    {
        voskBridge?.StopAcceptingSpeech();
        piperTTSClient?.StopSpeaking();
    }

    private float GetReactionTime()
    {
        return Time.time - stageStartTime;
    }

    private void OnDisable()
    {
        if (manager != null)
        {
            manager.OnChatResponseReceived -=
                HandleChatResponse;

            manager.OnUserSpeechRecognized -=
                HandleUserSpeech;
        }

        callActive = false;

        StopVoiceSystems();

        if (otpCoroutine != null)
        {
            StopCoroutine(
                otpCoroutine
            );

            otpCoroutine = null;
        }
    }
}