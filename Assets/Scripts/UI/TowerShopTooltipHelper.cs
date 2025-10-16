using UnityEngine;
using UnityEngine.Localization;
using Plugins.GUI.Information;

namespace TD.UI
{
	public class TowerShopTooltipHelper : MonoBehaviour
	{
		private const string TOOLTIP_TABLE = "Table name for localization strings";

		[Tooltip(TOOLTIP_TABLE)]
		[SerializeField] private string tableName = "UI";
		[SerializeField] private AutoPositionalTooltip tooltipSystem;

		public void SetupTooltip(GameObject button, TowerStats stats, string towerName)
		{
			if (tooltipSystem == null)
			{
				tooltipSystem = FindFirstObjectByType<AutoPositionalTooltip>();
			}

			if (tooltipSystem == null)
			{
				Debug.LogWarning("[TowerShopTooltipHelper] AutoPositionalTooltip not found in scene!");
				return;
			}

			var hoverTooltip = button.GetComponent<HoverShowTooltip>();
			if (hoverTooltip == null)
			{
				hoverTooltip = button.AddComponent<HoverShowTooltip>();
			}

			LocalizedString title = new LocalizedString(tableName, "tooltip.tower.shop.title");
			title.Arguments = new object[] { towerName };

			LocalizedString description = new LocalizedString(tableName, "tooltip.tower.shop.description");
			description.Arguments = new object[]
			{
				stats.Damage,
				stats.FireRate,
				stats.Range,
				stats.ProjectileSpeed,
				stats.TargetPriority.ToString(),
				stats.Cost,
				stats.SellValue
			};

			typeof(HoverShowTooltip).GetField("title", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hoverTooltip, title);
			typeof(HoverShowTooltip).GetField("message", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hoverTooltip, description);
			typeof(HoverShowTooltip).GetField("tooltip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hoverTooltip, tooltipSystem);
		}
	}
}
