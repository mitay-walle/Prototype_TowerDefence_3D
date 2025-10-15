using UnityEngine;
using UnityEditor;
using TD;

public static class SetupWaves
{
    [MenuItem("TD/Automation/Setup All Waves")]
    public static void SetupAllWaves()
    {
        GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Monster.prefab");
        if (monsterPrefab == null)
        {
            Debug.LogError("[SetupWaves] Monster prefab not found!");
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
                Debug.LogWarning($"[SetupWaves] Config not found: {path}");
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

                Debug.Log($"[SetupWaves] ✓ {config.WaveName}: {5 + config.WaveNumber * 2} monsters");
                configured++;
            }
            else
            {
                Debug.Log($"[SetupWaves] - {config.WaveName}: already configured");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SetupWaves] ✓ Complete! Configured {configured} wave(s)");
    }
}
