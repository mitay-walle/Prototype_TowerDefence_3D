using System.Collections;
using Sirenix.OdinInspector;
using TD.Towers;
using TD.UI;
using TD.Voxels;
using Unity.AI.Navigation;
using UnityEngine;

namespace TD.GameLoop
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
		[SerializeField] private bool logs = true;
		[SerializeField, Required, SceneObjectsOnly] private LevelGenerator LevelGenerator;
		[SerializeField, SceneObjectsOnly] private GameObject playerBase;

		private Transform[] spawnPoints;
		private GameObject basePrefab;
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

			FindAnyObjectByType<GameHUD>().Initialize();
			FindAnyObjectByType<GameManager>().Initialize();

			// Step 5: Finalize
			FinalizeBootstrap();

			if (logs) Debug.Log("[GameplayBootstrap] ✓ Bootstrap complete!");
		}

		private IEnumerator GenerateLevel()
		{
			if (logs) Debug.Log("[GameplayBootstrap] Step 1/5: Generating level...");

			LevelGenerator.GenerateLevel();

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
			if (!playerBase)
			{
				if (basePrefab == null)
				{
					Debug.LogWarning("[GameplayBootstrap] Base prefab not assigned, creating default cube");
					playerBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
					playerBase.transform.localScale = new Vector3(5, 3, 5);
					playerBase.AddComponent<PlayerBase>();
				}
				else
				{
					playerBase = Instantiate(basePrefab);
				}
			}

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
			if (LevelGenerator == null) return null;

			VoxelGenerator voxelGen = LevelGenerator.GetComponentInChildren<VoxelGenerator>();
			if (voxelGen != null)
			{
				return voxelGen.profile as LevelRoadGenerationProfile;
			}

			return null;
		}
	}
}