using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TD.GameLoop
{
    [System.Serializable]
    public class EnemySpawnData
    {
        public GameObject enemyPrefab;
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

        public string WaveName => waveName;
        public int WaveNumber => waveNumber;
        public List<EnemySpawnData> EnemySpawns => enemySpawns;
        public float DelayBeforeWave => delayBeforeWave;
        public int CompletionReward => completionReward;
        public float HealthScaling => healthScaling;
        public float CountScaling => countScaling;

        public int GetTotalEnemyCount()
        {
            int total = 0;
            foreach (var spawn in enemySpawns)
            {
                total += spawn.count;
            }
            return total;
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
