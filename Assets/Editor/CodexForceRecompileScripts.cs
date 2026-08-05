using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[InitializeOnLoad]
public static class CodexForceRecompileScripts
{
    private const string MenuPath = "MCP/Force Recompile Scripts";
    private const double CompileStartTimeoutSeconds = 30.0;
    private static bool waitingForCompile;
    private static bool compileStarted;
    private static double requestTime;
    private static double compileStartTime;

    static CodexForceRecompileScripts()
    {
        CompilationPipeline.compilationStarted -= OnCompilationStarted;
        CompilationPipeline.compilationFinished -= OnCompilationFinished;
        CompilationPipeline.compilationStarted += OnCompilationStarted;
        CompilationPipeline.compilationFinished += OnCompilationFinished;
    }

    [MenuItem(MenuPath)]
    public static void Recompile()
    {
        double now = EditorApplication.timeSinceStartup;
        if (EditorApplication.isCompiling)
        {
            BeginWatch(now, true);
            Debug.Log("[Codex] Script compilation is already in progress; watching current compilation.");
            return;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Codex] Refreshing AssetDatabase before script recompilation.");

        BeginWatch(now, false);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        CompilationPipeline.RequestScriptCompilation();
        Debug.Log("[Codex] Force script recompilation requested after AssetDatabase refresh.");
    }

    private static void BeginWatch(double now, bool alreadyCompiling)
    {
        waitingForCompile = true;
        compileStarted = alreadyCompiling;
        requestTime = now;
        compileStartTime = alreadyCompiling ? now : 0.0;

        EditorApplication.update -= TrackCompilationStartTimeout;
        EditorApplication.update += TrackCompilationStartTimeout;
    }

    private static void OnCompilationStarted(object context)
    {
        double now = EditorApplication.timeSinceStartup;
        if (!compileStarted)
        {
            compileStarted = true;
            compileStartTime = now;
        }

        if (waitingForCompile)
            Debug.Log($"[Codex] Script compilation started after {now - requestTime:0.00}s.");
        else
            Debug.Log("[Codex] Script compilation started.");
    }

    private static void OnCompilationFinished(object context)
    {
        double now = EditorApplication.timeSinceStartup;
        double elapsed = compileStartTime > 0.0 ? now - compileStartTime : 0.0;

        if (waitingForCompile)
            Debug.Log($"[Codex] Script recompilation finished in {elapsed:0.00}s.");
        else
            Debug.Log($"[Codex] Script compilation finished in {elapsed:0.00}s.");

        FinishWatch();
    }

    private static void TrackCompilationStartTimeout()
    {
        if (!waitingForCompile || compileStarted)
            return;

        double elapsed = EditorApplication.timeSinceStartup - requestTime;
        if (elapsed < CompileStartTimeoutSeconds)
            return;

        Debug.LogWarning($"[Codex] Script recompilation did not start within {CompileStartTimeoutSeconds:0.00}s after AssetDatabase refresh.");
        FinishWatch();
    }

    private static void FinishWatch()
    {
        waitingForCompile = false;
        compileStarted = false;
        requestTime = 0.0;
        compileStartTime = 0.0;
        EditorApplication.update -= TrackCompilationStartTimeout;
    }
}
