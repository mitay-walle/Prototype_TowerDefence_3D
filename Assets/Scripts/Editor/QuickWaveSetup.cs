using UnityEngine;
using UnityEditor;
using TD;

public class QuickWaveSetup
{
    [MenuItem("TD/Automation/Quick Wave Setup")]
    private static void AssignPrefabsToAllWaves()
    {
        GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Monster.prefab");
        if (monsterPrefab == null)
        {
            Debug.LogError("[QuickWaveSetup] Monster prefab not found at Assets/Prefabs/Enemies/Monster.prefab!");
            return;
        }

        string[] waveConfigPaths = new string[]
        {
            "Assets/Resources/WaveConfigs/Wave_01.asset",
            "Assets/Resources/WaveConfigs/Wave_02.asset",
            "Assets/Resources/WaveConfigs/Wave_03.asset",
            "Assets/Resources/WaveConfigs/Wave_04.asset",
            "Assets/Resources/WaveConfigs/Wave_05.asset"
        };

        int configured = 0;
        foreach (string path in waveConfigPaths)
        {
            WaveConfig config = AssetDatabase.LoadAssetAtPath<WaveConfig>(path);
            if (config == null)
            {
                Debug.LogWarning($"[QuickWaveSetup] Config not found: {path}");
                continue;
            }

            SerializedObject so = new SerializedObject(config);
            SerializedProperty enemySpawnsProperty = so.FindProperty("enemySpawns");

            if (enemySpawnsProperty.arraySize == 0)
            {
                enemySpawnsProperty.InsertArrayElementAtIndex(0);
                SerializedProperty spawnElement = enemySpawnsProperty.GetArrayElementAtIndex(0);

                spawnElement.FindPropertyRelative("enemyPrefab").objectReferenceValue = monsterPrefab;
                spawnElement.FindPropertyRelative("count").intValue = 5 + config.WaveNumber * 2;
                spawnElement.FindPropertyRelative("spawnDelay").floatValue = 1.0f;
                spawnElement.FindPropertyRelative("healthMultiplier").floatValue = 1.0f;
                spawnElement.FindPropertyRelative("speedMultiplier").floatValue = 1.0f;

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(config);

                Debug.Log($"[QuickWaveSetup] Assigned Monster to {config.WaveName} (count: {5 + config.WaveNumber * 2})");
                configured++;
            }
            else
            {
                Debug.Log($"[QuickWaveSetup] {config.WaveName} already configured, skipping");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[QuickWaveSetup] Complete! Configured {configured} wave(s)");
    }
}
