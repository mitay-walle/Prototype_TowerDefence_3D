using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using TD;

public static class TestGameplayBootstrap
{
    [MenuItem("TD/Automation/Test Gameplay Bootstrap")]
    public static void TestBootstrap()
    {
        // Find or create GameplayBootstrap in the scene
        GameplayBootstrap bootstrap = Object.FindFirstObjectByType<GameplayBootstrap>();
        
        if (bootstrap == null)
        {
            Debug.LogError("[TestGameplayBootstrap] GameplayBootstrap not found in scene! Add it to the Gameplay scene first.");
            return;
        }

        // Validate required prefabs
        SerializedObject so = new SerializedObject(bootstrap);
        
        GameObject levelRoadPrefab = so.FindProperty("levelRoadPrefab").objectReferenceValue as GameObject;
        GameObject projectilePrefab = so.FindProperty("projectilePrefab").objectReferenceValue as GameObject;
        
        if (levelRoadPrefab == null)
        {
            Debug.LogError("[TestGameplayBootstrap] LevelRoad prefab not assigned!");
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning("[TestGameplayBootstrap] Projectile prefab not assigned. Pool initialization will fail.");
        }

        Debug.Log("[TestGameplayBootstrap] ✓ Starting Gameplay Bootstrap test...");
        Debug.Log($"[TestGameplayBootstrap] Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"[TestGameplayBootstrap] LevelRoad Prefab: {levelRoadPrefab.name}");
        Debug.Log("[TestGameplayBootstrap] Enter Play Mode to test bootstrap sequence");
        
        // Enter play mode
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("TD/Automation/Check Bootstrap Configuration")]
    public static void CheckBootstrapConfig()
    {
        GameplayBootstrap bootstrap = Object.FindFirstObjectByType<GameplayBootstrap>();
        
        if (bootstrap == null)
        {
            Debug.LogError("[TestGameplayBootstrap] GameplayBootstrap not found in scene!");
            return;
        }

        SerializedObject so = new SerializedObject(bootstrap);
        
        Debug.Log("[TestGameplayBootstrap] === Bootstrap Configuration ===");
        
        GameObject levelRoadPrefab = so.FindProperty("levelRoadPrefab").objectReferenceValue as GameObject;
        Debug.Log($"LevelRoad Prefab: {(levelRoadPrefab != null ? levelRoadPrefab.name : "NOT ASSIGNED")}");
        
        int levelSeed = so.FindProperty("levelSeed").intValue;
        bool randomSeed = so.FindProperty("randomSeed").boolValue;
        Debug.Log($"Level Seed: {levelSeed} (Random: {randomSeed})");
        
        GameObject basePrefab = so.FindProperty("basePrefab").objectReferenceValue as GameObject;
        Debug.Log($"Base Prefab: {(basePrefab != null ? basePrefab.name : "NOT ASSIGNED (will create default)")}");
        
        GameObject spawnerPrefab = so.FindProperty("spawnerPrefab").objectReferenceValue as GameObject;
        int spawnerCount = so.FindProperty("spawnerCount").intValue;
        Debug.Log($"Spawner Prefab: {(spawnerPrefab != null ? spawnerPrefab.name : "NOT ASSIGNED")}");
        Debug.Log($"Spawner Count: {spawnerCount}");
        
        ProjectilePool pool = so.FindProperty("projectilePool").objectReferenceValue as ProjectilePool;
        Debug.Log($"ProjectilePool: {(pool != null ? pool.name : "NOT ASSIGNED (will create)")}");
        
        GameObject projectilePrefab = so.FindProperty("projectilePrefab").objectReferenceValue as GameObject;
        Debug.Log($"Projectile Prefab: {(projectilePrefab != null ? projectilePrefab.name : "NOT ASSIGNED")}");
        
        int initialPoolSize = so.FindProperty("initialProjectilePoolSize").intValue;
        int maxPoolSize = so.FindProperty("maxProjectilePoolSize").intValue;
        Debug.Log($"Pool Size: {initialPoolSize} initial / {maxPoolSize} max");
        
        Debug.Log("[TestGameplayBootstrap] ============================");
    }
}
