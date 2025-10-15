using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine.AI;
using TD;

public class PrefabConfigurator : OdinEditorWindow
{
    [MenuItem("TD/Prefab Configurator")]
    private static void OpenWindow()
    {
        GetWindow<PrefabConfigurator>().Show();
    }

    [Button("Configure ALL Prefabs", ButtonSizes.Large)]
    [GUIColor(0.4f, 1f, 0.4f)]
    private void ConfigureAllPrefabs()
    {
        ConfigureMonsterPrefabs();
        ConfigureTurretPrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PrefabConfigurator] All prefabs configured!");
    }

    [Button("Configure Monster Prefabs")]
    private void ConfigureMonsterPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("Monster t:Prefab", new[] { "Assets/Prefabs" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

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

            Debug.Log($"[PrefabConfigurator] Configured monster: {prefab.name}");
        }
    }

    [Button("Configure Turret Prefabs")]
    private void ConfigureTurretPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("Turret t:Prefab", new[] { "Assets/Prefabs" });

        TowerStats basicStats = AssetDatabase.LoadAssetAtPath<TowerStats>("Assets/Resources/TowerStats/BasicTower.asset");
        if (basicStats == null)
        {
            Debug.LogWarning("[PrefabConfigurator] No TowerStats found! Create them first.");
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            Transform turretPart = instance.transform.Find("Turret");

            var turret = instance.GetComponent<Turret>();
            if (turret == null)
            {
                turret = instance.AddComponent<Turret>();
            }

            SerializedObject so = new SerializedObject(turret);
            so.FindProperty("stats").objectReferenceValue = basicStats;
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

            Debug.Log($"[PrefabConfigurator] Configured turret: {prefab.name}");
        }
    }

    [Button("Create Configuration Assets")]
    private void CreateConfigAssets()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/TowerStats"))
            AssetDatabase.CreateFolder("Assets/Resources", "TowerStats");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/WaveConfigs"))
            AssetDatabase.CreateFolder("Assets/Resources", "WaveConfigs");

        CreateTowerStatsAsset("BasicTower", 15f, 1f, 10f, 20f, 100);
        CreateTowerStatsAsset("SniperTower", 30f, 0.5f, 15f, 30f, 150);
        CreateTowerStatsAsset("RapidTower", 8f, 2.5f, 8f, 25f, 120);

        for (int i = 1; i <= 5; i++)
        {
            CreateWaveConfigAsset(i);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PrefabConfigurator] Configuration assets created!");
    }

    private void CreateTowerStatsAsset(string name, float damage, float fireRate, float range, float projectileSpeed, int cost)
    {
        string path = $"Assets/Resources/TowerStats/{name}.asset";
        if (AssetDatabase.LoadAssetAtPath<TowerStats>(path) != null)
        {
            Debug.Log($"[PrefabConfigurator] {name} already exists, skipping");
            return;
        }

        var stats = ScriptableObject.CreateInstance<TowerStats>();
        var so = new SerializedObject(stats);
        so.FindProperty("towerName").stringValue = name;
        so.FindProperty("description").stringValue = $"A {name.ToLower()} for defending your base";
        so.FindProperty("damage").floatValue = damage;
        so.FindProperty("fireRate").floatValue = fireRate;
        so.FindProperty("range").floatValue = range;
        so.FindProperty("projectileSpeed").floatValue = projectileSpeed;
        so.FindProperty("targetPriority").enumValueIndex = 0;
        so.FindProperty("predictiveAiming").boolValue = false;
        so.FindProperty("cost").intValue = cost;
        so.FindProperty("sellValue").intValue = cost / 2;
        so.FindProperty("upgradeCost").intValue = cost + 50;
        so.ApplyModifiedProperties();

        AssetDatabase.CreateAsset(stats, path);
    }

    private void CreateWaveConfigAsset(int waveNumber)
    {
        string path = $"Assets/Resources/WaveConfigs/Wave_{waveNumber:00}.asset";
        if (AssetDatabase.LoadAssetAtPath<WaveConfig>(path) != null)
        {
            Debug.Log($"[PrefabConfigurator] Wave {waveNumber} already exists, skipping");
            return;
        }

        var config = ScriptableObject.CreateInstance<WaveConfig>();
        var so = new SerializedObject(config);
        so.FindProperty("waveName").stringValue = $"Wave {waveNumber}";
        so.FindProperty("waveNumber").intValue = waveNumber;
        so.FindProperty("delayBeforeWave").floatValue = 5f;
        so.FindProperty("completionReward").intValue = 50 + waveNumber * 30;
        so.FindProperty("healthScaling").floatValue = 1f + (waveNumber - 1) * 0.2f;
        so.FindProperty("countScaling").floatValue = 1f + (waveNumber - 1) * 0.15f;
        so.ApplyModifiedProperties();

        AssetDatabase.CreateAsset(config, path);
    }
}
