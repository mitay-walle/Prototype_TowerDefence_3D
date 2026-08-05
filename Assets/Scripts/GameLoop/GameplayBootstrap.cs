using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TD.Levels;
using TD.Towers;
using TD.UI;
using UnityEngine;

namespace TD.GameLoop
{
	public class GameplayBootstrap : MonoBehaviour
	{
		public readonly struct BootstrapResult
		{
			public bool Succeeded { get; }
			public string FailureReason { get; }

			private BootstrapResult(bool succeeded, string failureReason)
			{
				Succeeded = succeeded;
				FailureReason = failureReason;
			}

			public static BootstrapResult Success => new(true, string.Empty);

			public static BootstrapResult Failure(string reason) => new(false, reason);
		}

		[SerializeField] private LevelGenerator levelGenerator;
		[SerializeField] private GameManager gameManager;
		[SerializeField] private ResourceManager resourceManager;
		[SerializeField] private GameHUD gameHUD;
		[SerializeField] private WaveManager waveManager;
		[SerializeField] private PlayerBase playerBase;
		[SerializeField] private TilePlacementSystem tilePlacementSystem;
		[SerializeField] private NavMeshSurfaceWrapper navMeshSurfaceWrapper;
		[SerializeField] private bool Logs;

		private void Start()
		{
			_ = BootstrapSequenceAsync();
		}

		private async UniTask<BootstrapResult> BootstrapSequenceAsync()
		{
			if (Logs) Debug.Log("[GameplayBootstrap] === BOOTSTRAP STARTED ===");

			if (gameManager != null)
			{
				gameManager.BeginBoot();
			}

			var validation = ValidateRequiredReferences();
			if (!validation.Succeeded)
				return ReportFailure(validation);

			gameManager.BeginMapBuild();

			var levelResult = await GenerateLevelAsync();
			if (!levelResult.Succeeded)
				return ReportFailure(levelResult);

			var navMeshResult = await BakeNavMeshAsync();
			if (!navMeshResult.Succeeded)
				return ReportFailure(navMeshResult);

			var placementResult = await PlaceGameplayObjectsAsync();
			if (!placementResult.Succeeded)
				return ReportFailure(placementResult);

			var systemsResult = await InitializeSystemsAsync();
			if (!systemsResult.Succeeded)
				return ReportFailure(systemsResult);

			gameManager.CompleteMapBuild();

			if (Logs) Debug.Log("[GameplayBootstrap] === BOOTSTRAP COMPLETE ===");
			return BootstrapResult.Success;
		}

		private BootstrapResult ValidateRequiredReferences()
		{
			var missing = new List<string>();
			if (levelGenerator == null) missing.Add(nameof(levelGenerator));
			if (gameManager == null) missing.Add(nameof(gameManager));
			if (resourceManager == null) missing.Add(nameof(resourceManager));
			if (gameHUD == null) missing.Add(nameof(gameHUD));
			if (waveManager == null) missing.Add(nameof(waveManager));
			if (playerBase == null) missing.Add(nameof(playerBase));
			if (tilePlacementSystem == null) missing.Add(nameof(tilePlacementSystem));
			if (navMeshSurfaceWrapper == null) missing.Add(nameof(navMeshSurfaceWrapper));

			return missing.Count == 0
				? BootstrapResult.Success
				: BootstrapResult.Failure($"Missing required references: {string.Join(", ", missing)}");
		}

		private BootstrapResult ReportFailure(BootstrapResult result)
		{
			Debug.LogError($"[GameplayBootstrap] Bootstrap blocked: {result.FailureReason}");
			return result;
		}

		private UniTask<BootstrapResult> GenerateLevelAsync()
		{
			if (Logs) Debug.Log("[GameplayBootstrap] Generating level...");

			if (levelGenerator == null)
				return UniTask.FromResult(BootstrapResult.Failure("LevelGenerator reference is missing."));

			return UniTask.FromResult(levelGenerator.GenerateLevel()
				? BootstrapResult.Success
				: BootstrapResult.Failure("Level generation failed validation."));
		}

		private UniTask<BootstrapResult> BakeNavMeshAsync()
		{
			if (Logs) Debug.Log("[GameplayBootstrap] Baking NavMesh...");

			if (navMeshSurfaceWrapper == null)
				return UniTask.FromResult(BootstrapResult.Failure("NavMeshSurfaceWrapper reference is missing."));

			return UniTask.FromResult(navMeshSurfaceWrapper.BuildNavMesh()
				? BootstrapResult.Success
				: BootstrapResult.Failure("NavMesh rebuild did not produce NavMesh data."));
		}

		private UniTask<BootstrapResult> PlaceGameplayObjectsAsync()
		{
			if (Logs) Debug.Log("[GameplayBootstrap] Placing gameplay objects...");

			var tileMapManager = levelGenerator != null ? levelGenerator.GetTileMapManager() : null;
			if (tileMapManager == null)
				return UniTask.FromResult(BootstrapResult.Failure("TileMapManager reference is missing from LevelGenerator."));

			List<Vector3> spawnPositions = tileMapManager.SpawnPositions;
			if (spawnPositions == null || spawnPositions.Count == 0)
				return UniTask.FromResult(BootstrapResult.Failure("Validated map has no spawn positions."));

			foreach (var spawnPosition in spawnPositions)
			{
				if (spawnPosition == Vector3.zero)
					return UniTask.FromResult(BootstrapResult.Failure("Validated map contains an origin spawn position."));
			}

			if (Logs) Debug.Log($"[GameplayBootstrap] Base at {tileMapManager.BasePosition}, spawners: {spawnPositions.Count}");
			playerBase.transform.position = tileMapManager.BasePosition;

			var spawnTransforms = new Transform[spawnPositions.Count];
			for (int i = 0; i < spawnPositions.Count; i++)
			{
				var spawnGo = new GameObject($"Spawner_{i}");
				spawnGo.transform.position = spawnPositions[i];
				spawnTransforms[i] = spawnGo.transform;
			}

			waveManager.Initialize(null, spawnTransforms, playerBase, tileMapManager, tilePlacementSystem);
			if (Logs) Debug.Log("[GameplayBootstrap] WaveManager initialized with spawn points and inter-wave owners");
			return UniTask.FromResult(BootstrapResult.Success);
		}

		private UniTask<BootstrapResult> InitializeSystemsAsync()
		{
			if (Logs) Debug.Log("[GameplayBootstrap] Initializing systems...");

			if (gameHUD == null)
				return UniTask.FromResult(BootstrapResult.Failure("GameHUD reference is missing."));

			if (gameManager == null)
				return UniTask.FromResult(BootstrapResult.Failure("GameManager reference is missing."));

			gameHUD.Initialize();
			gameManager.Initialize();
			return UniTask.FromResult(BootstrapResult.Success);
		}
	}
}
