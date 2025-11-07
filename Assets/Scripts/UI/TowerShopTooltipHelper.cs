using TD.Towers;
using TD.UI.Information;
using UnityEngine;
using UnityEngine.Localization;

namespace TD.UI
{
	public class TowerShopTooltipHelper : MonoBehaviour
	{
		private const string TOOLTIP_TABLE = "Table name for localization strings";

		[Tooltip(TOOLTIP_TABLE)]
		[SerializeField] private string tableName = "UI";

		public void SetupTooltip(GameObject button, TowerStats stats, string towerName)
		{
			var hoverTooltip = button.GetComponent<HoverShowTooltip>();
			if (hoverTooltip == null)
			{
				hoverTooltip = button.AddComponent<HoverShowTooltip>();
			}

			LocalizedString title = new LocalizedString(tableName, stats.TowerName);

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

			hoverTooltip.title = title;
			hoverTooltip.message = description;
		}
	}
}