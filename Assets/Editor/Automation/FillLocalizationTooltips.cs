using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;
using System.Collections.Generic;

namespace TD.Editor
{
    public static class FillLocalizationTooltips
    {
        [MenuItem("TD/Automation/Fill Missing Localization Tooltips")]
        public static void FillMissingTooltips()
        {
            const bool logs = true;

            var sharedDataPath = "Assets/Localization/UI Shared Data.asset";
            SharedTableData sharedData = AssetDatabase.LoadAssetAtPath<SharedTableData>(sharedDataPath);

            if (sharedData == null)
            {
                Debug.LogError($"Could not load UI Shared Data at {sharedDataPath}");
                return;
            }

            var keysToAdd = new List<(string key, long id)>
            {
                ("tooltip.turret.title", 681283936259),
                ("tooltip.turret.description", 681279741952),
                ("tooltip.enemy.title", 681279741953),
                ("tooltip.enemy.description", 681279741954),
                ("tooltip.base.title", 681279741955),
                ("tooltip.base.description", 681283936256),
                ("tooltip.tower.shop.title", 681267159040),
                ("tooltip.tower.shop.description", 681283936260),
                ("tooltip.object.title", 681283936257),
                ("tooltip.object.description", 681283936258),
            };

            var existingEntries = new HashSet<string>();
            foreach (var entry in sharedData.Entries)
            {
                existingEntries.Add(entry.Key);
            }

            int addedCount = 0;
            foreach (var (key, id) in keysToAdd)
            {
                if (!existingEntries.Contains(key))
                {
                    sharedData.AddKey(key, id);
                    addedCount++;
                    if (logs) Debug.Log($"Added localization key: {key} (ID: {id})");
                }
            }

            if (addedCount > 0)
            {
                EditorUtility.SetDirty(sharedData);
                AssetDatabase.SaveAssets();
                Debug.Log($"Successfully added {addedCount} localization keys!");
            }
            else
            {
                Debug.Log("All localization keys already exist.");
            }
        }
    }
}
