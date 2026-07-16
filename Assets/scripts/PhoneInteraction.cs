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
    private GameObject phoneModel;

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
                GetComponent<
                    UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable
                >();
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

        if (phoneModel != null)
        {
            phoneModel.SetActive(true);
        }

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

    public void AnswerNow()
    {
        AnswerInternal();
    }

    private void AnswerInternal()
    {
        if (!waitingForHover)
            return;

        waitingForHover = false;

        
        if (hologram != null)
        {
            hologram.SetActive(false);
        }

        if (phoneModel != null)
        {
            phoneModel.SetActive(false);
        }

        SetPlayerMovement(false);

        if (manager != null)
        {
            manager.ActivateCurrentStage();
        }
        else
        {
            Debug.LogError(
                "PhoneInteraction: manager is missing."
            );
        }
    }
    public void StopRingtone()
{
    if (ringtoneAudioSource != null)
    {
        ringtoneAudioSource.Stop();
    }
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