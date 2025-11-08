using TD.Monsters;
using TD.Towers;
using TD.UI.Information;
using UnityEngine;
using UnityEngine.Localization;

namespace TD.UI
{
	public class TooltipWorldBridge : MonoBehaviour
	{
		private const string tableName = "UI";

		[SerializeField] private LocalizedString title = new(tableName, "tooltip.object.title");
		[SerializeField] private LocalizedString description = new(tableName, "tooltip.object.description");

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
			if (TryGetComponent<ITooltip>(out var tooltip))
			{
				title = tooltip.Title ?? title;
				description = tooltip.Description;
			}

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
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, tooltipCanvas.worldCamera, out Vector2 localPoint);

			proxyRect.anchoredPosition = localPoint;
			proxyRect.gameObject.SetActive(true);
		}

		private LocalizedString GetLocalizedDescription()
		{
			var tower = GetComponent<Tower>();
			if (tower != null && tower.Stats != null)
			{
				description.Arguments = new object[]
				{
					tower.Stats.Damage,
					tower.Stats.FireDelay,
					tower.Stats.Range,
					tower.TargetPriority.ToString(),
					tower.CurrentTarget != null ? tower.CurrentTarget.name : "-",
					tower.CanUpgrade() ? tower.Stats.UpgradeCost.ToString() : "-"
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