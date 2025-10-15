using UnityEngine;
using UnityEditor;
using TD;

public static class ConfigureGameplayBootstrap
{
    [MenuItem("TD/Automation/Configure Gameplay Bootstrap")]
    public static void Configure()
    {
        GameplayBootstrap bootstrap = Object.FindFirstObjectByType<GameplayBootstrap>();
        
        if (bootstrap == null)
        {
            Debug.LogError("[ConfigureGameplayBootstrap] GameplayBootstrap not found in scene! Creating...");
            GameObject go = new GameObject("GameplayBootstrap");
            bootstrap = go.AddComponent<GameplayBootstrap>();
        }

        SerializedObject so = new SerializedObject(bootstrap);

        // Load prefabs
        GameObject levelRoadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LevelRoad.prefab");
        GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");

        // Assign prefabs
        if (levelRoadPrefab != null)
        {
            so.FindProperty("levelRoadPrefab").objectReferenceValue = levelRoadPrefab;
            Debug.Log("[ConfigureGameplayBootstrap] ✓ Assigned LevelRoad prefab");
        }
        else
        {
            Debug.LogError("[ConfigureGameplayBootstrap] LevelRoad.prefab not found!");
        }

        if (projectilePrefab != null)
        {
            so.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            Debug.Log("[ConfigureGameplayBootstrap] ✓ Assigned Projectile prefab");
        }
        else
        {
            Debug.LogWarning("[ConfigureGameplayBootstrap] Projectile.prefab not found!");
        }

        // Configure settings
        so.FindProperty("randomSeed").boolValue = true;
        so.FindProperty("levelSeed").intValue = 0;
        so.FindProperty("spawnerCount").intValue = 3;
        so.FindProperty("initialProjectilePoolSize").intValue = 50;
        so.FindProperty("maxProjectilePoolSize").intValue = 200;
        so.FindProperty("logs").boolValue = true;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(bootstrap);

        Debug.Log("[ConfigureGameplayBootstrap] ✓ GameplayBootstrap configured successfully!");
        Debug.Log("[ConfigureGameplayBootstrap] Ready to test with 'TD/Automation/Test Gameplay Bootstrap'");
    }
}
