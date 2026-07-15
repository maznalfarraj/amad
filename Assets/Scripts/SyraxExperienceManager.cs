using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class SyraxExperienceManager : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private string serverBaseUrl = "http://localhost:3000";

    [Header("Experience Objects")]
    [SerializeField] private GameObject phoneCallObject;
    [SerializeField] private GameObject websiteObject;
    [SerializeField] private GameObject messageObject;

    [Header("Completion")]
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private TMP_Text completionMessageText;
    [SerializeField] private TMP_Text finalScoreText;

    public SyraxSession CurrentSession { get; private set; }

    private const int PointsPerCorrectDecision = 5;
    private const int MaximumScore = 15;

    private readonly string[] fixedStageOrder =
    {
        "PhoneCall",
        "Website",
        "Message"
    };

    private int currentStageIndex;
    private int totalScore;
    private bool isSendingDecision;

    private void Start()
    {
        DisableAllStages();

        if (completionPanel != null)
            completionPanel.SetActive(false);

        StartCoroutine(LoadActiveSession());
    }

    public IEnumerator LoadActiveSession()
    {
        string url = $"{serverBaseUrl}/api/unity/session";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                $"SYRAX session error: {request.error}\n" +
                $"Response: {request.downloadHandler?.text}"
            );
            yield break;
        }

        CurrentSession =
            JsonUtility.FromJson<SyraxSession>(
                request.downloadHandler.text
            );

        if (CurrentSession == null ||
            string.IsNullOrWhiteSpace(CurrentSession.session_id))
        {
            Debug.LogError("Invalid SYRAX session.");
            yield break;
        }

        currentStageIndex = 0;
        totalScore = 0;

        ActivateCurrentStage();
    }

    private void ActivateCurrentStage()
    {
        DisableAllStages();

        string stageType = fixedStageOrder[currentStageIndex];

        switch (stageType)
        {
            case "PhoneCall":
                if (phoneCallObject != null)
                    phoneCallObject.SetActive(true);
                break;

            case "Website":
                if (websiteObject != null)
                    websiteObject.SetActive(true);
                break;

            case "Message":
                if (messageObject != null)
                    messageObject.SetActive(true);
                break;

            default:
                Debug.LogError($"Unknown stage: {stageType}");
                return;
        }

        Debug.Log(
            $"SYRAX Stage {currentStageIndex + 1}/3 started: {stageType}"
        );
    }

    public void RejectUnknownCall(float reactionTime)
    {
        CompleteStage(
            "PHONE_CALL_DECISION",
            "rejected_unknown_call",
            true,
            reactionTime
        );
    }

    public void EndCallWithoutSharingOtp(float reactionTime)
    {
        CompleteStage(
            "PHONE_CALL_DECISION",
            "ended_call_without_sharing_otp",
            true,
            reactionTime
        );
    }

    public void ShareOtp(float reactionTime)
    {
        CompleteStage(
            "PHONE_CALL_DECISION",
            "shared_otp",
            false,
            reactionTime
        );
    }

    public void SelectOfficialBankLink(float reactionTime)
    {
        CompleteStage(
            "WEBSITE_DECISION",
            "selected_official_domain",
            true,
            reactionTime
        );
    }

    public void SelectPhishingLink(float reactionTime)
    {
        CompleteStage(
            "WEBSITE_DECISION",
            "selected_phishing_domain",
            false,
            reactionTime
        );
    }

    public void CallMotherToVerify(float reactionTime)
    {
        CompleteStage(
            "MESSAGE_DECISION",
            "called_mother_to_verify",
            true,
            reactionTime
        );
    }

    public void TransferMoney(float reactionTime)
    {
        CompleteStage(
            "MESSAGE_DECISION",
            "transferred_money_without_verification",
            false,
            reactionTime
        );
    }

    public void SendUserSpeech(string speechText)
    {
        if (CurrentSession == null ||
            string.IsNullOrWhiteSpace(speechText))
        {
            return;
        }

        StartCoroutine(SendChatRequest(speechText));
    }

    private IEnumerator SendChatRequest(string userMessage)
    {
        string url = $"{serverBaseUrl}/api/chat";

        ChatRequest body = new ChatRequest
        {
            session_id = CurrentSession.session_id,
            user_message = userMessage
        };

        using UnityWebRequest request =
            CreateJsonPostRequest(
                url,
                JsonUtility.ToJson(body)
            );

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                $"SYRAX chat error: {request.error}\n" +
                $"Response: {request.downloadHandler?.text}"
            );
            yield break;
        }

        ChatResponse response =
            JsonUtility.FromJson<ChatResponse>(
                request.downloadHandler.text
            );

        Debug.Log($"AI Reply: {response.reply_text}");

        // ???? ???? Unity response.reply_text ??? Text-to-Speech.
    }

    private void CompleteStage(
        string action,
        string decision,
        bool isCorrect,
        float reactionTime
    )
    {
        if (isSendingDecision || CurrentSession == null)
            return;

        int pointsAwarded =
            isCorrect ? PointsPerCorrectDecision : 0;

        totalScore += pointsAwarded;

        StartCoroutine(
            SendEventAndAdvance(
                action,
                decision,
                isCorrect,
                pointsAwarded,
                reactionTime
            )
        );
    }

    private IEnumerator SendEventAndAdvance(
        string action,
        string decision,
        bool isCorrect,
        int pointsAwarded,
        float reactionTime
    )
    {
        isSendingDecision = true;

        string url = $"{serverBaseUrl}/api/event";

        EventRequest body = new EventRequest
        {
            session_id = CurrentSession.session_id,
            action = action,
            decision = decision,
            reaction_time = reactionTime,
            stage_completed = true,

            details = new EventDetails
            {
                stage_type = fixedStageOrder[currentStageIndex],
                stage_number = currentStageIndex + 1,
                is_correct = isCorrect,
                points_awarded = pointsAwarded,
                total_score = totalScore,
                maximum_score = MaximumScore
            }
        };

        using UnityWebRequest request =
            CreateJsonPostRequest(
                url,
                JsonUtility.ToJson(body)
            );

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            isSendingDecision = false;

            Debug.LogError(
                $"SYRAX event error: {request.error}\n" +
                $"Response: {request.downloadHandler?.text}"
            );
            yield break;
        }

        EventResponse response =
            JsonUtility.FromJson<EventResponse>(
                request.downloadHandler.text
            );

        currentStageIndex++;

        if (currentStageIndex >= fixedStageOrder.Length)
        {
            ShowCompletion(response.completion_message);
        }
        else
        {
            ActivateCurrentStage();
        }

        isSendingDecision = false;
    }

    private void ShowCompletion(string serverCompletionMessage)
    {
        DisableAllStages();

        string playerName =
            CurrentSession?.user_profile?.display_name;

        if (string.IsNullOrWhiteSpace(playerName))
            playerName = "Player";

        string completionMessage =
            !string.IsNullOrWhiteSpace(serverCompletionMessage)
                ? serverCompletionMessage
                : $"????? ?? {playerName} ??????? ????? SYRAX. " +
                  "?? ????? ???????? ?????? ???? ????? ???? ??????.";

        if (completionPanel != null)
            completionPanel.SetActive(true);

        if (completionMessageText != null)
            completionMessageText.text = completionMessage;

        if (finalScoreText != null)
            finalScoreText.text = $"{totalScore} / {MaximumScore}";
    }

    public string GetDashboardApiUrl()
    {
        if (CurrentSession == null)
            return string.Empty;

        return
            $"{serverBaseUrl}/api/dashboard/" +
            $"{CurrentSession.session_id}";
    }

    private void DisableAllStages()
    {
        if (phoneCallObject != null)
            phoneCallObject.SetActive(false);

        if (websiteObject != null)
            websiteObject.SetActive(false);

        if (messageObject != null)
            messageObject.SetActive(false);
    }

    private UnityWebRequest CreateJsonPostRequest(
        string url,
        string json
    )
    {
        UnityWebRequest request =
            new UnityWebRequest(
                url,
                UnityWebRequest.kHttpVerbPOST
            );

        request.uploadHandler =
            new UploadHandlerRaw(
                System.Text.Encoding.UTF8.GetBytes(json)
            );

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        return request;
    }
}

[Serializable]
public class SyraxSession
{
    public string session_id;
    public string engine_mode;
    public string status;
    public SyraxUserProfile user_profile;
    public SyraxRiskProfile risk_profile;
    public string[] stage_order;
    public int current_stage_index;
    public SyraxStage current_stage;
}

[Serializable]
public class SyraxUserProfile
{
    public string display_name;
    public string age_group;
    public string financial_experience;
    public string language;
}

[Serializable]
public class SyraxRiskProfile
{
    public string primary_risk;
    public int risk_score;
    public string[] weaknesses;
    public string reasoning;
}

[Serializable]
public class SyraxStage
{
    public string type;
    public string scenario_id;
    public string title;
    public string actor_name;
    public string opening_text;
    public string body_text;
    public string call_to_action;
    public string fake_url;
    public string goal;
    public string visual_hint;
}

[Serializable]
public class ChatRequest
{
    public string session_id;
    public string user_message;
}

[Serializable]
public class ChatResponse
{
    public string session_id;
    public string stage_type;
    public string engine_mode;
    public string reply_text;
    public string emotion;
    public string conversation_status;
    public string detected_behavior;
}

[Serializable]
public class EventRequest
{
    public string session_id;
    public string action;
    public string decision;
    public bool stage_completed;
    public float reaction_time;
    public EventDetails details;
}

[Serializable]
public class EventDetails
{
    public string stage_type;
    public int stage_number;
    public bool is_correct;
    public int points_awarded;
    public int total_score;
    public int maximum_score;
}

[Serializable]
public class EventResponse
{
    public string session_id;
    public bool stage_completed;
    public bool experience_completed;
    public int completed_stages;
    public string completion_message;
    public string dashboard_url;
    public string message;
    public SyraxStage next_stage;
}