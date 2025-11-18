using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TD.Monsters;
using TD.Levels;
using UnityEngine;
using UnityEngine.Events;

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

		private Coroutine spawnCoroutine;
		private bool isSpawning = false;

		public int CurrentWaveNumber => currentWaveIndex + 1;
		public int TotalWaves => waves.Count;
		public bool IsSpawning => isSpawning;
		public bool IsWaveActive => enemiesAlive > 0 || isSpawning;
		public int EnemiesAlive => enemiesAlive;
		public int EnemiesSpawned => enemiesSpawned;
		public int TotalEnemiesInWave => totalEnemiesInWave;
		public float WaveProgress => totalEnemiesInWave > 0 ? (float)enemiesSpawned / totalEnemiesInWave : 0f;

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
		}

		private void Start()
		{
			ValidateSpawnPoints();

			if (waves.Count > 0 && autoStartNextWave)
			{
				StartNextWave();
			}
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

			if (spawnCoroutine != null)
			{
				StopCoroutine(spawnCoroutine);
			}

			spawnCoroutine = StartCoroutine(SpawnWaveCoroutine(waves[currentWaveIndex]));
		}

		private IEnumerator SpawnWaveCoroutine(WaveConfig waveConfig)
		{
			isSpawning = true;
			enemiesSpawned = 0;
			totalEnemiesInWave = waveConfig.GetTotalEnemyCount();
			spawnProgress = 0f;
			currentWaveStatus = $"Wave {currentWaveIndex + 1} - Waiting to start";

			if (Logs) Debug.Log($"[WaveManager] ==== Wave {currentWaveIndex + 1} Started ====");
			if (Logs)
				Debug.Log(
					$"[WaveManager] Total enemies: {totalEnemiesInWave}, Loop: {currentLoopCount + 1}, Difficulty: {Mathf.Pow(difficultyScalingPerLoop, currentLoopCount):F2}x");

			onWaveStarted?.Invoke(currentWaveIndex + 1);

			float delayTimer = waveConfig.DelayBeforeWave;
			while (delayTimer > 0)
			{
				timeUntilNextWave = delayTimer;
				currentWaveStatus = $"Wave {currentWaveIndex + 1} - Starting in {delayTimer:F1}s";
				delayTimer -= Time.deltaTime;
				yield return null;
			}

			timeUntilNextWave = 0f;
			currentWaveStatus = $"Wave {currentWaveIndex + 1} - Spawning";

			int spawnGroupIndex = 0;
			foreach (var enemySpawn in waveConfig.EnemySpawns)
			{
				int count = Mathf.RoundToInt(enemySpawn.count * waveConfig.CountScaling * Mathf.Pow(difficultyScalingPerLoop, currentLoopCount));
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

					yield return new WaitForSeconds(enemySpawn.spawnDelay);
				}
			}

			isSpawning = false;
			spawnProgress = 1f;
			currentWaveStatus = $"Wave {currentWaveIndex + 1} - Fighting ({enemiesAlive} enemies alive)";

			if (Logs) Debug.Log($"[WaveManager] All enemies spawned. Waiting for {enemiesAlive} enemies to be defeated...");

			while (enemiesAlive > 0)
			{
				currentWaveStatus = $"Wave {currentWaveIndex + 1} - Fighting ({enemiesAlive} enemies alive)";
				yield return null;
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
				                     Mathf.Pow(difficultyScalingPerLoop, currentLoopCount);

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
			if (Logs) Debug.Log($"[WaveManager] Wave {currentWaveIndex + 1} completed! Reward: {waveConfig.CompletionReward}");

			ResourceManager.Instance?.AddCurrency(waveConfig.CompletionReward);
			ResourceManager.Instance?.GivePassiveIncome();

			onWaveCompleted?.Invoke(currentWaveIndex + 1);

			StartCoroutine(TilePlacementPhase());
		}

		private IEnumerator TilePlacementPhase()
		{
			var tileMapManager = FindFirstObjectByType<TileMapManager>();
			if (tileMapManager == null || TileDatabase.Instance == null)
			{
				ContinueToNextWave();
				yield break;
			}

			if (Logs) Debug.Log("[WaveManager] Tile placement phase started");

			var tilePrefab = TileDatabase.Instance.GetRandomTilePrefab();
			if (tilePrefab == null)
			{
				ContinueToNextWave();
				yield break;
			}

			yield return new WaitForSeconds(0.5f);

			if (Logs) Debug.Log("[WaveManager] Player can now place a tile");

			yield return new WaitUntil(() => Input.GetMouseButtonDown(0));

			if (Logs) Debug.Log("[WaveManager] Tile placement phase completed");

			ContinueToNextWave();
		}

		private void ContinueToNextWave()
		{
			if (currentWaveIndex + 1 >= waves.Count)
			{
				if (Logs) Debug.Log("[WaveManager] All waves completed!");
				onAllWavesCompleted?.Invoke();
			}
			else if (autoStartNextWave)
			{
				Invoke(nameof(StartNextWave), autoStartDelay);
			}
		}

		public void ForceStopWave()
		{
			if (spawnCoroutine != null)
			{
				StopCoroutine(spawnCoroutine);
				spawnCoroutine = null;
			}

			isSpawning = false;

			// Destroy all enemies
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
		}
	}
}