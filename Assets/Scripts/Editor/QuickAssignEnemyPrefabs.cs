using UnityEngine;
using UnityEditor;
using TD;

public static class QuickAssignEnemyPrefabs
{
    [MenuItem("TD/Automation/Quick Assign Enemy Prefabs")]
    public static void AssignEnemyPrefabs()
    {
        GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Monster.prefab");
        if (monsterPrefab == null)
        {
            Debug.LogError("[QuickAssignEnemyPrefabs] Monster prefab not found!");
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

        int configuredCount = 0;
        int skippedCount = 0;

        foreach (string path in waveConfigPaths)
        {
            WaveConfig config = AssetDatabase.LoadAssetAtPath<WaveConfig>(path);
            if (config == null)
            {
                Debug.LogWarning($"[QuickAssignEnemyPrefabs] Config not found: {path}");
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

                Debug.Log($"[QuickAssignEnemyPrefabs] ✓ Configured {config.WaveName}: {5 + config.WaveNumber * 2} monsters");
                configuredCount++;
            }
            else
            {
                Debug.Log($"[QuickAssignEnemyPrefabs] ⊘ {config.WaveName} already configured, skipping");
                skippedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[QuickAssignEnemyPrefabs] ✓ Complete! Configured: {configuredCount}, Skipped: {skippedCount}");
    }
}
