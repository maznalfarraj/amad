using System.Collections;

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MessageStageController : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField]
    private SyraxExperienceManager manager;

    [Header("Windows")]
    [SerializeField]
    private GameObject chatWindow;

    [SerializeField]
    private GameObject choicePopup;

    [SerializeField]
    private GameObject callWindow;

    [Header("Chat")]
    [SerializeField]
    private TMP_Text messageText;

    [SerializeField]
    private float choicePopupDelay = 30f;

    [Header("Buttons")]
    [SerializeField]
    private Button transferButton;

    [SerializeField]
    private Button callButton;

    [SerializeField]
    private Button endCallButton;

    [Header("Mother Call")]
    [SerializeField]
    private TMP_Text callTimerText;

    [SerializeField]
    private AudioSource motherAudioSource;

    [Header("Text To Speech")]
    public UnityEvent<string> onSpeakMessage;

    [Header("Designer Events (hook anything — no code needed)")]
    [Tooltip("Fired the moment the player chooses to CALL the mother to verify. Hook the mother's real voice line ('لا، أنا ما طلبت أي فلوس!'), animations, effects…")]
    public UnityEvent onCallMotherChosen;

    [Tooltip("Fired when the verification call finishes, just before the experience advances. Hook closing effects.")]
    public UnityEvent onVerifiedCallFinished;

    [Tooltip("Fired the moment the player TRANSFERS the money without verifying. Hook alarm/regret effects, custom ending sequence…")]
    public UnityEvent onTransferMoneyChosen;

    [Tooltip("Seconds to wait AFTER onTransferMoneyChosen fires before the experience advances — gives designer effects time to play.")]
    [SerializeField, Min(0f)]
    private float transferEffectDuration = 0f;

    private float stageStartTime;
    private float callStartTime;

    private bool callActive;
    private bool decisionSent;

    private Coroutine popupCoroutine;
    private Coroutine callCoroutine;

    private void Awake()
    {
        transferButton.onClick.AddListener(
            TransferMoney
        );

        callButton.onClick.AddListener(
            CallMother
        );

        if (endCallButton != null)
        {
            endCallButton.onClick.AddListener(
                FinishVerifiedCall
            );
        }
    }

    private void OnEnable()
    {
        ResetStage();
        LoadMessage();

        stageStartTime = Time.time;

        popupCoroutine =
            StartCoroutine(
                ShowChoicePopup()
            );
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

        chatWindow.SetActive(true);
        choicePopup.SetActive(false);
        callWindow.SetActive(false);

        if (callTimerText != null)
        {
            callTimerText.text = "00:00";
        }

        if (motherAudioSource != null)
        {
            motherAudioSource.Stop();
        }
    }

    private void LoadMessage()
    {
        SyraxStage stage =
            manager?.CurrentSession?.current_stage;

        // opening_text is the AI's actual in-character message (what the
        // "mother" says). body_text is internal scenario direction and must
        // never be shown to the player. The type guard protects against a
        // stale stage when the previous event request failed offline.
        string text =
            stage != null && stage.type == "Message"
                ? stage.opening_text
                : "";

        if (string.IsNullOrWhiteSpace(text))
        {
            text =
                "حول لي مبلغ بسرعة، أنا في موقف طارئ.";
        }

        // Shaped text for display; the TTS event gets the RAW string.
        messageText.text =
            SyraxArabicText.Fix(text);

        // يرسل رسالة الـAI إلى Text-to-Speech.
        onSpeakMessage?.Invoke(text);
    }

    private IEnumerator ShowChoicePopup()
    {
        yield return new WaitForSeconds(
            choicePopupDelay
        );

        if (!decisionSent)
        {
            choicePopup.SetActive(true);
        }
    }

    private void TransferMoney()
    {
        if (decisionSent)
            return;

        decisionSent = true;

        // Designers' moment: alarm sound, screen effect, custom ending…
        onTransferMoneyChosen?.Invoke();

        StartCoroutine(
            SendTransferAfterEffects()
        );
    }

    private IEnumerator SendTransferAfterEffects()
    {
        if (transferEffectDuration > 0f)
        {
            yield return new WaitForSeconds(
                transferEffectDuration
            );
        }

        manager.TransferMoney(
            GetReactionTime()
        );
    }

    private void CallMother()
    {
        if (decisionSent)
            return;

        choicePopup.SetActive(false);
        chatWindow.SetActive(false);
        callWindow.SetActive(true);

        callStartTime = Time.time;
        callActive = true;

        // Designers' moment: the mother's real voice ("لا، أنا ما طلبت
        // أي فلوس!"), caller UI animation, etc.
        onCallMotherChosen?.Invoke();

        float duration = 4f;

        if (
            motherAudioSource != null &&
            motherAudioSource.clip != null
        )
        {
            motherAudioSource.Play();

            duration =
                motherAudioSource.clip.length;
        }

        callCoroutine =
            StartCoroutine(
                WaitForMotherAudio(duration)
            );
    }

    private IEnumerator WaitForMotherAudio(
        float duration
    )
    {
        yield return new WaitForSeconds(
            duration
        );

        FinishVerifiedCall();
    }

    private void FinishVerifiedCall()
    {
        if (decisionSent)
            return;

        decisionSent = true;
        callActive = false;

        if (motherAudioSource != null)
        {
            motherAudioSource.Stop();
        }

        callWindow.SetActive(false);

        // Designers' moment: closing effects before the experience advances.
        onVerifiedCallFinished?.Invoke();

        manager.CallMotherToVerify(
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

        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }

        if (callCoroutine != null)
        {
            StopCoroutine(callCoroutine);
            callCoroutine = null;
        }

        if (motherAudioSource != null)
        {
            motherAudioSource.Stop();
        }
    }
}