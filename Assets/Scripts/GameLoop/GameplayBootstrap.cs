using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TD.Levels;
using TD.UI;

namespace TD.GameLoop
{
	public class GameplayBootstrap : MonoBehaviour
	{
		[SerializeField] private LevelGenerator levelGenerator;
		[SerializeField] private GameManager gameManager;
		[SerializeField] private GameHUD gameHUD;
		[SerializeField] private WaveManager waveManager;
		[SerializeField] private TilePlacementSystem tilePlacementSystem;
		[SerializeField] private NavMeshSurfaceWrapper navMeshSurfaceWrapper;
		[SerializeField] private bool Logs;

		private void Start()
		{
			_ = BootstrapSequenceAsync();
		}

		private async UniTask BootstrapSequenceAsync()
		{
			if (Logs) Debug.Log("[GameplayBootstrap] === BOOTSTRAP STARTED ===");

			await GenerateLevelAsync();
			await BakeNavMeshAsync();
			await PlaceGameplayObjectsAsync();
			await InitializeSystemsAsync();

			if (Logs) Debug.Log("[GameplayBootstrap] === BOOTSTRAP COMPLETE ===");
		}

		private async UniTask GenerateLevelAsync()
		{
			if (Logs) Debug.Log("[GameplayBootstrap] Generating level...");

			if (levelGenerator != null)
				levelGenerator.GenerateLevel();

			await UniTask.Delay(500, cancellationToken: this.GetCancellationTokenOnDestroy());
		}

		private async UniTask BakeNavMeshAsync()
		{
			if (Logs) Debug.Log("[GameplayBootstrap] Baking NavMesh...");

			if (navMeshSurfaceWrapper != null)
			{
				navMeshSurfaceWrapper.BuildNavMesh();
				await UniTask.Delay(500, cancellationToken: this.GetCancellationTokenOnDestroy());
			}
			else
			{
				if (Logs) Debug.LogWarning("[GameplayBootstrap] NavMeshSurface wrapper not found!");
			}
		}

		private async UniTask PlaceGameplayObjectsAsync()
		{
			if (Logs) Debug.Log("[GameplayBootstrap] Placing gameplay objects...");

			if (levelGenerator == null)
			{
				if (Logs) Debug.LogError("[GameplayBootstrap] LevelGenerator not found!");
				return;
			}

			var tileMapManager = levelGenerator.GetTileMapManager();
			if (tileMapManager == null)
			{
				if (Logs) Debug.LogError("[GameplayBootstrap] TileMapManager not found!");
				return;
			}

			Vector3 basePosition = tileMapManager.BasePosition;
			List<Vector3> spawnPositions = tileMapManager.SpawnPositions;

			if (Logs) Debug.Log($"[GameplayBootstrap] Base at {basePosition}, spawners: {spawnPositions.Count}");

			var playerBaseGo = GameObject.FindObjectOfType<GameManager>()?.gameObject;
			if (playerBaseGo != null)
			{
				var base3dGo = playerBaseGo.transform.Find("Base3D");
				if (base3dGo != null)
					base3dGo.position = basePosition;
			}

			if (waveManager != null)
			{
				var spawnTransforms = new Transform[spawnPositions.Count];
				for (int i = 0; i < spawnPositions.Count; i++)
				{
					var spawnGo = new GameObject($"Spawner_{i}");
					spawnGo.transform.position = spawnPositions[i];
					spawnTransforms[i] = spawnGo.transform;
				}

				waveManager.Initialize(null, spawnTransforms);
				if (Logs) Debug.Log("[GameplayBootstrap] WaveManager initialized with spawn points");
			}
			else
			{
				if (Logs) Debug.LogWarning("[GameplayBootstrap] WaveManager not found!");
			}

			await UniTask.DelayFrame(1, cancellationToken: this.GetCancellationTokenOnDestroy());
		}

		private async UniTask InitializeSystemsAsync()
		{
			if (Logs) Debug.Log("[GameplayBootstrap] Initializing systems...");

			if (gameHUD != null)
			{
				gameHUD.Initialize();
			}
			else
			{
				if (Logs) Debug.LogWarning("[GameplayBootstrap] GameHUD not found!");
			}

			if (gameManager != null)
			{
				gameManager.Initialize();
			}
			else
			{
				if (Logs) Debug.LogError("[GameplayBootstrap] GameManager not found!");
			}

			await UniTask.DelayFrame(1, cancellationToken: this.GetCancellationTokenOnDestroy());
		}
	}
}