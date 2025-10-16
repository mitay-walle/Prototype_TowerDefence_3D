using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Plugins.GUI.Information;
using TD.UI;
using UnityEditor.SceneManagement;

namespace TD.Editor
{
	public class SetupTooltipUI
	{
		[MenuItem("TD/Automation/Setup Tooltip UI")]
		public static void Setup()
		{
			Debug.Log("[SetupTooltipUI] Starting tooltip UI setup...");

			var canvasObjects = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
			Canvas mainCanvas = null;

			foreach (var canvas in canvasObjects)
			{
				if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
				{
					mainCanvas = canvas;
					break;
				}
			}

			if (mainCanvas == null)
			{
				Debug.LogError("[SetupTooltipUI] ✗ No ScreenSpaceOverlay Canvas found in scene!");
				return;
			}

			AutoPositionalTooltip tooltipSystem = Object.FindFirstObjectByType<AutoPositionalTooltip>();

			if (tooltipSystem == null)
			{
				GameObject tooltipObj = new GameObject("TooltipSystem");
				tooltipObj.transform.SetParent(mainCanvas.transform, false);

				tooltipSystem = tooltipObj.AddComponent<AutoPositionalTooltip>();

				GameObject visual = new GameObject("Visual");
				visual.transform.SetParent(tooltipObj.transform, false);

				RectTransform visualRect = visual.AddComponent<RectTransform>();
				visualRect.anchorMin = Vector2.zero;
				visualRect.anchorMax = Vector2.zero;
				visualRect.pivot = new Vector2(0, 1);
				visualRect.sizeDelta = new Vector2(300, 150);

				Image bgImage = visual.AddComponent<Image>();
				bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

				VerticalLayoutGroup layoutGroup = visual.AddComponent<VerticalLayoutGroup>();
				layoutGroup.padding = new RectOffset(10, 10, 10, 10);
				layoutGroup.spacing = 5;
				layoutGroup.childAlignment = TextAnchor.UpperLeft;
				layoutGroup.childControlWidth = true;
				layoutGroup.childControlHeight = true;
				layoutGroup.childForceExpandWidth = false;
				layoutGroup.childForceExpandHeight = false;

				ContentSizeFitter sizeFitter = visual.AddComponent<ContentSizeFitter>();
				sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
				sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

				GameObject titleObj = new GameObject("Title");
				titleObj.transform.SetParent(visual.transform, false);
				var titleLocalize = titleObj.AddComponent<UnityEngine.Localization.Components.LocalizeStringEvent>();

				GameObject descObj = new GameObject("Description");
				descObj.transform.SetParent(visual.transform, false);
				var descLocalize = descObj.AddComponent<UnityEngine.Localization.Components.LocalizeStringEvent>();

				var tooltipSO = new SerializedObject(tooltipSystem);
				tooltipSO.FindProperty("_visual").objectReferenceValue = visual;
				tooltipSO.FindProperty("titleEvent").objectReferenceValue = titleLocalize;
				tooltipSO.FindProperty("messageEvent").objectReferenceValue = descLocalize;
				tooltipSO.ApplyModifiedProperties();

				Debug.Log("[SetupTooltipUI]   ✓ Created AutoPositionalTooltip system");
			}
			else
			{
				Debug.Log("[SetupTooltipUI]   → AutoPositionalTooltip already exists");
			}

			var towerShopUI = Object.FindFirstObjectByType<TowerShopUI>();

			if (towerShopUI != null)
			{
				var helper = towerShopUI.GetComponent<TowerShopTooltipHelper>();
				if (helper == null)
				{
					helper = towerShopUI.gameObject.AddComponent<TowerShopTooltipHelper>();

					var helperSO = new SerializedObject(helper);
					helperSO.FindProperty("tableName").stringValue = "UI";
					helperSO.FindProperty("tooltipSystem").objectReferenceValue = tooltipSystem;
					helperSO.ApplyModifiedProperties();

					var shopSO = new SerializedObject(towerShopUI);
					shopSO.FindProperty("tooltipHelper").objectReferenceValue = helper;
					shopSO.ApplyModifiedProperties();

					Debug.Log("[SetupTooltipUI]   ✓ Added TowerShopTooltipHelper to TowerShopUI");
				}
				else
				{
					Debug.Log("[SetupTooltipUI]   → TowerShopTooltipHelper already exists");
				}
			}
			else
			{
				Debug.LogWarning("[SetupTooltipUI]   ⚠ TowerShopUI not found in scene");
			}

			EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

			Debug.Log("[SetupTooltipUI] ✓ Tooltip UI setup complete!");
		}
	}
}
