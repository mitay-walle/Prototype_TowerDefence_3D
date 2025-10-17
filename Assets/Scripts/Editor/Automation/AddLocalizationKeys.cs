using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace TD.Editor
{
    public static class AddLocalizationKeys
    {
        [MenuItem("TD/Automation/Add Localization Keys")]
        public static void AddKeys()
        {
            const bool logs = true;

            string sharedDataPath = "Assets/Localization/UI Shared Data.asset";

            if (!File.Exists(sharedDataPath))
            {
                Debug.LogError($"File not found: {sharedDataPath}");
                return;
            }

            string content = File.ReadAllText(sharedDataPath);
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

            int addedCount = 0;
            foreach (var (key, id) in keysToAdd)
            {
                string searchPattern = $"m_Key: {key}";
                if (!content.Contains(searchPattern))
                {
                    string entryYaml = $"  - m_Id: {id}\n    m_Key: {key}\n";
                    int insertIndex = content.LastIndexOf("  - m_Id:");

                    if (insertIndex > 0)
                    {
                        int endLine = content.IndexOf("\n", insertIndex) + 1;
                        if (endLine > insertIndex)
                        {
                            insertIndex = endLine;
                            while (insertIndex < content.Length && content[insertIndex] == ' ')
                                insertIndex++;

                            if (insertIndex > 0 && content[insertIndex - 1] == '\n')
                            {
                                content = content.Insert(insertIndex, entryYaml);
                                addedCount++;
                                if (logs) Debug.Log($"Added key: {key}");
                            }
                        }
                    }
                }
            }

            if (addedCount > 0)
            {
                File.WriteAllText(sharedDataPath, content);
                AssetDatabase.ImportAsset(sharedDataPath);
                Debug.Log($"Successfully added {addedCount} localization keys!");
            }
            else
            {
                Debug.Log("All keys already exist.");
            }
        }
    }
}
