using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PhoneInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private SyraxExperienceManager manager;

    [SerializeField]
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable phoneInteractable;

    [SerializeField]
    private GameObject hologram;

    [SerializeField]
    private AudioSource ringtoneAudioSource;

    [Header("Player Movement")]
    [SerializeField]
    private GameObject[] locomotionObjects;

    private bool waitingForHover;

    private void Awake()
    {
        if (phoneInteractable == null)
        {
            phoneInteractable =
                GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        }
    }

    private void OnEnable()
    {
        if (phoneInteractable != null)
        {
            phoneInteractable.hoverEntered.AddListener(
                OnPhoneHovered
            );
        }
    }

    private void OnDisable()
    {
        if (phoneInteractable != null)
        {
            phoneInteractable.hoverEntered.RemoveListener(
                OnPhoneHovered
            );
        }
    }

    public void StartPhoneCallInteraction()
    {
        waitingForHover = true;

        if (hologram != null)
        {
            hologram.SetActive(true);
        }

        if (
            ringtoneAudioSource != null &&
            ringtoneAudioSource.clip != null
        )
        {
            ringtoneAudioSource.loop = true;
            ringtoneAudioSource.Play();
        }
    }

    private void OnPhoneHovered(
        HoverEnterEventArgs args
    )
    {
        AnswerInternal();
    }

    /// <summary>
    /// Simulates the XR hover that answers the ringing phone. Used by
    /// SyraxDebugHotkeys for desktop testing (no headset). Safe to call
    /// any time — it only acts while the phone is actually ringing.
    /// </summary>
    public void AnswerNow()
    {
        AnswerInternal();
    }

    private void AnswerInternal()
    {
        if (!waitingForHover)
            return;

        waitingForHover = false;

        if (ringtoneAudioSource != null)
        {
            ringtoneAudioSource.Stop();
        }

        if (hologram != null)
        {
            hologram.SetActive(false);
        }

        SetPlayerMovement(false);

        manager.ActivateCurrentStage();
    }

    private void SetPlayerMovement(
        bool enabled
    )
    {
        if (locomotionObjects == null)
            return;

        foreach (
            GameObject locomotionObject
            in locomotionObjects
        )
        {
            if (locomotionObject != null)
            {
                locomotionObject.SetActive(
                    enabled
                );
            }
        }
    }
}