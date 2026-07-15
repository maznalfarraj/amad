using UnityEngine;
using UnityEngine.UI;

public class WebsiteStageController : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField]
    private SyraxExperienceManager manager;

    [Header("Windows")]
    [SerializeField]
    private GameObject smsWindow;

    [SerializeField]
    private GameObject webSmsWindow;

    [Header("Buttons")]
    [SerializeField]
    private Button openButton;

    [SerializeField]
    private Button linkOneButton;

    [SerializeField]
    private Button linkTwoButton;

    [Header("Correct Link")]
    [Tooltip("فعليها إذا الرابط الأول في الصورة هو الرابط الصحيح.")]
    [SerializeField]
    private bool linkOneIsOfficial = true;

    private float stageStartTime;
    private bool decisionSent;

    private void Awake()
    {
        openButton.onClick.AddListener(
            OpenMessage
        );

        linkOneButton.onClick.AddListener(
            SelectLinkOne
        );

        linkTwoButton.onClick.AddListener(
            SelectLinkTwo
        );
    }

    private void OnEnable()
    {
        decisionSent = false;
        stageStartTime = Time.time;

        smsWindow.SetActive(true);
        webSmsWindow.SetActive(false);
    }

    private void OpenMessage()
    {
        smsWindow.SetActive(false);
        webSmsWindow.SetActive(true);
    }

    private void SelectLinkOne()
    {
        if (decisionSent)
            return;

        decisionSent = true;

        if (linkOneIsOfficial)
        {
            manager.SelectOfficialBankLink(
                GetReactionTime()
            );
        }
        else
        {
            manager.SelectPhishingLink(
                GetReactionTime()
            );
        }
    }

    private void SelectLinkTwo()
    {
        if (decisionSent)
            return;

        decisionSent = true;

        if (linkOneIsOfficial)
        {
            manager.SelectPhishingLink(
                GetReactionTime()
            );
        }
        else
        {
            manager.SelectOfficialBankLink(
                GetReactionTime()
            );
        }
    }

    private float GetReactionTime()
    {
        return Time.time - stageStartTime;
    }
}