using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using TD;

public class QuickPrefabSetup : EditorWindow
{
    [MenuItem("TD/Automation/Quick Prefab Setup")]
    private static void ShowWindow()
    {
        GetWindow<QuickPrefabSetup>("Prefab Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Configure Prefabs", EditorStyles.boldLabel);

        if (GUILayout.Button("Configure Monster Prefabs", GUILayout.Height(40)))
        {
            ConfigureMonsterPrefabs();
        }

        if (GUILayout.Button("Configure Turret Prefabs", GUILayout.Height(40)))
        {
            ConfigureTurretPrefabs();
        }

        if (GUILayout.Button("Configure ALL Prefabs", GUILayout.Height(50)))
        {
            ConfigureMonsterPrefabs();
            ConfigureTurretPrefabs();
        }

        GUILayout.Space(20);
        GUILayout.Label("Wave Configuration", EditorStyles.boldLabel);

        if (GUILayout.Button("Assign Monsters to WaveConfigs", GUILayout.Height(40)))
        {
            AssignMonstersToWaves();
        }
    }

    private void ConfigureMonsterPrefabs()
    {
        string[] prefabPaths = new string[]
        {
            "Assets/Prefabs/Monster.prefab",
            "Assets/Prefabs/Monster_1.prefab",
            "Assets/Prefabs/Monster_2.prefab"
        };

        foreach (string path in prefabPaths)
        {
            ConfigureMonster(path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[QuickPrefabSetup] Monster prefabs configured!");
    }

    private void ConfigureMonster(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"[QuickPrefabSetup] Prefab not found: {path}");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        if (instance.GetComponent<NavMeshAgent>() == null)
        {
            var agent = instance.AddComponent<NavMeshAgent>();
            agent.speed = 3f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.5f;
            agent.autoBraking = true;
            agent.radius = 0.5f;
            agent.height = 2f;
        }

        if (instance.GetComponent<EnemyHealth>() == null)
        {
            var health = instance.AddComponent<EnemyHealth>();
            SerializedObject so = new SerializedObject(health);
            so.FindProperty("maxHealth").floatValue = 100f;
            so.FindProperty("deathDelay").floatValue = 0.5f;
            so.FindProperty("giveReward").boolValue = true;
            so.FindProperty("rewardAmount").intValue = 10;
            so.FindProperty("changeColorOnDamage").boolValue = true;
            so.FindProperty("colorChangeDuration").floatValue = 0.2f;
            so.ApplyModifiedProperties();
        }

        if (instance.GetComponent<MoveToBase>() == null)
        {
            instance.AddComponent<MoveToBase>();
        }

        var collider = instance.GetComponent<Collider>();
        if (collider == null)
        {
            var capsule = instance.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;
            capsule.center = new Vector3(0, 1, 0);
        }

        PrefabUtility.SaveAsPrefabAsset(instance, path);
        DestroyImmediate(instance);

        Debug.Log($"[QuickPrefabSetup] Configured: {prefab.name}");
    }

    private void ConfigureTurretPrefabs()
    {
        string[] prefabPaths = new string[]
        {
            "Assets/Prefabs/Turret.prefab",
            "Assets/Prefabs/Turret (1).prefab",
            "Assets/Prefabs/Turret (2).prefab",
            "Assets/Prefabs/Turret (3).prefab",
            "Assets/Prefabs/Turret (4).prefab",
            "Assets/Prefabs/Turret (5).prefab"
        };

        TowerStats basicStats = AssetDatabase.LoadAssetAtPath<TowerStats>("Assets/Resources/TowerStats/BasicTower.asset");
        if (basicStats == null)
        {
            Debug.LogError("[QuickPrefabSetup] BasicTower.asset not found!");
            return;
        }

        foreach (string path in prefabPaths)
        {
            ConfigureTurret(path, basicStats);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[QuickPrefabSetup] Turret prefabs configured!");
    }

    private void ConfigureTurret(string path, TowerStats stats)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"[QuickPrefabSetup] Prefab not found: {path}");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        Transform turretPart = instance.transform.Find("Turret");

        var turret = instance.GetComponent<Turret>();
        if (turret == null)
        {
            turret = instance.AddComponent<Turret>();
        }

        SerializedObject so = new SerializedObject(turret);
        so.FindProperty("stats").objectReferenceValue = stats;
        so.FindProperty("turretRotationPart").objectReferenceValue = turretPart;
        so.FindProperty("rotationSpeed").floatValue = 180f;
        so.FindProperty("showRange").boolValue = true;
        so.FindProperty("rangeColor").colorValue = new Color(1, 0, 0, 0.3f);
        so.FindProperty("Logs").boolValue = false;
        so.ApplyModifiedProperties();

        var collider = instance.GetComponent<Collider>();
        if (collider == null)
        {
            var sphere = instance.AddComponent<SphereCollider>();
            sphere.radius = 1f;
            sphere.center = Vector3.up;
        }

        PrefabUtility.SaveAsPrefabAsset(instance, path);
        DestroyImmediate(instance);

        Debug.Log($"[QuickPrefabSetup] Configured: {prefab.name}");
    }

    private void AssignMonstersToWaves()
    {
        GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Monster.prefab");
        if (monsterPrefab == null)
        {
            Debug.LogError("[QuickPrefabSetup] Monster prefab not found at Assets/Prefabs/Enemies/Monster.prefab!");
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
                Debug.LogWarning($"[QuickPrefabSetup] WaveConfig not found: {path}");
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

                Debug.Log($"[QuickPrefabSetup] Assigned Monster to {config.WaveName} (count: {5 + config.WaveNumber * 2})");
                configured++;
            }
            else
            {
                Debug.Log($"[QuickPrefabSetup] {config.WaveName} already configured, skipping");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[QuickPrefabSetup] Complete! Configured {configured} wave(s)");
    }
}
