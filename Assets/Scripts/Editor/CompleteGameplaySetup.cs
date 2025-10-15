using UnityEngine;
using UnityEditor;
using TD;

public static class CompleteGameplaySetup
{
    [MenuItem("TD/Automation/Complete Gameplay Setup")]
    public static void SetupAll()
    {
        Debug.Log("[CompleteGameplaySetup] Starting complete gameplay setup...");

        int steps = 0;

        // Step 1: Configure GameplayBootstrap
        Debug.Log("[CompleteGameplaySetup] Step 1/5: Configuring GameplayBootstrap...");
        ConfigureGameplayBootstrap.Configure();
        steps++;

        // Step 2: Setup WaveManager
        Debug.Log("[CompleteGameplaySetup] Step 2/5: Setting up WaveManager...");
        QuickSetupWaveManager.SetupWaveManager();
        steps++;

        // Step 3: Setup Wave UI
        Debug.Log("[CompleteGameplaySetup] Step 3/5: Setting up Wave UI...");
        QuickSetupWaveUI.SetupWaveUI();
        steps++;

        // Step 4: Create Projectile Prefab
        Debug.Log("[CompleteGameplaySetup] Step 4/5: Creating Projectile prefab...");
        CreateProjectilePrefab.CreatePrefab();
        steps++;

        // Step 5: Setup Waves
        Debug.Log("[CompleteGameplaySetup] Step 5/5: Setting up wave configurations...");
        SetupWaves.SetupAllWaves();
        steps++;

        // Save scene
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        Debug.Log($"[CompleteGameplaySetup] ✓ Complete! Finished {steps}/5 steps");
        Debug.Log("[CompleteGameplaySetup] ====================================");
        Debug.Log("[CompleteGameplaySetup] Ready to test:");
        Debug.Log("[CompleteGameplaySetup] - Use 'TD/Automation/Check Bootstrap Configuration' to verify setup");
        Debug.Log("[CompleteGameplaySetup] - Use 'TD/Automation/Test Gameplay Bootstrap' to enter Play Mode");
        Debug.Log("[CompleteGameplaySetup] ====================================");
    }
}
