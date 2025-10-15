using UnityEngine;
using UnityEditor;
using TD;

public class AssignPrefabsToWaves : EditorWindow
{
    [MenuItem("TD/Assign Prefabs to Waves")]
    private static void ShowWindow()
    {
        GetWindow<AssignPrefabsToWaves>("Assign Prefabs");
    }

    private void OnGUI()
    {
        GUILayout.Label("Assign Monster Prefabs to Wave Configs", EditorStyles.boldLabel);

        if (GUILayout.Button("Assign Monster Prefabs to All Waves", GUILayout.Height(40)))
        {
            AssignPrefabsToAllWaves();
        }
    }

    private void AssignPrefabsToAllWaves()
    {
        GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Monster.prefab");
        if (monsterPrefab == null)
        {
            Debug.LogError("[AssignPrefabsToWaves] Monster prefab not found!");
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

        foreach (string path in waveConfigPaths)
        {
            WaveConfig config = AssetDatabase.LoadAssetAtPath<WaveConfig>(path);
            if (config == null)
            {
                Debug.LogWarning($"[AssignPrefabsToWaves] Config not found: {path}");
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

                Debug.Log($"[AssignPrefabsToWaves] Assigned Monster prefab to {config.WaveName}");
            }
            else
            {
                Debug.Log($"[AssignPrefabsToWaves] {config.WaveName} already has enemy spawns configured");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AssignPrefabsToWaves] Prefab assignment complete!");
    }
}
