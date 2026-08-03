using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TD.Monsters;
using TD.Levels;
using TD.Towers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace TD.GameLoop
{
	public class WaveManager : MonoBehaviour
	{
		private const string TOOLTIP_LOOP_WAVES = "Restart from wave 1 after completing all waves with increased difficulty";
		private const string TOOLTIP_DIFFICULTY_SCALING = "Multiplier applied to enemy stats each loop (1.5 = 150% health/count per loop)";
		private const string TOOLTIP_RANDOMIZE_SPAWN = "Pick random spawn point for each enemy, otherwise use first spawn point";
		private const string TOOLTIP_AUTO_START = "Automatically start next wave after completion";
		private const string TOOLTIP_AUTO_DELAY = "Delay in seconds before auto-starting next wave";
		private const string TOOLTIP_DETAILED_LOGS = "Show detailed spawn logs for each enemy";

		public const int ResourceCacheAmount = 5;
		public const int BountyContractBonus = 15;
		public const float ReinforcedHordeEnemyCountFactor = 1.25f;
		public const float ReinforcedHordeEnemyHealthFactor = 1.25f;
		public const float ReinforcedHordeCompletionRewardFactor = 1.5f;

		[SerializeField] private bool Logs = true;
		[Tooltip(TOOLTIP_DETAILED_LOGS)]
		[SerializeField] private bool detailedLogs = false;
		public static WaveManager Instance { get; private set; }

		[SerializeField] private List<WaveConfig> waves = new List<WaveConfig>();
		[Tooltip(TOOLTIP_LOOP_WAVES)]
		[SerializeField] private bool loopWaves = true;
		[Tooltip(TOOLTIP_DIFFICULTY_SCALING)]
		[SerializeField] private float difficultyScalingPerLoop = 1.5f;

		[SerializeField] private Transform[] spawnPoints;
		[Tooltip(TOOLTIP_RANDOMIZE_SPAWN)]
		[SerializeField] private bool randomizeSpawnPoint = true;

		[Tooltip(TOOLTIP_AUTO_START)]
		[SerializeField] private bool autoStartNextWave = false;
		[Tooltip(TOOLTIP_AUTO_DELAY)]
		[SerializeField] private float autoStartDelay = 3f;
		[SerializeField] private InputActionAsset inputActions;

		[SerializeField] private int currentWaveIndex = -1;
		[SerializeField] private int currentLoopCount = 0;
		[SerializeField] private int enemiesAlive = 0;
		[SerializeField] private int enemiesSpawned = 0;
		[SerializeField] private int totalEnemiesInWave = 0;

		[ProgressBar(0, 1, ColorGetter = "GetSpawnProgressColor")]
		[ShowInInspector, ReadOnly]
		private float spawnProgress = 0f;

		[ShowInInspector, ReadOnly]
		private float timeUntilNextWave = 0f;

		[ShowInInspector, ReadOnly]
		private string currentWaveStatus = "Idle";

		public UnityEvent<int> onWaveStarted;
		public UnityEvent<int> onWaveCompleted;
		public UnityEvent<int> onEnemySpawned;
		public UnityEvent<int> onEnemyKilled;
		public UnityEvent onAllWavesCompleted;
		public UnityEvent onPreparationReady;

		private UniTask spawnTask;
		private bool isSpawning = false;
		private bool rewardOfferPending;
		private int nextCompletionRewardBonus;
		private ChallengeModifier activeChallengeModifier;
		private InputAction startWaveAction;

		public int CurrentWaveNumber => currentWaveIndex + 1;
		public int TotalWaves => waves.Count;
		public bool AutoStartNextWave => autoStartNextWave;
		public bool IsSpawning => isSpawning;
		public bool IsWaveActive => enemiesAlive > 0 || isSpawning;
		public bool IsRewardOfferPending => rewardOfferPending;
		public ChallengeModifier ActiveChallengeModifier => activeChallengeModifier;
		public bool CanSelectChallengeModifier => currentWaveIndex < 0 && activeChallengeModifier == ChallengeModifier.None;
		public float EnemyCountFactor => activeChallengeModifier == ChallengeModifier.ReinforcedHorde
			? ReinforcedHordeEnemyCountFactor
			: 1f;
		public float EnemyHealthFactor => activeChallengeModifier == ChallengeModifier.ReinforcedHorde
			? ReinforcedHordeEnemyHealthFactor
			: 1f;
		public float CompletionRewardFactor => activeChallengeModifier == ChallengeModifier.ReinforcedHorde
			? ReinforcedHordeCompletionRewardFactor
			: 1f;
		public int EnemiesAlive => enemiesAlive;

		public void SelectChallengeModifier(ChallengeModifier modifier)
		{
			if (!CanSelectChallengeModifier || modifier != ChallengeModifier.ReinforcedHorde)
				return;

			activeChallengeModifier = modifier;

			if (Logs) Debug.Log($"[WaveManager] Challenge modifier selected: {modifier}");
		}

		public int EnemiesSpawned => enemiesSpawned;
		public int TotalEnemiesInWave => totalEnemiesInWave;
		public float WaveProgress => totalEnemiesInWave > 0 ? (float)enemiesSpawned / totalEnemiesInWave : 0f;
		public WaveConfig UpcomingWave
		{
			get
			{
				if (waves == null || waves.Count == 0)
					return null;

				var nextWaveIndex = currentWaveIndex + 1;
				if (nextWaveIndex >= waves.Count)
				{
					if (!loopWaves)
						return null;

					nextWaveIndex = 0;
				}

				return waves[nextWaveIndex];
			}
		}

		private Color GetSpawnProgressColor()
		{
			if (spawnProgress < 0.33f) return Color.red;
			if (spawnProgress < 0.66f) return Color.yellow;

			return Color.green;
		}

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;

			if (inputActions != null)
			{
				startWaveAction = inputActions.FindAction("Player/Start Wave", true);
			}
		}

		private void OnEnable()
		{
			if (startWaveAction == null) return;

			startWaveAction.Enable();
			startWaveAction.performed += OnStartWaveInput;
		}

		private void OnDisable()
		{
			if (startWaveAction != null) startWaveAction.performed -= OnStartWaveInput;
		}

		private void OnStartWaveInput(InputAction.CallbackContext context)
		{
			if (context.performed)
			{
				GameManager.Instance?.StartNextWave();
			}
		}

		private void Start()
		{
			ValidateSpawnPoints();
		}

		private void ValidateSpawnPoints()
		{
			if (spawnPoints == null || spawnPoints.Length == 0)
			{
				Debug.LogWarning("WaveManager: No spawn points assigned! Searching for GameObject with tag 'SpawnPoint'");
				GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
				spawnPoints = new Transform[spawnPointObjects.Length];
				for (int i = 0; i < spawnPointObjects.Length; i++)
				{
					spawnPoints[i] = spawnPointObjects[i].transform;
				}
			}

			if (spawnPoints.Length == 0)
			{
				Debug.LogError("WaveManager: Still no spawn points found! Creating default at origin.");
				GameObject spawnPoint = new GameObject("DefaultSpawnPoint");
				spawnPoint.transform.position = Vector3.zero;
				spawnPoints = new Transform[] { spawnPoint.transform };
			}
		}

		public void Initialize(List<WaveConfig> waveConfigs, Transform[] spawners)
		{
			if (waveConfigs != null && waveConfigs.Count > 0)
			{
				waves = waveConfigs;
			}

			if (spawners != null && spawners.Length > 0)
			{
				spawnPoints = spawners;
			}

			if (Logs) Debug.Log($"[WaveManager] Initialized with {waves.Count} waves and {spawnPoints?.Length ?? 0} spawn points");
		}

		public void StartNextWave()
		{
			if (isSpawning)
			{
				Debug.LogWarning("WaveManager: Cannot start wave while already spawning!");
				return;
			}

			currentWaveIndex++;

			if (currentWaveIndex >= waves.Count)
			{
				if (loopWaves)
				{
					currentWaveIndex = 0;
					currentLoopCount++;
					if (Logs) Debug.Log($"[WaveManager] Starting loop {currentLoopCount + 1} with {difficultyScalingPerLoop}x difficulty");
				}
				else
				{
					return;
				}
			}

			spawnTask = SpawnWaveAsync(waves[currentWaveIndex]);
		}

		private async UniTask SpawnWaveAsync(WaveConfig waveConfig)
		{
			isSpawning = true;
			enemiesSpawned = 0;
			totalEnemiesInWave = GetTotalEnemyCount(waveConfig);
			spawnProgress = 0f;
			currentWaveStatus = $"Wave {currentWaveIndex + 1} - Waiting to start";

			if (Logs) Debug.Log($"[WaveManager] ==== Wave {currentWaveIndex + 1} Started ====");
			if (Logs)
				Debug.Log(
					$"[WaveManager] Total enemies: {totalEnemiesInWave}, Loop: {currentLoopCount + 1}, Difficulty: {Mathf.Pow(difficultyScalingPerLoop, currentLoopCount):F2}x");

			onWaveStarted?.Invoke(currentWaveIndex + 1);

			await UniTask.Delay((int)(waveConfig.DelayBeforeWave * 1000f), cancellationToken: this.GetCancellationTokenOnDestroy());

			timeUntilNextWave = 0f;
			currentWaveStatus = $"Wave {currentWaveIndex + 1} - Spawning";

			int spawnGroupIndex = 0;
			foreach (var enemySpawn in waveConfig.EnemySpawns)
			{
				int count = GetScaledEnemyCount(enemySpawn, waveConfig);
				spawnGroupIndex++;

				if (Logs)
					Debug.Log(
						$"[WaveManager] Spawning group {spawnGroupIndex}: {count}x {enemySpawn.enemyPrefab?.name ?? "NULL"} (delay: {enemySpawn.spawnDelay}s)");

				for (int i = 0; i < count; i++)
				{
					SpawnEnemy(enemySpawn, waveConfig, spawnGroupIndex, i + 1, count);
					enemiesSpawned++;
					spawnProgress = (float)enemiesSpawned / totalEnemiesInWave;
					onEnemySpawned?.Invoke(enemiesSpawned);

					if (detailedLogs) Debug.Log($"[WaveManager] Spawned enemy {enemiesSpawned}/{totalEnemiesInWave} ({spawnProgress * 100:F1}%)");

					await UniTask.Delay((int)(enemySpawn.spawnDelay * 1000f), cancellationToken: this.GetCancellationTokenOnDestroy());
				}
			}

			isSpawning = false;
			spawnProgress = 1f;
			currentWaveStatus = $"Wave {currentWaveIndex + 1} - Fighting ({enemiesAlive} enemies alive)";

			if (Logs) Debug.Log($"[WaveManager] All enemies spawned. Waiting for {enemiesAlive} enemies to be defeated...");

			while (enemiesAlive > 0)
			{
				currentWaveStatus = $"Wave {currentWaveIndex + 1} - Fighting ({enemiesAlive} enemies alive)";
				await UniTask.DelayFrame(1, cancellationToken: this.GetCancellationTokenOnDestroy());
			}

			currentWaveStatus = $"Wave {currentWaveIndex + 1} - Completed!";
			OnWaveCompleted(waveConfig);
		}

		private void SpawnEnemy(EnemySpawnData enemySpawn, WaveConfig waveConfig, int groupIndex, int enemyIndexInGroup, int groupTotal)
		{
			if (enemySpawn.enemyPrefab == null)
			{
				Debug.LogError("[WaveManager] Enemy prefab is null!");
				return;
			}

			Transform spawnPoint = GetSpawnPoint();
			GameObject enemyObject = Instantiate(enemySpawn.enemyPrefab, spawnPoint.position, spawnPoint.rotation);

			var enemyHealth = enemyObject.GetComponent<MonsterHealth>();
			if (enemyHealth != null)
			{
				float scaledHealth = enemyHealth.MaxHealth * enemySpawn.healthMultiplier * waveConfig.HealthScaling *
				                     Mathf.Pow(difficultyScalingPerLoop, currentLoopCount) * EnemyHealthFactor;

				enemyHealth.Initialize(scaledHealth);

				enemyHealth.onDeath.AddListener(() => OnEnemyKilled());
				enemyHealth.onRewardGiven.AddListener((reward) => OnEnemyRewardGiven(reward));

				if (detailedLogs)
				{
					Debug.Log(
						$"[WaveManager]   → Enemy [{groupIndex}.{enemyIndexInGroup}/{groupTotal}]: {enemyObject.name} | HP: {scaledHealth:F0} | Spawn: {spawnPoint.name}");
				}
			}

			var enemyMovement = enemyObject.GetComponent<MonsterMove>();
			if (enemyMovement != null)
			{
				float scaledSpeed = enemyMovement.Speed * enemySpawn.speedMultiplier;
				enemyMovement.Speed = scaledSpeed;

				if (detailedLogs)
				{
					Debug.Log($"[WaveManager]   → Speed: {scaledSpeed:F2} units/sec");
				}
			}

			enemiesAlive++;
		}

		private int GetTotalEnemyCount(WaveConfig waveConfig)
		{
			int total = 0;
			foreach (var enemySpawn in waveConfig.EnemySpawns)
			{
				total += GetScaledEnemyCount(enemySpawn, waveConfig);
			}

			return total;
		}

		private int GetScaledEnemyCount(EnemySpawnData enemySpawn, WaveConfig waveConfig)
		{
			return Mathf.RoundToInt(enemySpawn.count * waveConfig.CountScaling *
				Mathf.Pow(difficultyScalingPerLoop, currentLoopCount) * EnemyCountFactor);
		}

		private Transform GetSpawnPoint()
		{
			if (randomizeSpawnPoint)
			{
				return spawnPoints[Random.Range(0, spawnPoints.Length)];
			}
			else
			{
				return spawnPoints[0];
			}
		}

		private void OnEnemyKilled()
		{
			enemiesAlive--;
			onEnemyKilled?.Invoke(enemiesAlive);
		}

		private void OnEnemyRewardGiven(int reward)
		{
			ResourceManager.Instance?.AddCurrency(reward);
		}

		private void OnWaveCompleted(WaveConfig waveConfig)
		{
			int completionReward = Mathf.RoundToInt((waveConfig.CompletionReward + nextCompletionRewardBonus) * CompletionRewardFactor);
			nextCompletionRewardBonus = 0;

			if (Logs) Debug.Log($"[WaveManager] Wave {currentWaveIndex + 1} completed! Reward: {completionReward}");

			ResourceManager.Instance?.AddCurrency(completionReward);
			ResourceManager.Instance?.GivePassiveIncome();

			onWaveCompleted?.Invoke(currentWaveIndex + 1);

			if (currentWaveIndex + 1 >= waves.Count)
			{
				if (Logs) Debug.Log("[WaveManager] All waves completed!");
				onAllWavesCompleted?.Invoke();
				return;
			}

			_ = InterWavePhase();
		}

		public void SelectRewardOffer(int choiceIndex)
		{
			if (!rewardOfferPending || choiceIndex < (int)RewardOfferChoice.ResourceCache ||
				choiceIndex > (int)RewardOfferChoice.BountyContract)
				return;

			rewardOfferPending = false;
			var choice = (RewardOfferChoice)choiceIndex;
			switch (choice)
			{
				case RewardOfferChoice.ResourceCache:
					ResourceManager.Instance?.AddCurrency(ResourceCacheAmount);
					break;
				case RewardOfferChoice.EmergencyRepairs:
					FindFirstObjectByType<PlayerBase>()?.Repair(10);
					break;
				case RewardOfferChoice.BountyContract:
					nextCompletionRewardBonus = BountyContractBonus;
					break;
			}

			if (Logs) Debug.Log($"[WaveManager] Reward offer selected: {choice}");
		}

		private async UniTask InterWavePhase()
		{
			await RewardOfferPhase();
			await TilePlacementPhase();
		}

		private async UniTask RewardOfferPhase()
		{
			rewardOfferPending = true;
			await UniTask.WaitUntil(() => !rewardOfferPending,
				cancellationToken: this.GetCancellationTokenOnDestroy());
		}

		private async UniTask TilePlacementPhase()
		{
			var tileMapManager = FindFirstObjectByType<TileMapManager>();
			var tilePlacementSystem = FindFirstObjectByType<TilePlacementSystem>();
			if (tileMapManager == null || TileDatabase.Instance == null || tilePlacementSystem == null)
			{
				ContinueToNextWave();
				return;
			}

			if (Logs) Debug.Log("[WaveManager] Tile placement phase started");

			var tilePrefabs = TileDatabase.Instance.GetAllTilePrefabs();
			var placementChoices = tileMapManager.BuildPlacementChoices(tilePrefabs, 3);
			if (placementChoices.Count < 3)
			{
				if (Logs) Debug.LogWarning($"[WaveManager] Tile placement phase skipped: only {placementChoices.Count} valid choices");
				ContinueToNextWave();
				return;
			}

			await UniTask.Delay(500, cancellationToken: this.GetCancellationTokenOnDestroy());

			tilePlacementSystem.StartTilePlacementOptions(placementChoices);

			if (Logs)
			{
				for (var i = 0; i < placementChoices.Count; i++)
				{
					var choice = placementChoices[i];
					Debug.Log(
						$"[WaveManager] Tile option {i + 1}: {choice.TileName} {choice.Rotation * 90}° at {choice.GridPosition}, " +
						$"open ends {choice.OpenRoadEndCountBefore}->{choice.OpenRoadEndCountAfter}");
				}
			}

			await UniTask.WaitUntil(() => !tilePlacementSystem.IsPlacing,
				cancellationToken: this.GetCancellationTokenOnDestroy());

			RefreshSpawnPoints(tileMapManager);
			if (Logs) Debug.Log("[WaveManager] Tile placement phase completed");

			ContinueToNextWave();
		}

		private void RefreshSpawnPoints(TileMapManager tileMapManager)
		{
			var spawnPositions = tileMapManager.SpawnPositions;
			if (spawnPositions.Count == 0)
				return;

			var refreshedSpawnPoints = new Transform[spawnPositions.Count];
			for (var i = 0; i < spawnPositions.Count; i++)
			{
				if (spawnPoints != null && i < spawnPoints.Length && spawnPoints[i] != null)
				{
					refreshedSpawnPoints[i] = spawnPoints[i];
				}
				else
				{
					var spawnPoint = new GameObject($"Spawner_{i}");
					refreshedSpawnPoints[i] = spawnPoint.transform;
				}

				refreshedSpawnPoints[i].position = spawnPositions[i];
			}

			spawnPoints = refreshedSpawnPoints;
		}

		private void ContinueToNextWave()
		{
			if (currentWaveIndex + 1 >= waves.Count)
			{
				if (Logs) Debug.Log("[WaveManager] All waves completed!");
				onAllWavesCompleted?.Invoke();
			}
			else
			{
				onPreparationReady?.Invoke();

				if (autoStartNextWave)
				{
					Invoke(nameof(RequestNextWave), autoStartDelay);
				}
			}
		}

		private void RequestNextWave()
		{
			GameManager.Instance?.StartNextWave();
		}

		public void ForceStopWave()
		{
			isSpawning = false;

			var enemies = FindObjectsByType<MonsterHealth>(FindObjectsSortMode.None);
			foreach (var enemy in enemies)
			{
				Destroy(enemy.gameObject);
			}

			enemiesAlive = 0;
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}

			onWaveStarted?.RemoveAllListeners();
			onWaveCompleted?.RemoveAllListeners();
			onEnemySpawned?.RemoveAllListeners();
			onEnemyKilled?.RemoveAllListeners();
			onAllWavesCompleted?.RemoveAllListeners();
			onPreparationReady?.RemoveAllListeners();
		}
	}
}
