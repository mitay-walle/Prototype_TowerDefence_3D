using TD.Towers;
using TD.UI.Information;
using UnityEngine;
using UnityEngine.Localization;

namespace TD.UI
{
	public static class TowerShopTooltipHelper
	{
		private const string tableName = "UI";

		public  static void SetupTooltip(GameObject button, Tower tower, string towerName)
		{
			var hoverTooltip = button.GetComponent<HoverShowTooltip>();
			if (hoverTooltip == null)
			{
				hoverTooltip = button.AddComponent<HoverShowTooltip>();
			}

			LocalizedString description = new LocalizedString(tableName, "tooltip.tower.shop.description");
			description.Arguments = new object[]
			{
				tower.Stats.statsSO.Damage.BaseValue,
				tower.Stats.statsSO.FireDelay.BaseValue,
				tower.Stats.statsSO.Range.BaseValue,
				tower.Stats.statsSO.ProjectileSpeed.BaseValue,
				tower.TargetPriority.ToString(),
				tower.Cost,
				tower.SellValue
			};

			hoverTooltip.title = tower.Title;
			hoverTooltip.message = description;
		}
	}
}