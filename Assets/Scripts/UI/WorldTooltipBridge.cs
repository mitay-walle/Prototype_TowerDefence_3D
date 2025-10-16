using UnityEngine;
using UnityEngine.Localization;
using Plugins.GUI.Information;

namespace TD.UI
{
	public class WorldTooltipBridge : MonoBehaviour
	{
		private const string TOOLTIP_TITLE_KEY = "Localization key for tooltip title";
		private const string TOOLTIP_DESC_KEY = "Localization key for tooltip description (use {0}, {1} for dynamic values)";
		private const string TOOLTIP_TABLE = "Table name for localization strings";

		[Tooltip(TOOLTIP_TITLE_KEY)]
		[SerializeField] private string titleKey = "tooltip.object.title";
		[Tooltip(TOOLTIP_DESC_KEY)]
		[SerializeField] private string descriptionKey = "tooltip.object.description";
		[Tooltip(TOOLTIP_TABLE)]
		[SerializeField] private string tableName = "UI";

		private AutoPositionalTooltip tooltipSystem;
		private RectTransform proxyRect;
		private Canvas tooltipCanvas;
		private Camera mainCamera;

		private void Awake()
		{
			mainCamera = Camera.main;
			FindTooltipSystem();
			CreateProxyRect();
		}

		private void FindTooltipSystem()
		{
			tooltipSystem = FindFirstObjectByType<AutoPositionalTooltip>();

			if (tooltipSystem != null)
			{
				tooltipCanvas = tooltipSystem.GetComponentInParent<Canvas>();
			}
		}

		private void CreateProxyRect()
		{
			if (tooltipCanvas == null) return;

			GameObject proxy = new GameObject($"TooltipProxy_{gameObject.name}");
			proxy.transform.SetParent(tooltipCanvas.transform, false);
			proxyRect = proxy.AddComponent<RectTransform>();
			proxyRect.sizeDelta = Vector2.one;
			proxyRect.gameObject.SetActive(false);
		}

		public void ShowTooltip()
		{
			if (tooltipSystem == null || proxyRect == null) return;

			UpdateProxyPosition();

			LocalizedString title = new LocalizedString(tableName, titleKey);
			LocalizedString description = GetLocalizedDescription();

			proxyRect.gameObject.SetActive(true);
			tooltipSystem.Show(proxyRect, title, description);
		}

		public void HideTooltip()
		{
			if (tooltipSystem == null || proxyRect == null) return;

			tooltipSystem.Hide();
			proxyRect.gameObject.SetActive(false);
		}

		private void Update()
		{
			if (proxyRect != null && proxyRect.gameObject.activeSelf)
			{
				UpdateProxyPosition();
			}
		}

		private void UpdateProxyPosition()
		{
			if (mainCamera == null || proxyRect == null || tooltipCanvas == null) return;

			Vector3 worldPosition = transform.position;
			Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

			if (screenPosition.z < 0)
			{
				proxyRect.gameObject.SetActive(false);
				return;
			}

			RectTransform canvasRect = tooltipCanvas.transform as RectTransform;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				canvasRect,
				screenPosition,
				tooltipCanvas.worldCamera,
				out Vector2 localPoint
			);

			proxyRect.anchoredPosition = localPoint;
			proxyRect.gameObject.SetActive(true);
		}

		private LocalizedString GetLocalizedDescription()
		{
			LocalizedString description = new LocalizedString(tableName, descriptionKey);

			var turret = GetComponent<Turret>();
			if (turret != null && turret.Stats != null)
			{
				description.Arguments = new object[]
				{
					turret.Stats.Damage,
					turret.Stats.FireRate,
					turret.Stats.Range,
					turret.Stats.TargetPriority.ToString(),
					turret.CurrentTarget != null ? turret.CurrentTarget.name : "-",
					turret.CanUpgrade() ? turret.Stats.UpgradeCost.ToString() : "-"
				};
			}

			var enemyHealth = GetComponent<EnemyHealth>();
			if (enemyHealth != null)
			{
				description.Arguments = new object[]
				{
					enemyHealth.CurrentHealth,
					enemyHealth.MaxHealth
				};
			}

			var playerBase = GetComponent<Base>();
			if (playerBase != null)
			{
				description.Arguments = new object[]
				{
					playerBase.CurrentHealth,
					playerBase.MaxHealth
				};
			}

			return description;
		}

		private void OnDestroy()
		{
			if (proxyRect != null)
			{
				Destroy(proxyRect.gameObject);
			}
		}

		private void OnDisable()
		{
			HideTooltip();
		}
	}
}
