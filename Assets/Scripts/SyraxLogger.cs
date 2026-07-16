using UnityEngine;

/// <summary>
/// Central SYRAX debug logging. All SYRAX scripts log through this so the
/// whole system can be silenced from one flag on SyraxConfig.
/// </summary>
public static class SyraxLogger
{
    /// <summary>Set once by SyraxExperienceManager on Awake from SyraxConfig.</summary>
    public static bool Verbose = true;

    private const string Prefix = "[SYRAX] ";

    public static void Log(string message)
    {
        if (Verbose)
            Debug.Log(Prefix + message);
    }

    public static void Warn(string message)
    {
        if (Verbose)
            Debug.LogWarning(Prefix + message);
    }

    /// <summary>Errors always log, regardless of the verbose flag.</summary>
    public static void Error(string message)
    {
        Debug.LogError(Prefix + message);
    }
}
