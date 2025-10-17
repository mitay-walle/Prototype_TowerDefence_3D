using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace TD.Editor
{
    public static class CreateTowerVariants
    {
        [MenuItem("TD/Automation/Create Tower Variants")]
        public static void CreateTowers()
        {
            const bool logs = true;
            string basePath = "Assets/Resources/TowerStats/";

            var towers = new List<(string name, string desc, float dmg, float firerate, float range, float projSpeed, int priority, bool predictive, int cost, int sell, int upgradeCost)>
            {
                ("FastGun", "High fire rate, low damage - good for swarms", 8f, 3f, 6f, 12f, 0, false, 90, 63, 120),
                ("PlasmaBlast", "Medium damage, medium fire rate, AoE effect", 25f, 1.2f, 9f, 18f, 1, false, 140, 98, 180),
                ("LaserTower", "Precision targeting with instant damage", 35f, 0.9f, 11f, 99f, 2, true, 180, 126, 250),
                ("CryoTower", "Slows enemies with icy projectiles", 20f, 1.4f, 10f, 16f, 1, false, 160, 112, 220),
                ("Cannon", "Explosive shells with heavy splash damage", 50f, 0.7f, 13f, 22f, 3, true, 220, 154, 320),
                ("Tesla", "Chain lightning between nearby enemies", 30f, 1.1f, 8f, 20f, 2, false, 170, 119, 260),
            };

            int createdCount = 0;
            foreach (var tower in towers)
            {
                string path = basePath + tower.name + ".asset";
                if (!AssetDatabase.LoadAssetAtPath<TowerStats>(path))
                {
                    var stats = ScriptableObject.CreateInstance<TowerStats>();
                    SetTowerStats(stats, tower);

                    AssetDatabase.CreateAsset(stats, path);
                    createdCount++;

                    if (logs) Debug.Log($"Created tower: {tower.name} at {path}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Successfully created {createdCount} new tower variants!");
        }

        private static void SetTowerStats(TowerStats stats, (string name, string desc, float dmg, float firerate, float range, float projSpeed, int priority, bool predictive, int cost, int sell, int upgradeCost) tower)
        {
            var nameField = typeof(TowerStats).GetField("towerName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descField = typeof(TowerStats).GetField("description", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dmgField = typeof(TowerStats).GetField("damage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fireRateField = typeof(TowerStats).GetField("fireRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rangeField = typeof(TowerStats).GetField("range", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var projSpeedField = typeof(TowerStats).GetField("projectileSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var priorityField = typeof(TowerStats).GetField("targetPriority", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var predictiveField = typeof(TowerStats).GetField("predictiveAiming", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var costField = typeof(TowerStats).GetField("cost", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sellField = typeof(TowerStats).GetField("sellValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var upgradeField = typeof(TowerStats).GetField("upgradeCost", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            nameField?.SetValue(stats, tower.name);
            descField?.SetValue(stats, tower.desc);
            dmgField?.SetValue(stats, tower.dmg);
            fireRateField?.SetValue(stats, tower.firerate);
            rangeField?.SetValue(stats, tower.range);
            projSpeedField?.SetValue(stats, tower.projSpeed);
            priorityField?.SetValue(stats, (TargetPriority)tower.priority);
            predictiveField?.SetValue(stats, tower.predictive);
            costField?.SetValue(stats, tower.cost);
            sellField?.SetValue(stats, tower.sell);
            upgradeField?.SetValue(stats, tower.upgradeCost);
        }
    }
}
