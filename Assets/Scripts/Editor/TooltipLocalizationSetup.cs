using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace TD.Editor
{
	public class TooltipLocalizationSetup
	{
		[MenuItem("TD/Automation/Setup Tooltip Localization")]
		public static void SetupLocalization()
		{
			Debug.Log("[TooltipLocalizationSetup] Starting tooltip localization setup...");

			var collection = LocalizationEditorSettings.GetStringTableCollection("UI");

			if (collection == null)
			{
				Debug.Log("[TooltipLocalizationSetup] Creating new 'UI' string table collection...");
				collection = LocalizationEditorSettings.CreateStringTableCollection("UI", "Assets/Localization");
			}

			AddTooltipStrings(collection);

			EditorUtility.SetDirty(collection);
			AssetDatabase.SaveAssets();

			Debug.Log("[TooltipLocalizationSetup] ✓ Tooltip localization setup complete!");
		}

		private static void AddTooltipStrings(StringTableCollection collection)
		{
			AddOrUpdateEntry(collection, "tooltip.turret.title", "Tower", "Башня");
			AddOrUpdateEntry(collection, "tooltip.turret.description",
				"<b>Stats:</b>\nDamage: {0}\nFire Rate: {1:F2}/s\nRange: {2:F1}\nPriority: {3}\n\n<color=red>Target: {4}</color>\n\n<color=yellow>Upgrade: ${5}</color>",
				"<b>Характеристики:</b>\nУрон: {0}\nСкорострельность: {1:F2}/с\nДальность: {2:F1}\nПриоритет: {3}\n\n<color=red>Цель: {4}</color>\n\n<color=yellow>Улучшение: ${5}</color>");

			AddOrUpdateEntry(collection, "tooltip.enemy.title", "Enemy", "Враг");
			AddOrUpdateEntry(collection, "tooltip.enemy.description",
				"<b>Health:</b> {0:F0}/{1:F0}",
				"<b>Здоровье:</b> {0:F0}/{1:F0}");

			AddOrUpdateEntry(collection, "tooltip.base.title", "Base", "База");
			AddOrUpdateEntry(collection, "tooltip.base.description",
				"<b>Health:</b> {0}/{1}",
				"<b>Здоровье:</b> {0}/{1}");

			AddOrUpdateEntry(collection, "tooltip.object.title", "Object", "Объект");
			AddOrUpdateEntry(collection, "tooltip.object.description", "No description", "Нет описания");

			AddOrUpdateEntry(collection, "tooltip.tower.shop.title", "Tower", "Башня");
			AddOrUpdateEntry(collection, "tooltip.tower.shop.description",
				"<b>Stats:</b>\nDamage: {0}\nFire Rate: {1:F2}/s\nRange: {2:F1}\nSpeed: {3}\nPriority: {4}\n\n<b>Cost:</b> <color=yellow>${5}</color>\n<b>Sell:</b> ${6}",
				"<b>Характеристики:</b>\nУрон: {0}\nСкорострельность: {1:F2}/с\nДальность: {2:F1}\nСкорость: {3}\nПриоритет: {4}\n\n<b>Цена:</b> <color=yellow>${5}</color>\n<b>Продажа:</b> ${6}");
		}

		private static void AddOrUpdateEntry(StringTableCollection collection, string key, string englishValue, string russianValue)
		{
			var englishTable = collection.GetTable("en") as StringTable;
			var russianTable = collection.GetTable("ru") as StringTable;

			if (englishTable != null)
			{
				var entry = englishTable.GetEntry(key);
				if (entry == null)
				{
					englishTable.AddEntry(key, englishValue);
					Debug.Log($"[TooltipLocalizationSetup]   Added EN: {key}");
				}
				else
				{
					entry.Value = englishValue;
					Debug.Log($"[TooltipLocalizationSetup]   Updated EN: {key}");
				}
				EditorUtility.SetDirty(englishTable);
			}

			if (russianTable != null)
			{
				var entry = russianTable.GetEntry(key);
				if (entry == null)
				{
					russianTable.AddEntry(key, russianValue);
					Debug.Log($"[TooltipLocalizationSetup]   Added RU: {key}");
				}
				else
				{
					entry.Value = russianValue;
					Debug.Log($"[TooltipLocalizationSetup]   Updated RU: {key}");
				}
				EditorUtility.SetDirty(russianTable);
			}
		}
	}
}
