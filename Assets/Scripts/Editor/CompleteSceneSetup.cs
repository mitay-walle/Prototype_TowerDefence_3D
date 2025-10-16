using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

namespace TD.Editor
{
    public class CompleteSceneSetup
    {
        private const string TOOLTIP_SETUP_ALL = "Automatically configure all scene references and components";
        private const string TOOLTIP_VALIDATE = "Check scene for missing references and configuration issues";

        [MenuItem("Automation/Complete Scene Setup", priority = 100)]
        public static void SetupScene()
        {
            Debug.Log("[CompleteSceneSetup] Starting comprehensive scene setup...");

            ConfigureProjectilePool();
            ConfigureTowerPrefabs();
            ConfigureEnemyPrefabs();
            ConfigureWaveManager();
            ValidateAllSystems();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("[CompleteSceneSetup] ✓✓✓ Scene setup complete!");
        }

        [MenuItem("Automation/Validate Scene Configuration", priority = 101)]
        public static void ValidateScene()
        {
            Debug.Log("[ValidateScene] Starting validation...");
            ValidateAllSystems();
        }

        private static void ConfigureProjectilePool()
        {
            var pool = Object.FindFirstObjectByType<ProjectilePool>();
            if (pool == null)
            {
                Debug.LogError("[Setup] ProjectilePool not found in scene!");
                return;
            }

            var so = new SerializedObject(pool);
            var prefabProp = so.FindProperty("projectilePrefab");

            if (prefabProp.objectReferenceValue == null)
            {
                string[] guids = AssetDatabase.FindAssets("Projectile t:Prefab", new[] { "Assets/Prefabs" });
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    if (prefab != null)
                    {
                        var projectileComponent = prefab.GetComponent<Projectile>();
                        if (projectileComponent != null)
                        {
                            prefabProp.objectReferenceValue = projectileComponent;
                            so.ApplyModifiedProperties();
                            Debug.Log($"[Setup] ✓ Assigned Projectile prefab: {path}");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[Setup] No Projectile prefab found in Assets/Prefabs!");
                }
            }
            else
            {
                Debug.Log("[Setup] ✓ ProjectilePool already configured");
            }
        }

        private static void ConfigureTowerPrefabs()
        {
            var shopUI = Object.FindFirstObjectByType<TowerShopUI>();
            if (shopUI == null)
            {
                Debug.LogWarning("[Setup] TowerShopUI not found in scene");
                return;
            }

            var so = new SerializedObject(shopUI);
            var towersProp = so.FindProperty("towers");

            if (towersProp.arraySize == 0)
            {
                Debug.Log("[Setup] Loading tower prefabs...");

                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Towers" });

                if (guids.Length > 0)
                {
                    towersProp.ClearArray();

                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                        if (prefab != null && prefab.GetComponent<Turret>() != null)
                        {
                            int index = towersProp.arraySize;
                            towersProp.InsertArrayElementAtIndex(index);
                            towersProp.GetArrayElementAtIndex(index).objectReferenceValue = prefab;
                            Debug.Log($"[Setup] Added tower: {prefab.name}");
                        }
                    }

                    so.ApplyModifiedProperties();
                    Debug.Log($"[Setup] ✓ Configured {towersProp.arraySize} tower prefabs");
                }
                else
                {
                    Debug.LogWarning("[Setup] No tower prefabs found in Assets/Prefabs/Towers!");
                }
            }
            else
            {
                Debug.Log($"[Setup] ✓ TowerShopUI already has {towersProp.arraySize} towers configured");
            }
        }

        private static void ConfigureEnemyPrefabs()
        {
            var waveConfigs = AssetDatabase.FindAssets("t:WaveConfig", new[] { "Assets/Resources/WaveConfigs" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<WaveConfig>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(wc => wc != null)
                .ToList();

            if (waveConfigs.Count == 0)
            {
                Debug.LogWarning("[Setup] No WaveConfig assets found!");
                return;
            }

            string[] enemyGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemies" });

            if (enemyGuids.Length == 0)
            {
                Debug.LogWarning("[Setup] No enemy prefabs found!");
                return;
            }

            var enemyPrefabs = enemyGuids
                .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(go => go != null && go.GetComponent<EnemyHealth>() != null)
                .ToList();

            Debug.Log($"[Setup] Found {enemyPrefabs.Count} enemy prefabs");

            foreach (var waveConfig in waveConfigs)
            {
                var so = new SerializedObject(waveConfig);
                var enemySpawnsProp = so.FindProperty("enemySpawns");

                if (enemySpawnsProp != null && enemySpawnsProp.arraySize == 0 && enemyPrefabs.Count > 0)
                {
                    enemySpawnsProp.ClearArray();

                    foreach (var prefab in enemyPrefabs)
                    {
                        int index = enemySpawnsProp.arraySize;
                        enemySpawnsProp.InsertArrayElementAtIndex(index);

                        var spawnDataElement = enemySpawnsProp.GetArrayElementAtIndex(index);
                        var enemyPrefabProp = spawnDataElement.FindPropertyRelative("enemyPrefab");
                        var countProp = spawnDataElement.FindPropertyRelative("count");
                        var delayProp = spawnDataElement.FindPropertyRelative("spawnDelay");

                        if (enemyPrefabProp != null)
                        {
                            enemyPrefabProp.objectReferenceValue = prefab;
                            if (countProp != null) countProp.intValue = 5;
                            if (delayProp != null) delayProp.floatValue = 0.5f;
                        }
                    }

                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(waveConfig);
                    Debug.Log($"[Setup] ✓ Configured {waveConfig.name} with {enemyPrefabs.Count} enemy spawn entries");
                }
                else if (enemySpawnsProp != null && enemySpawnsProp.arraySize > 0)
                {
                    Debug.Log($"[Setup] ✓ {waveConfig.name} already has {enemySpawnsProp.arraySize} enemy spawn entries");
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static void ConfigureWaveManager()
        {
            var waveManager = Object.FindFirstObjectByType<WaveManager>();
            if (waveManager == null)
            {
                Debug.LogWarning("[Setup] WaveManager not found in scene");
                return;
            }

            var so = new SerializedObject(waveManager);
            var wavesProp = so.FindProperty("waves");

            if (wavesProp.arraySize == 0)
            {
                Debug.Log("[Setup] Loading wave configs...");

                string[] guids = AssetDatabase.FindAssets("t:WaveConfig", new[] { "Assets/Resources/WaveConfigs" });

                if (guids.Length > 0)
                {
                    wavesProp.ClearArray();

                    var sortedPaths = guids
                        .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                        .OrderBy(path => path)
                        .ToList();

                    foreach (string path in sortedPaths)
                    {
                        WaveConfig config = AssetDatabase.LoadAssetAtPath<WaveConfig>(path);

                        if (config != null)
                        {
                            int index = wavesProp.arraySize;
                            wavesProp.InsertArrayElementAtIndex(index);
                            wavesProp.GetArrayElementAtIndex(index).objectReferenceValue = config;
                            Debug.Log($"[Setup] Added wave config: {config.name}");
                        }
                    }

                    so.ApplyModifiedProperties();
                    Debug.Log($"[Setup] ✓ Configured WaveManager with {wavesProp.arraySize} waves");
                }
                else
                {
                    Debug.LogWarning("[Setup] No wave configs found!");
                }
            }
            else
            {
                Debug.Log($"[Setup] ✓ WaveManager already has {wavesProp.arraySize} waves configured");
            }

            var spawnPointsProp = so.FindProperty("spawnPoints");
            if (spawnPointsProp.arraySize == 0)
            {
                var spawnPointsParent = GameObject.Find("SpawnPoints");
                if (spawnPointsParent != null)
                {
                    spawnPointsProp.ClearArray();

                    for (int i = 0; i < spawnPointsParent.transform.childCount; i++)
                    {
                        Transform child = spawnPointsParent.transform.GetChild(i);
                        int index = spawnPointsProp.arraySize;
                        spawnPointsProp.InsertArrayElementAtIndex(index);
                        spawnPointsProp.GetArrayElementAtIndex(index).objectReferenceValue = child;
                    }

                    so.ApplyModifiedProperties();
                    Debug.Log($"[Setup] ✓ Configured {spawnPointsProp.arraySize} spawn points");
                }
            }
            else
            {
                Debug.Log($"[Setup] ✓ WaveManager already has {spawnPointsProp.arraySize} spawn points");
            }
        }

        private static void ValidateAllSystems()
        {
            Debug.Log("[Validate] Checking all systems...");

            bool allValid = true;

            allValid &= ValidateComponent<ProjectilePool>("ProjectilePool");
            allValid &= ValidateComponent<WaveManager>("WaveManager");
            allValid &= ValidateComponent<TowerShopUI>("TowerShopUI");
            allValid &= ValidateComponent<TowerPlacementSystem>("TowerPlacementSystem");
            allValid &= ValidateComponent<SelectionSystem>("SelectionSystem");
            allValid &= ValidateComponent<ResourceManager>("ResourceManager");
            allValid &= ValidateComponent<GameManager>("GameManager");

            if (allValid)
            {
                Debug.Log("[Validate] ✓✓✓ All systems validated successfully!");
            }
            else
            {
                Debug.LogWarning("[Validate] Some systems have missing references - run 'Complete Scene Setup' to fix");
            }
        }

        private static bool ValidateComponent<T>(string name) where T : Component
        {
            var component = Object.FindFirstObjectByType<T>();
            if (component == null)
            {
                Debug.LogError($"[Validate] ✗ {name} not found in scene!");
                return false;
            }

            Debug.Log($"[Validate] ✓ {name} found");
            return true;
        }
    }
}
