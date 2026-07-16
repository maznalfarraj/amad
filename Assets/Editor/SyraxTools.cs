using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// SYRAX team editor tools. All entries live under the SYRAX menu:
///  - Validate Scene: scans SYRAX SYSTEM for missing scripts / null references.
///  - Auto Assign References: fills obvious empty references (config, manager).
///  - Backend Health Check: pings the backend /api/health using SyraxConfig.
///  - Select Config: pings/selects the SyraxConfig asset in the Project window.
/// </summary>
public static class SyraxTools
{
    private const string ConfigPath = "Assets/Settings/SyraxConfig.asset";

    // ------------------------------------------------------------------
    [MenuItem("SYRAX/Validate Scene")]
    public static void ValidateScene()
    {
        var root = GameObject.Find("SYRAX SYSTEM");

        if (root == null)
        {
            Debug.LogError("[SYRAX] 'SYRAX SYSTEM' root not found in the open scene.");
            return;
        }

        var sb = new StringBuilder("[SYRAX] Scene validation:\n");
        int issues = 0;

        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null)
            {
                sb.AppendLine("  MISSING SCRIPT somewhere under SYRAX SYSTEM (use Console double-click to locate).");
                issues++;
                continue;
            }

            var type = mb.GetType();
            string ns = type.Namespace ?? "";

            if (ns.StartsWith("Unity") || ns.StartsWith("TMPro"))
                continue;

            var fields = type.GetFields(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);

            foreach (var f in fields)
            {
                bool serialized =
                    f.IsPublic ||
                    System.Attribute.IsDefined(f, typeof(SerializeField));

                if (!serialized) continue;
                if (!typeof(Object).IsAssignableFrom(f.FieldType)) continue;

                if ((Object)f.GetValue(mb) == null)
                {
                    sb.AppendLine($"  NULL REF: {GetPath(mb.transform)} :: {type.Name}.{f.Name}");
                    issues++;
                }
            }
        }

        sb.AppendLine(issues == 0
            ? "  All good — no missing scripts, no null references."
            : $"  {issues} issue(s) found.");

        if (issues == 0) Debug.Log(sb.ToString());
        else Debug.LogWarning(sb.ToString());
    }

    // ------------------------------------------------------------------
    [MenuItem("SYRAX/Auto Assign References")]
    public static void AutoAssignReferences()
    {
        var config = AssetDatabase.LoadAssetAtPath<SyraxConfig>(ConfigPath);
        var manager = Object.FindFirstObjectByType<SyraxExperienceManager>(FindObjectsInactive.Include);
        int fixedCount = 0;

        // Assign the config asset anywhere a 'config' field is empty.
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            var type = mb.GetType();
            string ns = type.Namespace ?? "";
            if (ns.StartsWith("Unity") || ns.StartsWith("TMPro")) continue;

            foreach (var f in type.GetFields(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public))
            {
                bool serialized =
                    f.IsPublic ||
                    System.Attribute.IsDefined(f, typeof(SerializeField));
                if (!serialized) continue;

                object current = f.GetValue(mb);

                if (f.FieldType == typeof(SyraxConfig) && (Object)current == null && config != null)
                {
                    Undo.RecordObject(mb, "SYRAX Auto Assign");
                    f.SetValue(mb, config);
                    EditorUtility.SetDirty(mb);
                    fixedCount++;
                }
                else if (f.FieldType == typeof(SyraxExperienceManager) && (Object)current == null && manager != null)
                {
                    Undo.RecordObject(mb, "SYRAX Auto Assign");
                    f.SetValue(mb, manager);
                    EditorUtility.SetDirty(mb);
                    fixedCount++;
                }
            }
        }

        Debug.Log($"[SYRAX] Auto Assign complete — {fixedCount} reference(s) filled. Save the scene to persist.");
    }

    // ------------------------------------------------------------------
    [MenuItem("SYRAX/Backend Health Check")]
    public static void BackendHealthCheck()
    {
        var config = AssetDatabase.LoadAssetAtPath<SyraxConfig>(ConfigPath);
        string baseUrl = config != null ? config.backendBaseUrl.TrimEnd('/') : "http://localhost:3000";
        string url = baseUrl + "/api/health";

        var request = UnityWebRequest.Get(url);
        request.timeout = 5;
        var op = request.SendWebRequest();

        EditorApplication.CallbackFunction poll = null;
        poll = () =>
        {
            if (!op.isDone) return;
            EditorApplication.update -= poll;

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log($"[SYRAX] Backend ONLINE at {url}\n{request.downloadHandler.text}");
            else
                Debug.LogError($"[SYRAX] Backend UNREACHABLE at {url}: {request.error}\nStart it with 'node server.js' (see backend README).");

            request.Dispose();
        };
        EditorApplication.update += poll;
    }

    // ------------------------------------------------------------------
    [MenuItem("SYRAX/Validate Quest Build")]
    public static void ValidateQuestBuild()
    {
        var sb = new StringBuilder("[SYRAX] Quest 3 build readiness:\n");
        int problems = 0;

        // 1. Platform
        bool android =
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
        Append(sb, android,
            "Active platform is Android",
            "Active platform is NOT Android — File > Build Settings > Android > Switch Platform",
            ref problems);

        // 2. Scene in build settings
        bool sceneInBuild = false;
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled && s.path.EndsWith("AmadMazen.unity")) sceneInBuild = true;
        Append(sb, sceneInBuild,
            "AmadMazen scene is in Build Settings",
            "AmadMazen scene is MISSING from Build Settings",
            ref problems);

        // 3. IL2CPP + ARM64
        var group = UnityEditor.Build.NamedBuildTarget.Android;
        bool il2cpp = PlayerSettings.GetScriptingBackend(group) == ScriptingImplementation.IL2CPP;
        bool arm64 = (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0;
        Append(sb, il2cpp, "Scripting backend is IL2CPP", "Scripting backend must be IL2CPP for Quest", ref problems);
        Append(sb, arm64, "Target architecture includes ARM64", "ARM64 must be enabled for Quest", ref problems);

        // 4. Vosk model in root StreamingAssets
        bool model = System.IO.File.Exists(
            Application.dataPath + "/StreamingAssets/vosk-model-small-en-us-0.15.zip");
        Append(sb, model,
            "Vosk model zip is in Assets/StreamingAssets",
            "Vosk model zip NOT found in Assets/StreamingAssets",
            ref problems);

        // 5. Meta Quest OpenXR feature
        bool quest = false;
        string openXr = Application.dataPath + "/XR/Settings/OpenXRPackageSettings.asset";
        if (System.IO.File.Exists(openXr))
        {
            string text = System.IO.File.ReadAllText(openXr);
            int i = text.IndexOf("MetaQuestFeature Android");
            quest = i >= 0 && text.IndexOf("m_enabled: 1", i) - i < 400 && text.IndexOf("m_enabled: 1", i) >= 0;
        }
        Append(sb, quest,
            "OpenXR 'Meta Quest Support' feature enabled for Android",
            "Meta Quest Support feature not confirmed — check Project Settings > XR Plug-in Management > OpenXR (Android tab)",
            ref problems);

        // 6. Config URLs must not be localhost for a headset build
        var config = AssetDatabase.LoadAssetAtPath<SyraxConfig>(ConfigPath);
        if (config != null)
        {
            bool urlsOk =
                !config.backendBaseUrl.Contains("localhost") &&
                !config.backendBaseUrl.Contains("127.0.0.1") &&
                !config.piperSynthesizeUrl.Contains("localhost") &&
                !config.piperSynthesizeUrl.Contains("127.0.0.1");
            Append(sb, urlsOk,
                $"SyraxConfig URLs point to a LAN IP ({config.backendBaseUrl})",
                $"SyraxConfig still uses localhost ({config.backendBaseUrl}) — the Quest cannot reach it. Set the PC's LAN IP (ipconfig) before building.",
                ref problems);
        }
        else
        {
            sb.AppendLine("  ❌ SyraxConfig asset missing!");
            problems++;
        }

        sb.AppendLine(problems == 0
            ? "READY — you can build for Quest 3 now."
            : $"{problems} problem(s) to fix before building.");

        if (problems == 0) Debug.Log(sb.ToString());
        else Debug.LogWarning(sb.ToString());
    }

    private static void Append(StringBuilder sb, bool ok, string good, string bad, ref int problems)
    {
        sb.AppendLine(ok ? $"  ✅ {good}" : $"  ❌ {bad}");
        if (!ok) problems++;
    }

    // ------------------------------------------------------------------
    [MenuItem("SYRAX/Select Config")]
    public static void SelectConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<SyraxConfig>(ConfigPath);

        if (config == null)
        {
            Debug.LogError($"[SYRAX] Config asset not found at {ConfigPath}. Create it via Assets > Create > SYRAX > Config.");
            return;
        }

        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
