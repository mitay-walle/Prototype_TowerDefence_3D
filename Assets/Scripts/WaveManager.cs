using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TD
{
    public class WaveManager : MonoBehaviour
    {
        private const string TOOLTIP_LOOP_WAVES = "Restart from wave 1 after completing all waves with increased difficulty";
        private const string TOOLTIP_DIFFICULTY_SCALING = "Multiplier applied to enemy stats each loop (1.5 = 150% health/count per loop)";
        private const string TOOLTIP_RANDOMIZE_SPAWN = "Pick random spawn point for each enemy, otherwise use first spawn point";
        private const string TOOLTIP_AUTO_START = "Automatically start next wave after completion";
        private const string TOOLTIP_AUTO_DELAY = "Delay in seconds before auto-starting next wave";

        [SerializeField] private bool Logs = true;
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
                    if (Logs) Debug.Log("[WaveManager] All waves completed!");
                    onAllWavesCompleted?.Invoke();
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

            if (Logs) Debug.Log($"[WaveManager] Starting wave {currentWaveIndex + 1}, enemies: {totalEnemiesInWave}");

            onWaveStarted?.Invoke(currentWaveIndex + 1);

            // Wait before wave starts
            yield return new WaitForSeconds(waveConfig.DelayBeforeWave);

            // Spawn all enemy types
            foreach (var enemySpawn in waveConfig.EnemySpawns)
            {
                int count = Mathf.RoundToInt(enemySpawn.count * waveConfig.CountScaling * Mathf.Pow(difficultyScalingPerLoop, currentLoopCount));

                for (int i = 0; i < count; i++)
                {
                    SpawnEnemy(enemySpawn, waveConfig);
                    enemiesSpawned++;
                    onEnemySpawned?.Invoke(enemiesSpawned);

                    yield return new WaitForSeconds(enemySpawn.spawnDelay);
                }
            }

            isSpawning = false;

            // Wait for all enemies to be killed
            while (enemiesAlive > 0)
            {
                yield return null;
            }

            OnWaveCompleted(waveConfig);
        }

        private void SpawnEnemy(EnemySpawnData enemySpawn, WaveConfig waveConfig)
        {
            if (enemySpawn.enemyPrefab == null)
            {
                Debug.LogError("WaveManager: Enemy prefab is null!");
                return;
            }

            Transform spawnPoint = GetSpawnPoint();
            GameObject enemyObject = Instantiate(enemySpawn.enemyPrefab, spawnPoint.position, spawnPoint.rotation);

            // Apply difficulty scaling
            var enemyHealth = enemyObject.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                float scaledHealth = enemyHealth.MaxHealth * enemySpawn.healthMultiplier * waveConfig.HealthScaling *
                                    Mathf.Pow(difficultyScalingPerLoop, currentLoopCount);
                enemyHealth.Initialize(scaledHealth);

                // Subscribe to death event
                enemyHealth.onDeath.AddListener(() => OnEnemyKilled());
                enemyHealth.onRewardGiven.AddListener((reward) => OnEnemyRewardGiven(reward));
            }

            var moveToBase = enemyObject.GetComponent<MoveToBase>();
            if (moveToBase != null)
            {
                float scaledSpeed = moveToBase.Speed * enemySpawn.speedMultiplier;
                moveToBase.SetSpeed(scaledSpeed);
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

            if (autoStartNextWave)
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
            var enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
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
