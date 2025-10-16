using UnityEditor;
using UnityEngine;
using System.Linq;

namespace TD.Editor
{
    public class BalanceTowerStats
    {
        [MenuItem("Automation/Balance Tower Stats")]
        public static void Balance()
        {
            Debug.Log("[BalanceTowerStats] Starting tower balancing...");

            string[] guids = AssetDatabase.FindAssets("t:TowerStats", new[] { "Assets/Resources/TowerStats" });

            if (guids.Length == 0)
            {
                Debug.LogWarning("[BalanceTowerStats] No TowerStats found!");
                return;
            }

            var towerStats = guids
                .Select(guid => AssetDatabase.LoadAssetAtPath<TowerStats>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(stats => stats != null)
                .OrderBy(stats => stats.Cost)
                .ToList();

            Debug.Log($"[BalanceTowerStats] Found {towerStats.Count} tower configs");

            int index = 0;
            foreach (var stats in towerStats)
            {
                BalanceTower(stats, index, towerStats.Count);
                index++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[BalanceTowerStats] ✓ Balancing complete!");
        }

        private static void BalanceTower(TowerStats stats, int index, int totalCount)
        {
            var so = new SerializedObject(stats);

            float tier = (float)index / Mathf.Max(1, totalCount - 1);

            float baseDamage = Mathf.Lerp(15f, 60f, tier);
            float baseFireRate = Mathf.Lerp(1.5f, 0.8f, tier);
            float baseRange = Mathf.Lerp(8f, 15f, tier);
            float baseProjSpeed = Mathf.Lerp(15f, 30f, tier);

            int baseCost = Mathf.RoundToInt(Mathf.Lerp(100f, 500f, tier));
            int baseSellValue = Mathf.RoundToInt(baseCost * 0.7f);
            int upgradeC = Mathf.RoundToInt(baseCost * 1.5f);

            so.FindProperty("damage").floatValue = baseDamage;
            so.FindProperty("fireRate").floatValue = baseFireRate;
            so.FindProperty("range").floatValue = baseRange;
            so.FindProperty("projectileSpeed").floatValue = baseProjSpeed;

            so.FindProperty("cost").intValue = baseCost;
            so.FindProperty("sellValue").intValue = baseSellValue;
            so.FindProperty("upgradeCost").intValue = upgradeC;

            var priorityProp = so.FindProperty("targetPriority");
            switch (index % 4)
            {
                case 0: priorityProp.enumValueIndex = (int)TargetPriority.Nearest; break;
                case 1: priorityProp.enumValueIndex = (int)TargetPriority.Farthest; break;
                case 2: priorityProp.enumValueIndex = (int)TargetPriority.Strongest; break;
                case 3: priorityProp.enumValueIndex = (int)TargetPriority.Weakest; break;
            }

            so.FindProperty("predictiveAiming").boolValue = tier > 0.5f;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(stats);

            float dps = baseDamage * baseFireRate;
            float timeToKillBasicEnemy = 100f / dps;

            Debug.Log($"[BalanceTowerStats] {stats.name}:");
            Debug.Log($"  Damage: {baseDamage:F1} | Fire Rate: {baseFireRate:F2}/s | DPS: {dps:F1}");
            Debug.Log($"  Range: {baseRange:F1} | Projectile Speed: {baseProjSpeed:F1}");
            Debug.Log($"  Cost: ${baseCost} | Priority: {(TargetPriority)priorityProp.enumValueIndex}");
            Debug.Log($"  Time to kill 100HP enemy: {timeToKillBasicEnemy:F2}s");
        }
    }
}
