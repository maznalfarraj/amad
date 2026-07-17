using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class PiperTTSClient : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Central SYRAX config asset. When assigned, its Piper URL and length scale override the fields below.")]
    [SerializeField]
    private SyraxConfig config;

    [Header("Piper Server (legacy fallback — prefer SyraxConfig)")]
    [SerializeField]
    private string piperServerUrl =
        "http://localhost:5000/synthesize";

    [Header("Audio")]
    [SerializeField]
    private AudioSource outputAudioSource;


public bool IsSpeaking => isSpeaking;

    [SerializeField]
    private float lengthScale = 1f;

    private bool isSpeaking;


    private string EffectiveUrl =>
        config != null && !string.IsNullOrWhiteSpace(config.piperSynthesizeUrl)
            ? config.piperSynthesizeUrl
            : piperServerUrl;

    private float EffectiveLengthScale =>
        config != null ? config.piperLengthScale : lengthScale;

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        StopAllCoroutines();

        if (outputAudioSource != null)
        {
            outputAudioSource.Stop();
        }

        StartCoroutine(
            RequestSpeech(text)
        );
    }

    private void Start()
    {
        // Startup test speech is opt-in via SyraxConfig (was previously
        // unconditional, which made the scene talk on every launch).
        if (config != null && config.piperStartupTest)
        {
            Speak("Piper text to speech is ready.");
        }
    }

    public void StopSpeaking()
    {
        StopAllCoroutines();

        isSpeaking = false;

        if (outputAudioSource != null)
        {
            outputAudioSource.Stop();
        }
    }

    private IEnumerator RequestSpeech(
        string text
    )
    {
        if (outputAudioSource == null)
        {
            Debug.LogError(
                "Piper output AudioSource is missing."
            );

            yield break;
        }

        isSpeaking = true;

        PiperRequest body =
            new PiperRequest
            {
                text = text,
                length_scale = EffectiveLengthScale
            };

        string json =
            JsonUtility.ToJson(body);

        using UnityWebRequest request =
            new UnityWebRequest(
                EffectiveUrl,
                UnityWebRequest.kHttpVerbPOST
            );

        byte[] bodyBytes =
            System.Text.Encoding.UTF8.GetBytes(
                json
            );

        request.uploadHandler =
            new UploadHandlerRaw(
                bodyBytes
            );

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        request.timeout = 20;

        yield return request.SendWebRequest();

        if (
            request.result !=
            UnityWebRequest.Result.Success
        )
        {
            isSpeaking = false;

            Debug.LogError(
                $"Piper error: {request.error}\n" +
                $"{request.downloadHandler?.text}"
            );

            yield break;
        }

        byte[] wavBytes =
            request.downloadHandler.data;

        if (
            wavBytes == null ||
            wavBytes.Length == 0
        )
        {
            isSpeaking = false;

            Debug.LogError(
                "Piper returned empty audio data."
            );

            yield break;
        }

        AudioClip generatedClip;

        try
        {
            generatedClip =
                WavUtility.ToAudioClip(
                    wavBytes,
                    "PiperSpeech"
                );
        }
        catch (Exception exception)
        {
            isSpeaking = false;

            Debug.LogError(
                $"Could not decode Piper WAV: " +
                $"{exception.Message}"
            );

            yield break;
        }

        if (generatedClip == null)
        {
            isSpeaking = false;

            Debug.LogError(
                "Failed to create AudioClip from Piper response."
            );

            yield break;
        }

        outputAudioSource.clip =
            generatedClip;

        outputAudioSource.Play();

        yield return new WaitWhile(
            () =>
                outputAudioSource != null &&
                outputAudioSource.isPlaying
        );

        isSpeaking = false;
    }
}
   
[Serializable]
public class PiperRequest
{
    public string text;
    public float length_scale = 1f;

}