using UnityEngine;
using UnityEditor;
using TD;
using System.Linq;

public static class QuickSetupWaveManager
{
    [MenuItem("TD/Automation/Quick Setup WaveManager")]
    public static void SetupWaveManager()
    {
        var waveManager = Object.FindFirstObjectByType<WaveManager>();
        if (waveManager == null)
        {
            Debug.LogError("[QuickSetupWaveManager] WaveManager not found in scene!");
            return;
        }

        // Find all WaveConfigs
        string[] waveConfigGuids = AssetDatabase.FindAssets("t:WaveConfig", new[] { "Assets/Resources/WaveConfigs" });
        WaveConfig[] waveConfigs = waveConfigGuids
            .Select(guid => AssetDatabase.LoadAssetAtPath<WaveConfig>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(config => config != null)
            .OrderBy(config => config.WaveNumber)
            .ToArray();

        if (waveConfigs.Length == 0)
        {
            Debug.LogError("[QuickSetupWaveManager] No WaveConfigs found in Assets/Resources/WaveConfigs!");
            return;
        }

        // Find all SpawnPoints
        GameObject spawnPointsParent = GameObject.Find("SpawnPoints");
        if (spawnPointsParent == null)
        {
            Debug.LogError("[QuickSetupWaveManager] SpawnPoints GameObject not found!");
            return;
        }

        Transform[] spawnPoints = new Transform[spawnPointsParent.transform.childCount];
        for (int i = 0; i < spawnPointsParent.transform.childCount; i++)
        {
            spawnPoints[i] = spawnPointsParent.transform.GetChild(i);
        }

        if (spawnPoints.Length == 0)
        {
            Debug.LogError("[QuickSetupWaveManager] No SpawnPoint children found!");
            return;
        }

        // Assign to WaveManager
        SerializedObject so = new SerializedObject(waveManager);
        SerializedProperty wavesProperty = so.FindProperty("waves");
        SerializedProperty spawnPointsProperty = so.FindProperty("spawnPoints");

        wavesProperty.ClearArray();
        for (int i = 0; i < waveConfigs.Length; i++)
        {
            wavesProperty.InsertArrayElementAtIndex(i);
            wavesProperty.GetArrayElementAtIndex(i).objectReferenceValue = waveConfigs[i];
        }

        spawnPointsProperty.ClearArray();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            spawnPointsProperty.InsertArrayElementAtIndex(i);
            spawnPointsProperty.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(waveManager);

        Debug.Log($"[QuickSetupWaveManager] ✓ Assigned {waveConfigs.Length} WaveConfigs");
        Debug.Log($"[QuickSetupWaveManager] ✓ Assigned {spawnPoints.Length} SpawnPoints");
        Debug.Log("[QuickSetupWaveManager] ✓ WaveManager setup complete!");
    }
}
