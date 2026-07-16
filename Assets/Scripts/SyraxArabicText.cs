using ArabicSupport;

/// <summary>
/// Central Arabic text shaping for SYRAX. TextMeshPro does not shape Arabic
/// natively (letters appear disconnected and left-to-right), so every
/// RUNTIME-set text goes through Fix() before being assigned to a TMP_Text.
///
/// - Latin/empty strings pass through untouched.
/// - Uses ArabicSupport (Arabic Writer) under Assets/ThirdParty/ArabicSupport.
/// - Static texts placed by designers in the editor should be typed already
///   fixed via the same tool, or kept in the scene as images.
/// </summary>
public static class SyraxArabicText
{
    /// <summary>Shape + reorder Arabic for TMP display. Safe on any string.</summary>
    public static string Fix(string text)
    {
        if (string.IsNullOrEmpty(text) || !ContainsArabic(text))
            return text;

        try
        {
            // showTashkeel: false (models rarely emit tashkeel anyway),
            // useHinduNumbers: false (keep 0-9 so the OTP code stays readable).
            return ArabicFixer.Fix(text, false, false);
        }
        catch
        {
            // Never let a shaping failure break gameplay text.
            return text;
        }
    }

    public static bool ContainsArabic(string text)
    {
        foreach (char c in text)
        {
            if (c >= 0x0600 && c <= 0x06FF)
                return true;
        }

        return false;
    }
}
