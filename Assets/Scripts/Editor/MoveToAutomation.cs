using UnityEditor;
using UnityEngine;

public static class MoveToAutomation
{
    [MenuItem("TD/Automation/Reorganize Scripts")]
    public static void MoveScripts()
    {
        string automationFolder = "Assets/Scripts/Editor/Automation";

        if (!AssetDatabase.IsValidFolder(automationFolder))
        {
            AssetDatabase.CreateFolder("Assets/Scripts/Editor", "Automation");
        }

        string[] scriptsToMove = new string[]
        {
            "Assets/Scripts/Editor/CompilationLogger.cs",
            "Assets/Scripts/Editor/WaitForCompilation.cs",
            "Assets/Scripts/Editor/CreateProjectilePrefab.cs",
            "Assets/Scripts/Editor/QuickAssignEnemyPrefabs.cs",
            "Assets/Scripts/Editor/QuickPrefabSetup.cs",
            "Assets/Scripts/Editor/QuickSetupWaveManager.cs",
            "Assets/Scripts/Editor/QuickSetupWaveUI.cs",
            "Assets/Scripts/Editor/QuickWaveSetup.cs",
            "Assets/Scripts/Editor/SetupWaves.cs"
        };

        int movedCount = 0;
        foreach (string scriptPath in scriptsToMove)
        {
            if (System.IO.File.Exists(scriptPath))
            {
                string fileName = System.IO.Path.GetFileName(scriptPath);
                string newPath = $"{automationFolder}/{fileName}";

                string result = AssetDatabase.MoveAsset(scriptPath, newPath);
                if (string.IsNullOrEmpty(result))
                {
                    Debug.Log($"[MoveToAutomation] Moved: {fileName}");
                    movedCount++;
                }
                else
                {
                    Debug.LogWarning($"[MoveToAutomation] Failed to move {fileName}: {result}");
                }
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[MoveToAutomation] ✓ Moved {movedCount} scripts to Automation folder");
    }
}
