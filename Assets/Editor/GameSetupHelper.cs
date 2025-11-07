using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using TD.GameLoop;
using TD.Towers;
using TD.UI;
using TD.Weapons;

public class GameSetupHelper : OdinEditorWindow
{
	[MenuItem("TD/Game Setup Helper")]
	private static void OpenWindow()
	{
		GetWindow<GameSetupHelper>().Show();
	}

	[Button("Create All Resources", ButtonSizes.Large)]
	[GUIColor(0.4f, 0.8f, 1f)]
	private void CreateAllResources()
	{
		CreateFolders();
		CreateTowerStats();
		CreateWaveConfigs();
		CreatePrefabs();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("All resources created successfully!");
	}

	[Button("Setup Scene", ButtonSizes.Large)]
	[GUIColor(0.4f, 1f, 0.4f)]
	private void SetupScene()
	{
		CreateManagers();
		CreateUI();
		CreateSpawnPoints();
		CreateBase();
		Debug.Log("Scene setup complete!");
	}

	private void CreateFolders()
	{
		if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Towers"))
			AssetDatabase.CreateFolder("Assets/Prefabs", "Towers");

		if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies"))
			AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");

		if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Projectiles"))
			AssetDatabase.CreateFolder("Assets/Prefabs", "Projectiles");

		if (!AssetDatabase.IsValidFolder("Assets/Resources/TowerStats"))
			AssetDatabase.CreateFolder("Assets/Resources", "TowerStats");

		if (!AssetDatabase.IsValidFolder("Assets/Resources/WaveConfigs"))
			AssetDatabase.CreateFolder("Assets/Resources", "WaveConfigs");
	}

	[Button("Create Tower Stats")]
	private void CreateTowerStats()
	{
		CreateTowerStat("BasicTower", 10f, 1f, 8f, 15f, 100, TargetPriority.Nearest);
		CreateTowerStat("SniperTower", 25f, 0.5f, 15f, 25f, 150, TargetPriority.Farthest);
		CreateTowerStat("RapidTower", 5f, 3f, 6f, 20f, 120, TargetPriority.Weakest);
		CreateTowerStat("HeavyTower", 50f, 0.3f, 10f, 10f, 200, TargetPriority.Strongest);
		Debug.Log("Tower stats created!");
	}

	private void CreateTowerStat(string name, float damage, float fireRate, float range, float projectileSpeed, int cost, TargetPriority priority)
	{
		var stats = ScriptableObject.CreateInstance<TowerStats>();
		var path = $"Assets/Resources/TowerStats/{name}.asset";

		var serializedObject = new SerializedObject(stats);
		serializedObject.FindProperty("towerName").stringValue = name;
		serializedObject.FindProperty("damage").floatValue = damage;
		serializedObject.FindProperty("fireRate").floatValue = fireRate;
		serializedObject.FindProperty("range").floatValue = range;
		serializedObject.FindProperty("projectileSpeed").floatValue = projectileSpeed;
		serializedObject.FindProperty("cost").intValue = cost;
		serializedObject.FindProperty("targetPriority").enumValueIndex = (int)priority;
		serializedObject.FindProperty("sellValue").intValue = cost / 2;
		serializedObject.FindProperty("upgradeCost").intValue = cost + 50;
		serializedObject.ApplyModifiedProperties();

		AssetDatabase.CreateAsset(stats, path);
	}

	[Button("Create Wave Configs")]
	private void CreateWaveConfigs()
	{
		for (int i = 1; i <= 5; i++)
		{
			CreateWaveConfig(i);
		}

		Debug.Log("Wave configs created!");
	}

	private void CreateWaveConfig(int waveNumber)
	{
		var config = ScriptableObject.CreateInstance<WaveConfig>();
		var path = $"Assets/Resources/WaveConfigs/Wave_{waveNumber:00}.asset";

		var serializedObject = new SerializedObject(config);
		serializedObject.FindProperty("waveName").stringValue = $"Wave {waveNumber}";
		serializedObject.FindProperty("waveNumber").intValue = waveNumber;
		serializedObject.FindProperty("delayBeforeWave").floatValue = 5f;
		serializedObject.FindProperty("completionReward").intValue = 50 + waveNumber * 25;
		serializedObject.FindProperty("healthScaling").floatValue = 1f + (waveNumber - 1) * 0.15f;
		serializedObject.FindProperty("countScaling").floatValue = 1f + (waveNumber - 1) * 0.1f;
		serializedObject.ApplyModifiedProperties();

		AssetDatabase.CreateAsset(config, path);
	}

	[Button("Create Projectile Prefab")]
	private void CreatePrefabs()
	{
		CreateProjectilePrefab();
		Debug.Log("Projectile prefab created! Create tower and enemy prefabs manually with VoxelGenerator.");
	}

	private void CreateProjectilePrefab()
	{
		var projectileGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		projectileGO.name = "Projectile";
		projectileGO.transform.localScale = Vector3.one * 0.3f;

		var projectile = projectileGO.AddComponent<Projectile>();

		var trail = projectileGO.AddComponent<TrailRenderer>();
		trail.time = 0.3f;
		trail.startWidth = 0.2f;
		trail.endWidth = 0.05f;
		trail.material = new Material(Shader.Find("Sprites/Default"));
		trail.startColor = Color.yellow;
		trail.endColor = new Color(1, 0.5f, 0, 0);

		PrefabUtility.SaveAsPrefabAsset(projectileGO, "Assets/Prefabs/Projectiles/Projectile.prefab");
		DestroyImmediate(projectileGO);
	}

	private void CreateManagers()
	{
		if (FindObjectOfType<GameManager>() == null)
		{
			var managerGO = new GameObject("GameManager");
			managerGO.AddComponent<GameManager>();
		}

		if (FindObjectOfType<ResourceManager>() == null)
		{
			var resourceGO = new GameObject("ResourceManager");
			resourceGO.AddComponent<ResourceManager>();
		}

		if (FindObjectOfType<WaveManager>() == null)
		{
			var waveGO = new GameObject("WaveManager");
			waveGO.AddComponent<WaveManager>();
		}

		if (FindObjectOfType<ProjectilePool>() == null)
		{
			var poolGO = new GameObject("ProjectilePool");
			var pool = poolGO.AddComponent<ProjectilePool>();

			var projectilePrefab = AssetDatabase.LoadAssetAtPath<Projectile>("Assets/Prefabs/Projectiles/Projectile.prefab");
			if (projectilePrefab != null)
			{
				var serializedObject = new SerializedObject(pool);
				serializedObject.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
				serializedObject.ApplyModifiedProperties();
			}
		}
	}

	private void CreateUI()
	{
		if (FindObjectOfType<Canvas>() == null)
		{
			var canvasGO = new GameObject("Canvas");
			var canvas = canvasGO.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
			canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

			canvasGO.AddComponent<GameHUD>();

			Debug.Log("Canvas created! Add UI elements manually and assign to GameHUD.");
		}
	}

	private void CreateSpawnPoints()
	{
		if (GameObject.Find("SpawnPoints") == null)
		{
			var spawnParent = new GameObject("SpawnPoints");

			for (int i = 0; i < 3; i++)
			{
				var spawnPoint = new GameObject($"SpawnPoint_{i + 1}");
				spawnPoint.transform.SetParent(spawnParent.transform);
				spawnPoint.transform.position = new Vector3(-20 + i * 20, 0, -20);
				spawnPoint.tag = "SpawnPoint";
			}
		}
	}

	private void CreateBase()
	{
		if (FindObjectOfType<Base>() == null)
		{
			var baseGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
			baseGO.name = "PlayerBase";
			baseGO.transform.position = new Vector3(0, 0, 20);
			baseGO.transform.localScale = new Vector3(5, 3, 5);
			baseGO.GetComponent<Renderer>().material.color = Color.green;
			baseGO.AddComponent<Base>();
		}
	}
}