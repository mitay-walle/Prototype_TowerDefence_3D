using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace TD
{
	/// <summary>
	/// Single entry point for gameplay initialization.
	/// Bootstrap sequence:
	/// 1. Generate level (LevelRoad)
	/// 2. Bake NavMesh
	/// 3. Place Base and Spawners
	/// 4. Initialize pools
	/// 5. Start game
	/// </summary>
	public class GameplayBootstrap : MonoBehaviour
	{
		[Header("Level Generation")]
		[SerializeField] private GameObject levelRoadPrefab;
		[SerializeField] private int levelSeed = 0;
		[SerializeField] private bool randomSeed = true;

		[Header("Gameplay Objects")]
		[SerializeField] private GameObject basePrefab;
		[SerializeField] private GameObject spawnerPrefab;

		[Header("Pools")]
		[SerializeField] private ProjectilePool projectilePool;
		[SerializeField] private GameObject projectilePrefab;
		[SerializeField] private int initialProjectilePoolSize = 50;
		[SerializeField] private int maxProjectilePoolSize = 200;

		[Header("Debug")]
		[SerializeField] private bool logs = true;

		private GameObject generatedLevel;
		private GameObject playerBase;
		private Transform[] spawnPoints;
		private NavMeshSurface navMeshSurface;

		private void Start()
		{
			StartCoroutine(BootstrapSequence());
		}

		private IEnumerator BootstrapSequence()
		{
			if (logs) Debug.Log("[GameplayBootstrap] Starting bootstrap sequence...");

			// Step 1: Generate Level
			yield return StartCoroutine(GenerateLevel());

			// Step 2: Bake NavMesh
			yield return StartCoroutine(BakeNavMesh());

			// Step 3: Place Base and Spawners
			yield return StartCoroutine(PlaceGameplayObjects());

			// Step 4: Initialize Pools
			yield return StartCoroutine(InitializePools());

			FindAnyObjectByType<GameHUD>().Initialize();
			// Step 5: Finalize
			FinalizeBootstrap();

			if (logs) Debug.Log("[GameplayBootstrap] ✓ Bootstrap complete!");
		}

		private IEnumerator GenerateLevel()
		{
			if (logs) Debug.Log("[GameplayBootstrap] Step 1/5: Generating level...");

			if (levelRoadPrefab == null)
			{
				Debug.LogError("[GameplayBootstrap] LevelRoad prefab not assigned!");
				yield break;
			}

			// Instantiate LevelRoad prefab
			generatedLevel = Instantiate(levelRoadPrefab);
			generatedLevel.transform.position = Vector3.zero;
			generatedLevel.transform.rotation = Quaternion.identity;
			generatedLevel.name = "LevelRoad (Generated)";

			// Find VoxelGenerator and generate
			VoxelGenerator voxelGen = generatedLevel.GetComponentInChildren<VoxelGenerator>();
			if (voxelGen != null)
			{
				if (randomSeed)
				{
					levelSeed = Random.Range(0, 999999);
				}

				// Access profile through reflection or make it public
				var profile = voxelGen.profile as LevelRoadGenerationProfile;
				if (profile != null)
				{
					profile.seed = levelSeed;
				}

				voxelGen.Generate();

				if (logs) Debug.Log($"[GameplayBootstrap] Level generated with seed: {levelSeed}");
			}
			else
			{
				Debug.LogWarning("[GameplayBootstrap] VoxelGenerator not found in LevelRoad prefab");
			}

			yield return new WaitForSeconds(0.5f); // Wait for generation to complete
		}

		private IEnumerator BakeNavMesh()
		{
			if (logs) Debug.Log("[GameplayBootstrap] Step 2/5: Baking NavMesh...");

			// Find or create NavMeshSurface
			navMeshSurface = FindFirstObjectByType<NavMeshSurface>();
			if (navMeshSurface == null)
			{
				GameObject navMeshGO = new GameObject("NavMesh Surface");
				navMeshSurface = navMeshGO.AddComponent<NavMeshSurface>();
			}

			// Build NavMesh
			navMeshSurface.BuildNavMesh();

			if (logs) Debug.Log("[GameplayBootstrap] NavMesh baked");

			yield return null;
		}

		private IEnumerator PlaceGameplayObjects()
		{
			if (logs) Debug.Log("[GameplayBootstrap] Step 3/5: Placing Base and Spawners...");

			// Find level bounds from generated road
			LevelRoadGenerationProfile profile = GetLevelProfile();
			if (profile == null)
			{
				Debug.LogError("[GameplayBootstrap] Could not find level generation profile");
				yield break;
			}

			// Place Base at center (where road network converges)
			PlaceBase(profile.BasePosition);

			// Place Spawners around the edges
			PlaceSpawners(profile);

			yield return null;
		}

		private void PlaceBase(Vector3 position)
		{
			if (basePrefab == null)
			{
				Debug.LogWarning("[GameplayBootstrap] Base prefab not assigned, creating default cube");
				playerBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
				playerBase.transform.localScale = new Vector3(5, 3, 5);
				playerBase.AddComponent<Base>();
			}
			else
			{
				playerBase = Instantiate(basePrefab);
			}

			playerBase.name = "PlayerBase";
			playerBase.transform.position = position + Vector3.up * 0.5f;
			if (logs) Debug.Log($"[GameplayBootstrap] Base placed at {position}");
		}

		private void PlaceSpawners(LevelRoadGenerationProfile profile)
		{
			GameObject spawnersParent = new GameObject("SpawnPoints");
			spawnPoints = new Transform[profile.RoadOuterVoxelsWorldPositions.Count];

			int i = 0;
			foreach (Vector3 spawnPos in profile.RoadOuterVoxelsWorldPositions)
			{
				GameObject spawner = new GameObject($"SpawnPoint_{i + 1}");
				spawner.transform.SetParent(spawnersParent.transform);
				spawner.transform.position = spawnPos;

				spawnPoints[i] = spawner.transform;

				if (logs) Debug.Log($"[GameplayBootstrap] Spawner {i + 1} placed at {spawnPos}");
				i++;
			}

			// Assign spawn points to WaveManager
			AssignSpawnPointsToWaveManager();
		}

		private void AssignSpawnPointsToWaveManager()
		{
			WaveManager waveManager = FindFirstObjectByType<WaveManager>();
			if (waveManager != null)
			{
				waveManager.Initialize(null, spawnPoints);
				if (logs) Debug.Log($"[GameplayBootstrap] Assigned {spawnPoints.Length} spawn points to WaveManager");
			}
		}

		private IEnumerator InitializePools()
		{
			if (logs) Debug.Log("[GameplayBootstrap] Step 4/5: Initializing pools...");

			if (projectilePool == null)
			{
				GameObject poolGO = new GameObject("ProjectilePool");
				projectilePool = poolGO.AddComponent<ProjectilePool>();
			}

			if (projectilePrefab != null)
			{
				Projectile prefabProjectile = projectilePrefab.GetComponent<Projectile>();
				if (prefabProjectile != null)
				{
					projectilePool.Initialize(prefabProjectile, initialProjectilePoolSize, maxProjectilePoolSize);
					if (logs) Debug.Log($"[GameplayBootstrap] Projectile pool initialized ({initialProjectilePoolSize}/{maxProjectilePoolSize})");
				}
			}

			yield return null;
		}

		private void FinalizeBootstrap()
		{
			// Any final initialization
			GameManager gameManager = FindFirstObjectByType<GameManager>();
			if (gameManager != null)
			{
				// Game is ready to start
			}
		}

		private LevelRoadGenerationProfile GetLevelProfile()
		{
			if (generatedLevel == null) return null;

			VoxelGenerator voxelGen = generatedLevel.GetComponentInChildren<VoxelGenerator>();
			if (voxelGen != null)
			{
				return voxelGen.profile as LevelRoadGenerationProfile;
			}

			return null;
		}
	}
}