using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using TD.GameLoop;
using TD.Monsters;
using TD.Stats;
using TD.Towers;

namespace TD.Editor
{
    public class EconomyBalancer : OdinEditorWindow
    {
        [MenuItem("TD/Automation/Economy Balancer")]
        private static void OpenWindow()
        {
            GetWindow<EconomyBalancer>().Show();
        }

        [Title("Starting Resources")]
        [SerializeField] private int startingCurrency = 500;

        [Title("Enemy Rewards")]
        [SerializeField] private int baseEnemyReward = 15;
        [SerializeField, Range(1f, 3f)] private float earlyKillBonusMultiplier = 1.5f;
        [SerializeField, Range(0.3f, 0.8f)] private float earlyKillThreshold = 0.5f;

        [Title("Wave Rewards")]
        [SerializeField] private int wave1Reward = 50;
        [SerializeField] private int wave2Reward = 75;
        [SerializeField] private int wave3Reward = 100;
        [SerializeField] private int wave4Reward = 150;
        [SerializeField] private int wave5Reward = 200;
        [SerializeField] private int passiveIncomePerWave = 100;

        [Title("Tower Costs")]
        [SerializeField] private int basicTowerCost = 100;
        [SerializeField] private int sniperTowerCost = 150;
        [SerializeField] private int rapidTowerCost = 120;
        [SerializeField] private int heavyTowerCost = 200;

        [Button("Apply Economy Balance", ButtonSizes.Large)]
        private void ApplyBalance()
        {
            UpdateResourceManager();
            UpdateEnemyPrefabs();
            UpdateWaveConfigs();
            UpdateTowerStats();

            AssetDatabase.SaveAssets();
            Debug.Log("[EconomyBalancer] Balance applied successfully!");
        }

        private void UpdateResourceManager()
        {
            var resourceManager = FindObjectOfType<ResourceManager>();
            if (resourceManager != null)
            {
                var so = new SerializedObject(resourceManager);
                so.FindProperty("startingCurrency").intValue = startingCurrency;
                so.FindProperty("passiveIncomePerWave").intValue = passiveIncomePerWave;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(resourceManager);
            }
        }

        private void UpdateEnemyPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameObject Monster", new[] { "Assets/Prefabs" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                var health = prefab.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    var so = new SerializedObject(health);
                    so.FindProperty("rewardAmount").intValue = baseEnemyReward;
                    so.FindProperty("earlyKillBonusMultiplier").floatValue = earlyKillBonusMultiplier;
                    so.FindProperty("earlyKillThreshold").floatValue = earlyKillThreshold;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(prefab);
                }
            }
        }

        private void UpdateWaveConfigs()
        {
            int[] rewards = { wave1Reward, wave2Reward, wave3Reward, wave4Reward, wave5Reward };

            for (int i = 1; i <= 5; i++)
            {
                var waveConfig = AssetDatabase.LoadAssetAtPath<WaveConfig>($"Assets/Resources/WaveConfigs/Wave_0{i}.asset");
                if (waveConfig != null)
                {
                    var so = new SerializedObject(waveConfig);
                    so.FindProperty("completionReward").intValue = rewards[i - 1];
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(waveConfig);
                }
            }
        }

        private void UpdateTowerStats()
        {
            var basicStats = AssetDatabase.LoadAssetAtPath<StatsTower>("Assets/Resources/TowerStats/BasicTower.asset");
            var sniperStats = AssetDatabase.LoadAssetAtPath<StatsTower>("Assets/Resources/TowerStats/SniperTower.asset");
            var rapidStats = AssetDatabase.LoadAssetAtPath<StatsTower>("Assets/Resources/TowerStats/RapidTower.asset");
            var heavyStats = AssetDatabase.LoadAssetAtPath<StatsTower>("Assets/Resources/TowerStats/HeavyTower.asset");

            if (basicStats != null)
            {
                var so = new SerializedObject(basicStats);
                so.FindProperty("cost").intValue = basicTowerCost;
                so.FindProperty("sellValue").intValue = Mathf.RoundToInt(basicTowerCost * 0.7f);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(basicStats);
            }

            if (sniperStats != null)
            {
                var so = new SerializedObject(sniperStats);
                so.FindProperty("cost").intValue = sniperTowerCost;
                so.FindProperty("sellValue").intValue = Mathf.RoundToInt(sniperTowerCost * 0.7f);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(sniperStats);
            }

            if (rapidStats != null)
            {
                var so = new SerializedObject(rapidStats);
                so.FindProperty("cost").intValue = rapidTowerCost;
                so.FindProperty("sellValue").intValue = Mathf.RoundToInt(rapidTowerCost * 0.7f);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(rapidStats);
            }

            if (heavyStats != null)
            {
                var so = new SerializedObject(heavyStats);
                so.FindProperty("cost").intValue = heavyTowerCost;
                so.FindProperty("sellValue").intValue = Mathf.RoundToInt(heavyTowerCost * 0.7f);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(heavyStats);
            }
        }
    }
}
