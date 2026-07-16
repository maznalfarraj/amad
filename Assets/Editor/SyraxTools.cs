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
