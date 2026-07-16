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
    private TMP_Text callTimerText;

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
    [SerializeField]
    private float otpDelay = 6f;

    [SerializeField]
    private float otpVisibleDuration = 4f;

    private float stageStartTime;
    private float callStartTime;

    private bool callActive;
    private bool decisionSent;
    private bool otpAppeared;

    private Coroutine otpCoroutine;

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
        }

        ResetStage();
        LoadStageText();

        stageStartTime = Time.time;
    }

    private void Update()
    {
        if (
            !callActive ||
            callTimerText == null
        )
        {
            return;
        }

        float elapsed =
            Time.time - callStartTime;

        int minutes =
            Mathf.FloorToInt(
                elapsed / 60f
            );

        int seconds =
            Mathf.FloorToInt(
                elapsed % 60f
            );

        callTimerText.text =
            $"{minutes:00}:{seconds:00}";
    }

    private void ResetStage()
    {
        decisionSent = false;
        callActive = false;
        otpAppeared = false;

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

        if (callTimerText != null)
        {
            callTimerText.text = "00:00";
        }
    }

    private void LoadStageText()
    {
        SyraxStage stage =
            manager?.CurrentSession?.current_stage;

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
                string.IsNullOrWhiteSpace(
                    stage.title
                )
                    ? "Incoming call"
                    : stage.title;
        }

        if (activeDialogueText != null)
        {
            activeDialogueText.text =
                stage.opening_text;
        }
    }

    private void AnswerCall()
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

            piperTTSClient?.Speak(
                openingText
            );
        }

        otpCoroutine =
            StartCoroutine(
                ShowOtpAfterDelay()
            );
    }

    private IEnumerator ShowOtpAfterDelay()
    {
        // أولًا انتظري قبل ظهور رسالة OTP.
        yield return new WaitForSeconds(
            otpDelay
        );

        if (
            decisionSent ||
            !callActive
        )
        {
            yield break;
        }

        otpAppeared = true;

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
            otpText.text =
                "Verification code: 4821\n" +
                "Do not share this code with anyone.";
        }

        if (otpWindow != null)
        {
            otpWindow.SetActive(true);
        }

        if (endCallButton != null)
        {
            endCallButton.gameObject.SetActive(true);
        }

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
        if (
            response == null ||
            decisionSent ||
            !callActive ||
            string.IsNullOrWhiteSpace(
                response.reply_text
            )
        )
        {
            return;
        }

        if (activeDialogueText != null)
        {
            activeDialogueText.text =
                response.reply_text;
        }

        piperTTSClient?.Speak(
            response.reply_text
        );

        if (
            response.conversation_status ==
            "user_unsafe"
        )
        {
            PlayerSharedOtp();
        }
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