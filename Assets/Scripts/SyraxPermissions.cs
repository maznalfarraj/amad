using System.Collections;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Quest/Android boot gate: requests the RECORD_AUDIO permission and only
/// starts Vosk speech-to-text AFTER the player grants it. Without this, the
/// very first launch on the headset would start the microphone before the
/// permission dialog is answered and STT would capture silence.
///
/// On PC / in the editor it simply starts Vosk immediately, preserving the
/// current desktop behavior.
///
/// Setup (already done in the scene): VoskSpeechToText.AutoStart must be OFF —
/// this component owns the startup instead.
/// </summary>
public class SyraxPermissions : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VoskSpeechToText voskSpeechToText;

    [Tooltip("How long to wait for the player to answer the permission dialog before giving up (seconds).")]
    [SerializeField, Min(5f)] private float permissionTimeout = 60f;

    private void Start()
    {
        StartCoroutine(BootSequence());
    }

    private IEnumerator BootSequence()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            SyraxLogger.Log("Requesting microphone permission…");
            Permission.RequestUserPermission(Permission.Microphone);

            float waited = 0f;
            while (!Permission.HasUserAuthorizedPermission(Permission.Microphone) &&
                   waited < permissionTimeout)
            {
                waited += 0.25f;
                yield return new WaitForSeconds(0.25f);
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                SyraxLogger.Error(
                    "Microphone permission was not granted — speech recognition is disabled. " +
                    "Grant the permission in the headset settings and restart the app."
                );
                yield break;
            }

            SyraxLogger.Log("Microphone permission granted.");
        }
#endif

        if (voskSpeechToText == null)
        {
            SyraxLogger.Error("SyraxPermissions: VoskSpeechToText reference missing.");
            yield break;
        }

        SyraxLogger.Log("Starting Vosk speech-to-text…");
        voskSpeechToText.StartVoskStt();
    }
}
