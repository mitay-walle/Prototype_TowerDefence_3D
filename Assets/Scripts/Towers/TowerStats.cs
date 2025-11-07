using Sirenix.OdinInspector;
using TD.Weapons;
using UnityEngine;

namespace TD.Towers
{
    [CreateAssetMenu(fileName = "TowerStats", menuName = "TD/Tower Stats", order = 0)]
    public class TowerStats : ScriptableObject
    {
        [SerializeField] private string towerName = "Basic Tower";

        [TextArea(2, 4)]
        [SerializeField] private string description = "A basic tower";

        [SerializeField, Min(0)] private float damage = 10f;

        [SerializeField, Min(0)] private float fireRate = 1f;

        [SerializeField, Min(0)] private float range = 10f;

        [SerializeField, Min(0)] private float projectileSpeed = 20f;

        [SerializeField] private MonoBehaviour weaponComponent;

        [SerializeField] private TargetPriority targetPriority = TargetPriority.Nearest;

        [SerializeField] private bool predictiveAiming = false;

        [SerializeField, Min(0)] private int cost = 100;

        [SerializeField, Min(0)] private int sellValue = 50;

        [SerializeField] private TowerStats nextUpgrade;

        [SerializeField, Min(0)] private int upgradeCost = 150;

        public string TowerName => towerName;
        public string Description => description;
        public float Damage => damage;
        public float FireRate => fireRate;
        public float Range => range;
        public float ProjectileSpeed => projectileSpeed;
        public IWeapon Weapon => weaponComponent as IWeapon;
        public TargetPriority TargetPriority => targetPriority;
        public bool PredictiveAiming => predictiveAiming;
        public int Cost => cost;
        public int SellValue => sellValue;
        public TowerStats NextUpgrade => nextUpgrade;
        public int UpgradeCost => upgradeCost;
        public bool CanUpgrade => nextUpgrade != null;
        public float FireDelay => 1f / fireRate;

        [Button("Create Upgrade Level")]
        private void CreateUpgradeLevel()
        {
            #if UNITY_EDITOR
            var upgrade = CreateInstance<TowerStats>();
            upgrade.towerName = towerName + " (Upgraded)";
            upgrade.description = description;
            upgrade.damage = damage * 1.5f;
            upgrade.fireRate = fireRate * 1.2f;
            upgrade.range = range * 1.1f;
            upgrade.projectileSpeed = projectileSpeed * 1.1f;
            upgrade.targetPriority = targetPriority;
            upgrade.predictiveAiming = predictiveAiming;
            upgrade.cost = cost;
            upgrade.sellValue = Mathf.RoundToInt(cost * 0.7f + upgradeCost * 0.5f);
            upgrade.upgradeCost = Mathf.RoundToInt(upgradeCost * 1.5f);

            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            string directory = System.IO.Path.GetDirectoryName(path);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path) + "_Upgrade";
            string newPath = System.IO.Path.Combine(directory, fileName + ".asset");

            UnityEditor.AssetDatabase.CreateAsset(upgrade, newPath);
            nextUpgrade = upgrade;
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
    }

    public enum TargetPriority
    {
        Nearest,        // Closest enemy
        Farthest,       // Enemy closest to base
        Strongest,      // Highest HP
        Weakest         // Lowest HP
    }
}
