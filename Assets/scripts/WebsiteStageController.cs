using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Website stage — static designer-built scenario (no AI content).
///
/// Flow: the player gets an SMS notification, opens the messages app and
/// finds TWO messages that arrived at the same time. One contains the
/// official bank link, the other a look-alike phishing link. The player
/// must check the domain name and tap the correct one.
///
/// DESIGNERS: build any visuals you want, then hook YOUR buttons to
///   OnOfficialLinkClicked()  — the genuine bank link
///   OnPhishingLinkClicked()  — the fake/phishing link
/// via the Button's OnClick list (drag this "Website Stage" object in and
/// pick the method). Alternatively drag the buttons into the two fields
/// below and the hookup happens automatically.
///
/// Scoring (backend): official = +points; phishing = 0 points and the
/// mistake is flagged in the player's behavioral record. Either way the
/// experience advances WITHOUT showing the result.
/// </summary>
public class WebsiteStageController : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField]
    private SyraxExperienceManager manager;

    [Header("Windows")]
    [Tooltip("The SMS notification window shown first.")]
    [SerializeField]
    private GameObject smsWindow;

    [Tooltip("The messages-app window with the two bank messages.")]
    [SerializeField]
    private GameObject webSmsWindow;

    [Header("Buttons (optional — designers may instead wire OnClick manually)")]
    [Tooltip("Opens the messages app from the SMS notification.")]
    [SerializeField]
    private Button openButton;

    [Tooltip("The message/link that is the OFFICIAL bank domain.")]
    [SerializeField]
    private Button officialLinkButton;

    [Tooltip("The message/link that is the PHISHING domain.")]
    [SerializeField]
    private Button phishingLinkButton;

    private float stageStartTime;
    private bool decisionSent;

    private void Awake()
    {
        if (openButton != null)
            openButton.onClick.AddListener(OpenMessage);

        if (officialLinkButton != null)
            officialLinkButton.onClick.AddListener(OnOfficialLinkClicked);

        if (phishingLinkButton != null)
            phishingLinkButton.onClick.AddListener(OnPhishingLinkClicked);
    }

    private void OnEnable()
    {
        decisionSent = false;
        stageStartTime = Time.time;

        if (smsWindow != null)
            smsWindow.SetActive(true);

        if (webSmsWindow != null)
            webSmsWindow.SetActive(false);
    }

    /// <summary>Shows the messages app. Also usable directly from a Button's OnClick.</summary>
    public void OpenMessage()
    {
        if (smsWindow != null)
            smsWindow.SetActive(false);

        if (webSmsWindow != null)
            webSmsWindow.SetActive(true);
    }

    /// <summary>Player tapped the OFFICIAL bank link → correct, +points, advance.</summary>
    public void OnOfficialLinkClicked()
    {
        if (decisionSent)
            return;

        decisionSent = true;

        manager.SelectOfficialBankLink(
            Time.time - stageStartTime
        );
    }

    /// <summary>Player tapped the PHISHING link → 0 points, mistake flagged, advance.</summary>
    public void OnPhishingLinkClicked()
    {
        if (decisionSent)
            return;

        decisionSent = true;

        manager.SelectPhishingLink(
            Time.time - stageStartTime
        );
    }
}
