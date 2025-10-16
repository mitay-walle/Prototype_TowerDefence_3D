using UnityEditor;
using UnityEngine;
using TD.UI;

namespace TD.Editor
{
	public class SetupTooltipComponents
	{
		[MenuItem("TD/Automation/Setup Tooltip Components")]
		public static void Setup()
		{
			Debug.Log("[SetupTooltipComponents] Starting tooltip components setup...");

			SetupTowerPrefabs();
			SetupEnemyPrefabs();
			SetupBasePrefab();

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			Debug.Log("[SetupTooltipComponents] ✓ Tooltip components setup complete!");
		}

		private static void SetupTowerPrefabs()
		{
			string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Prefabs/Towers" });

			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

				if (prefab == null) continue;

				var turret = prefab.GetComponent<Turret>();
				if (turret == null) continue;

				var tooltip = prefab.GetComponent<WorldTooltipBridge>();
				if (tooltip == null)
				{
					GameObject prefabInstance = PrefabUtility.LoadPrefabContents(path);

					tooltip = prefabInstance.GetComponent<WorldTooltipBridge>();
					if (tooltip == null)
					{
						tooltip = prefabInstance.AddComponent<WorldTooltipBridge>();
					}

					var tooltipSO = new SerializedObject(tooltip);
					tooltipSO.FindProperty("titleKey").stringValue = "tooltip.turret.title";
					tooltipSO.FindProperty("descriptionKey").stringValue = "tooltip.turret.description";
					tooltipSO.FindProperty("tableName").stringValue = "UI";
					tooltipSO.ApplyModifiedProperties();

					PrefabUtility.SaveAsPrefabAsset(prefabInstance, path);
					PrefabUtility.UnloadPrefabContents(prefabInstance);

					Debug.Log($"[SetupTooltipComponents]   ✓ Added WorldTooltipBridge to {prefab.name}");
				}
				else
				{
					Debug.Log($"[SetupTooltipComponents]   → {prefab.name} already has WorldTooltipBridge");
				}
			}
		}

		private static void SetupEnemyPrefabs()
		{
			string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Prefabs/Enemies" });

			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

				if (prefab == null) continue;

				var enemyHealth = prefab.GetComponent<EnemyHealth>();
				if (enemyHealth == null) continue;

				var tooltip = prefab.GetComponent<WorldTooltipBridge>();
				if (tooltip == null)
				{
					GameObject prefabInstance = PrefabUtility.LoadPrefabContents(path);

					tooltip = prefabInstance.GetComponent<WorldTooltipBridge>();
					if (tooltip == null)
					{
						tooltip = prefabInstance.AddComponent<WorldTooltipBridge>();
					}

					var tooltipSO = new SerializedObject(tooltip);
					tooltipSO.FindProperty("titleKey").stringValue = "tooltip.enemy.title";
					tooltipSO.FindProperty("descriptionKey").stringValue = "tooltip.enemy.description";
					tooltipSO.FindProperty("tableName").stringValue = "UI";
					tooltipSO.ApplyModifiedProperties();

					PrefabUtility.SaveAsPrefabAsset(prefabInstance, path);
					PrefabUtility.UnloadPrefabContents(prefabInstance);

					Debug.Log($"[SetupTooltipComponents]   ✓ Added WorldTooltipBridge to {prefab.name}");
				}
				else
				{
					Debug.Log($"[SetupTooltipComponents]   → {prefab.name} already has WorldTooltipBridge");
				}
			}
		}

		private static void SetupBasePrefab()
		{
			string path = "Assets/Prefabs/Main/PlayerBase.prefab";
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

			if (prefab == null)
			{
				Debug.LogWarning($"[SetupTooltipComponents]   ⚠ PlayerBase prefab not found at {path}");
				return;
			}

			var playerBase = prefab.GetComponent<Base>();
			if (playerBase == null)
			{
				Debug.LogWarning($"[SetupTooltipComponents]   ⚠ PlayerBase prefab doesn't have Base component");
				return;
			}

			var tooltip = prefab.GetComponent<WorldTooltipBridge>();
			if (tooltip == null)
			{
				GameObject prefabInstance = PrefabUtility.LoadPrefabContents(path);

				tooltip = prefabInstance.GetComponent<WorldTooltipBridge>();
				if (tooltip == null)
				{
					tooltip = prefabInstance.AddComponent<WorldTooltipBridge>();
				}

				var tooltipSO = new SerializedObject(tooltip);
				tooltipSO.FindProperty("titleKey").stringValue = "tooltip.base.title";
				tooltipSO.FindProperty("descriptionKey").stringValue = "tooltip.base.description";
				tooltipSO.FindProperty("tableName").stringValue = "UI";
				tooltipSO.ApplyModifiedProperties();

				PrefabUtility.SaveAsPrefabAsset(prefabInstance, path);
				PrefabUtility.UnloadPrefabContents(prefabInstance);

				Debug.Log($"[SetupTooltipComponents]   ✓ Added WorldTooltipBridge to PlayerBase");
			}
			else
			{
				Debug.Log($"[SetupTooltipComponents]   → PlayerBase already has WorldTooltipBridge");
			}
		}
	}
}
