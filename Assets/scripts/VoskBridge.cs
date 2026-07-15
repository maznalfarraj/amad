using UnityEngine;

public class VoskBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private VoskSpeechToText voskSpeechToText;

    [SerializeField]
    private SyraxExperienceManager manager;

    private bool acceptingSpeech;

    private void OnEnable()
    {
        if (voskSpeechToText != null)
        {
            voskSpeechToText.OnTranscriptionResult +=
                HandleTranscriptionResult;
        }
    }

    private void OnDisable()
    {
        if (voskSpeechToText != null)
        {
            voskSpeechToText.OnTranscriptionResult -=
                HandleTranscriptionResult;
        }
    }

    public void StartAcceptingSpeech()
    {
        acceptingSpeech = true;

        Debug.Log(
            "Vosk Bridge: accepting player speech."
        );
    }

    public void StopAcceptingSpeech()
    {
        acceptingSpeech = false;

        Debug.Log(
            "Vosk Bridge: stopped accepting player speech."
        );
    }

    private void HandleTranscriptionResult(
        string json
    )
    {
        if (!acceptingSpeech)
            return;

        if (manager == null)
        {
            Debug.LogError(
                "VoskBridge: SyraxExperienceManager is missing."
            );

            return;
        }

        manager.OnSpeechRecognized(json);
    }
}