using UnityEditor;
using UnityEngine;

namespace TD.Editor
{
    public static class CreateMetaFiles
    {
        [MenuItem("TD/Automation/Refresh AssetDatabase")]
        public static void RefreshAssets()
        {
            const bool logs = true;

            string[] newAssets = new[]
            {
                "Assets/Resources/TowerStats/FastGun.asset",
                "Assets/Resources/TowerStats/PlasmaBlast.asset",
                "Assets/Resources/TowerStats/LaserTower.asset",
                "Assets/Resources/TowerStats/CryoTower.asset",
                "Assets/Resources/TowerStats/Cannon.asset",
                "Assets/Resources/TowerStats/Tesla.asset"
            };

            foreach (var asset in newAssets)
            {
                AssetDatabase.ImportAsset(asset);
                if (logs) Debug.Log($"Imported: {asset}");
            }

            AssetDatabase.Refresh();
            Debug.Log("Asset database refreshed!");
        }
    }
}
