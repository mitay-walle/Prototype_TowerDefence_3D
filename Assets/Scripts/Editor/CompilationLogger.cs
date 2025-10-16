using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[InitializeOnLoad]
public static class CompilationLogger
{
    private static System.DateTime compilationStartTime;

    static CompilationLogger()
    {
        CompilationPipeline.compilationStarted += OnCompilationStarted;
        CompilationPipeline.compilationFinished += OnCompilationFinished;
        CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
    }

    private static void OnCompilationStarted(object obj)
    {
        compilationStartTime = System.DateTime.Now;
        Debug.Log($"[CompilationLogger] ⏳ Compilation STARTED (Time: {Time.realtimeSinceStartup:F2}s)");
    }

    private static void OnCompilationFinished(object obj)
    {
        var duration = (System.DateTime.Now - compilationStartTime).TotalSeconds;
        Debug.Log($"[CompilationLogger] ✓ Compilation FINISHED ({duration:F2}s) (Time: {Time.realtimeSinceStartup:F2}s)");

        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var hud = UnityEngine.Object.FindObjectOfType<TD.GameHUD>();
                if (hud == null)
                {
                    Debug.Log("[CompilationLogger] GameHUD not found, calling Setup...");
                    var type = System.Type.GetType("TD.Editor.SetupGameHUD, Assembly-CSharp-Editor");
                    if (type != null)
                    {
                        var method = type.GetMethod("Setup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                        if (method != null)
                        {
                            method.Invoke(null, null);
                        }
                    }
                }
            }
        };
    }

    private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
    {
        bool hasErrors = false;
        bool hasWarnings = false;

        foreach (var msg in messages)
        {
            if (msg.type == CompilerMessageType.Error)
                hasErrors = true;
            if (msg.type == CompilerMessageType.Warning)
                hasWarnings = true;
        }

        string status = hasErrors ? "❌ ERRORS" : (hasWarnings ? "⚠️ WARNINGS" : "✓ OK");
        string assemblyName = System.IO.Path.GetFileName(assemblyPath);
        //Debug.Log($"[CompilationLogger] {status} Assembly: {assemblyName}");
    }

    [MenuItem("TD/Automation/Force Recompile All")]
    public static void ForceRecompile()
    {
        Debug.Log($"[CompilationLogger] 🔄 Forcing full recompilation... (Time: {Time.realtimeSinceStartup:F2}s)");
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        CompilationPipeline.RequestScriptCompilation();
    }

    [MenuItem("TD/Automation/Get Current Time")]
    public static void GetCurrentTime()
    {
        Debug.Log($"[CompilationLogger] ⏰ Current Time: {Time.realtimeSinceStartup:F2}s");
    }
}
