using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TD.GameLoop
{
    [System.Serializable]
    public class EnemySpawnData
    {
        [AssetList(Path = "Prefabs/Enemies"),PreviewField]public GameObject enemyPrefab;
        [Min(1)] public int count = 5;
        [Min(0)] public float spawnDelay = 0.5f;
        [Min(0)] public float healthMultiplier = 1f;
        [Min(0)] public float speedMultiplier = 1f;
    }

    [CreateAssetMenu(fileName = "WaveConfig", menuName = "TD/Wave Config", order = 1)]
    public class WaveConfig : ScriptableObject
    {
        [SerializeField] private string waveName = "Wave 1";

        [SerializeField, Min(1)] private int waveNumber = 1;

        [SerializeField] private List<EnemySpawnData> enemySpawns = new List<EnemySpawnData>();

        [SerializeField, Min(0)] private float delayBeforeWave = 5f;

        [SerializeField, Min(0)] private int completionReward = 100;

        [Tooltip("Multiplier for enemy health based on difficulty")]
        [SerializeField] private float healthScaling = 1f;

        [Tooltip("Multiplier for enemy count based on difficulty")]
        [SerializeField] private float countScaling = 1f;

        [SerializeField] private bool generatedByMl;
        [SerializeField] private int generationSeed;
        [SerializeField] private float predictedBaseDamage;
        [SerializeField] private float predictedCombatSeconds;
        [SerializeField] private float appliedAdaptiveEnemyHealthFactor = 1f;
        [SerializeField] private float appliedAdaptiveEnemyCountFactor = 1f;
        [SerializeField] private float appliedAdaptiveEnemySpeedFactor = 1f;
        [SerializeField] private float appliedAdaptiveRewardFactor = 1f;
        [SerializeField] private float safetyMargin;
        [SerializeField] private float tensionScore;
        [SerializeField] private float lastEvaluationScore;
        [SerializeField] private float lastObservedBaseHealthFraction;
        [SerializeField] private int evaluationCount;
        [SerializeField] private bool lastEvaluationVictory;
        [SerializeField] private bool lastEvaluationDefeat;

        public string WaveName => waveName;
        public int WaveNumber => waveNumber;
        public List<EnemySpawnData> EnemySpawns => enemySpawns;
        public float DelayBeforeWave => delayBeforeWave;
        public int CompletionReward => completionReward;
        public float HealthScaling => healthScaling;
        public float CountScaling => countScaling;
        public bool GeneratedByMl => generatedByMl;
        public int GenerationSeed => generationSeed;
        public float PredictedBaseDamage => predictedBaseDamage;
        public float PredictedCombatSeconds => predictedCombatSeconds;
        public float AppliedAdaptiveEnemyHealthFactor => appliedAdaptiveEnemyHealthFactor;
        public float AppliedAdaptiveEnemyCountFactor => appliedAdaptiveEnemyCountFactor;
        public float AppliedAdaptiveEnemySpeedFactor => appliedAdaptiveEnemySpeedFactor;
        public float AppliedAdaptiveRewardFactor => appliedAdaptiveRewardFactor;
        public float SafetyMargin => safetyMargin;
        public float TensionScore => tensionScore;
        public float LastEvaluationScore => lastEvaluationScore;
        public float LastObservedBaseHealthFraction => lastObservedBaseHealthFraction;
        public int EvaluationCount => evaluationCount;
        public bool LastEvaluationVictory => lastEvaluationVictory;
        public bool LastEvaluationDefeat => lastEvaluationDefeat;

        public int GetTotalEnemyCount()
        {
            int total = 0;
            foreach (var spawn in enemySpawns)
            {
                total += spawn.count;
            }
            return total;
        }

        public static WaveConfig CreateGenerated(
            string generatedWaveName,
            int generatedWaveNumber,
            List<EnemySpawnData> generatedSpawns,
            float generatedDelayBeforeWave,
            int generatedCompletionReward,
            float generatedHealthScaling,
            float generatedCountScaling,
            int seed,
            float predictedDamage,
            float generatedSafetyMargin,
            float generatedTensionScore,
            float generatedPredictedCombatSeconds = 0f,
            float generatedAdaptiveEnemyHealthFactor = 1f,
            float generatedAdaptiveEnemyCountFactor = 1f,
            float generatedAdaptiveEnemySpeedFactor = 1f,
            float generatedAdaptiveRewardFactor = 1f)
        {
            if (generatedSpawns == null || generatedSpawns.Count == 0)
                return null;

            var generatedWave = CreateInstance<WaveConfig>();
            generatedWave.name = generatedWaveName;
            generatedWave.waveName = generatedWaveName;
            generatedWave.waveNumber = Mathf.Max(1, generatedWaveNumber);
            generatedWave.delayBeforeWave = Mathf.Max(0f, generatedDelayBeforeWave);
            generatedWave.completionReward = Mathf.Max(0, generatedCompletionReward);
            generatedWave.healthScaling = Mathf.Max(0.1f, generatedHealthScaling);
            generatedWave.countScaling = Mathf.Max(0.1f, generatedCountScaling);
            generatedWave.generatedByMl = true;
            generatedWave.generationSeed = seed;
            generatedWave.predictedBaseDamage = Mathf.Max(0f, predictedDamage);
            generatedWave.predictedCombatSeconds = Mathf.Max(0f, generatedPredictedCombatSeconds);
            generatedWave.appliedAdaptiveEnemyHealthFactor = Mathf.Max(0f, generatedAdaptiveEnemyHealthFactor);
            generatedWave.appliedAdaptiveEnemyCountFactor = Mathf.Max(0f, generatedAdaptiveEnemyCountFactor);
            generatedWave.appliedAdaptiveEnemySpeedFactor = Mathf.Max(0f, generatedAdaptiveEnemySpeedFactor);
            generatedWave.appliedAdaptiveRewardFactor = Mathf.Max(0f, generatedAdaptiveRewardFactor);
            generatedWave.safetyMargin = generatedSafetyMargin;
            generatedWave.tensionScore = Mathf.Clamp01(generatedTensionScore);

            foreach (var spawn in generatedSpawns)
            {
                if (spawn == null || spawn.enemyPrefab == null)
                    continue;

                generatedWave.enemySpawns.Add(new EnemySpawnData
                {
                    enemyPrefab = spawn.enemyPrefab,
                    count = Mathf.Max(1, spawn.count),
                    spawnDelay = Mathf.Max(0f, spawn.spawnDelay),
                    healthMultiplier = Mathf.Max(0f, spawn.healthMultiplier),
                    speedMultiplier = Mathf.Max(0f, spawn.speedMultiplier)
                });
            }

            return generatedWave.enemySpawns.Count > 0 ? generatedWave : null;
        }

        public void RecordGenerationEvaluation(float score, float observedBaseHealthFraction, bool victory, bool defeat)
        {
            lastEvaluationScore = score;
            lastObservedBaseHealthFraction = observedBaseHealthFraction;
            lastEvaluationVictory = victory;
            lastEvaluationDefeat = defeat;
            evaluationCount++;

#if UNITY_EDITOR
            if (UnityEditor.AssetDatabase.Contains(this))
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
            }
#endif
        }

        [Button("Create Next Wave")]
        private void CreateNextWave()
        {
            #if UNITY_EDITOR
            var nextWave = CreateInstance<WaveConfig>();
            nextWave.waveName = $"Wave {waveNumber + 1}";
            nextWave.waveNumber = waveNumber + 1;
            nextWave.delayBeforeWave = delayBeforeWave;
            nextWave.completionReward = Mathf.RoundToInt(completionReward * 1.2f);
            nextWave.healthScaling = healthScaling * 1.15f;
            nextWave.countScaling = countScaling * 1.1f;

            // Copy enemy spawns with increased difficulty
            foreach (var spawn in enemySpawns)
            {
                var newSpawn = new EnemySpawnData
                {
                    enemyPrefab = spawn.enemyPrefab,
                    count = Mathf.RoundToInt(spawn.count * 1.2f),
                    spawnDelay = spawn.spawnDelay * 0.95f,
                    healthMultiplier = spawn.healthMultiplier * 1.1f,
                    speedMultiplier = spawn.speedMultiplier * 1.05f
                };
                nextWave.enemySpawns.Add(newSpawn);
            }

            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            string directory = System.IO.Path.GetDirectoryName(path);
            string fileName = $"Wave_{waveNumber + 1:00}";
            string newPath = System.IO.Path.Combine(directory, fileName + ".asset");

            UnityEditor.AssetDatabase.CreateAsset(nextWave, newPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.Selection.activeObject = nextWave;
            #endif
        }
    }
}
