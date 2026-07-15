using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PhoneCallStageController : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField]
    private SyraxExperienceManager manager;

    [Header("Windows")]
    [SerializeField]
    private GameObject incomingCallWindow;

    [SerializeField]
    private GameObject activeCallWindow;

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

    [Header("Text To Speech")]
    public UnityEvent<string> onSpeakText;

    private float stageStartTime;
    private float callStartTime;

    private bool callActive;
    private bool decisionSent;
    private bool otpAppeared;

    private Coroutine otpCoroutine;

    private void Awake()
    {
        answerButton.onClick.AddListener(
            AnswerCall
        );

        rejectButton.onClick.AddListener(
            RejectCall
        );

        endCallButton.onClick.AddListener(
            EndCall
        );
    }

    private void OnEnable()
    {
        ResetStage();
        LoadStageText();

        stageStartTime = Time.time;
    }

    private void Update()
    {
        if (!callActive)
            return;

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

        incomingCallWindow.SetActive(true);
        activeCallWindow.SetActive(false);
        otpWindow.SetActive(false);

        endCallButton.gameObject.SetActive(false);

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
            incomingDialogueText.text =
                "مكالمة واردة";

            activeDialogueText.text =
                "جارٍ تحميل المكالمة...";

            return;
        }

        incomingDialogueText.text =
            string.IsNullOrWhiteSpace(
                stage.title
            )
                ? "مكالمة واردة"
                : stage.title;

        activeDialogueText.text =
            stage.opening_text;
    }

    private void AnswerCall()
    {
        manager.StopStageAudio();

        incomingCallWindow.SetActive(false);
        activeCallWindow.SetActive(true);

        callStartTime = Time.time;
        callActive = true;

        string text =
            manager.CurrentSession
                ?.current_stage
                ?.opening_text;

        if (!string.IsNullOrWhiteSpace(text))
        {
            activeDialogueText.text = text;

            // يرسل النص لأداة Text-to-Speech.
            onSpeakText?.Invoke(text);
        }

        otpCoroutine =
            StartCoroutine(
                ShowOtpAfterDelay()
            );
    }

    private IEnumerator ShowOtpAfterDelay()
    {
        yield return new WaitForSeconds(
            otpDelay
        );

        otpAppeared = true;

        otpText.text =
            "رمز التحقق: 4821\n" +
            "لا تشارك الرمز مع أي شخص";

        otpWindow.SetActive(true);

        endCallButton.gameObject.SetActive(true);

        yield return new WaitForSeconds(
            otpVisibleDuration
        );

        otpWindow.SetActive(false);

        // زر إنهاء المكالمة يبقى ظاهرًا.
        endCallButton.gameObject.SetActive(true);
    }

    private void RejectCall()
    {
        if (decisionSent)
            return;

        decisionSent = true;

        manager.StopStageAudio();

        manager.RejectUnknownCall(
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

        manager.EndCallWithoutSharingOtp(
            GetReactionTime()
        );
    }

    // استدعيها من Speech-to-Text إذا اكتشف أن اللاعب قال الرمز.
    public void PlayerSharedOtp()
    {
        if (decisionSent)
            return;

        decisionSent = true;
        callActive = false;

        manager.ShareOtp(
            GetReactionTime()
        );
    }

    private float GetReactionTime()
    {
        return Time.time - stageStartTime;
    }

    private void OnDisable()
    {
        callActive = false;

        if (otpCoroutine != null)
        {
            StopCoroutine(otpCoroutine);
            otpCoroutine = null;
        }
    }
}