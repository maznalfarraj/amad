using UnityEngine;

/// <summary>
/// Central SYRAX configuration asset. One place for every URL, timeout and
/// tuning value — nothing is hardcoded in scripts. Designers/devs edit the
/// asset at Assets/Settings/SyraxConfig.asset (create via
/// Assets > Create > SYRAX > Config).
/// </summary>
[CreateAssetMenu(fileName = "SyraxConfig", menuName = "SYRAX/Config")]
public class SyraxConfig : ScriptableObject
{
    [Header("Backend (SYRAX Node server)")]
    [Tooltip("Base URL of the SYRAX backend, no trailing slash. e.g. http://192.168.1.20:3000 when the Quest and PC share Wi-Fi.")]
    public string backendBaseUrl = "http://localhost:3000";

    [Tooltip("Timeout in seconds for AI-backed requests (chat / event / session). AI calls can take several seconds.")]
    [Min(5)] public int requestTimeoutSeconds = 60;

    [Tooltip("How many times a failed request is retried before giving up.")]
    [Range(0, 5)] public int maxRetries = 2;

    [Tooltip("Base delay in seconds between retries (doubles each attempt).")]
    [Min(0.1f)] public float retryBaseDelaySeconds = 1f;

    [Header("Piper TTS server")]
    [Tooltip("Full URL of the Piper synthesize endpoint.")]
    public string piperSynthesizeUrl = "http://localhost:5000/synthesize";

    [Tooltip("Speaking speed. 1 = normal, >1 slower, <1 faster.")]
    [Min(0.25f)] public float piperLengthScale = 1f;

    [Header("Scoring (display only — backend is authoritative)")]
    [Min(1)] public int pointsPerCorrectDecision = 5;
    [Min(1)] public int maximumScore = 15;

    [Header("Debug")]
    [Tooltip("Master switch for SYRAX debug logging (SyraxLogger).")]
    public bool verboseLogging = true;

    [Tooltip("If enabled, Piper speaks a short test phrase on scene start.")]
    public bool piperStartupTest = false;
}
