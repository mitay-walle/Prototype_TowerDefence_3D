using TD.Towers;
using TD.UI.Information;
using UnityEngine;
using UnityEngine.Localization;

namespace TD.UI
{
	public static class TowerShopTooltipHelper
	{
		private const string tableName = "UI";

		public static void SetupTooltip(GameObject button, Tower tower)
		{
			var hoverTooltip = button.GetComponent<HoverShowTooltip>();
			if (hoverTooltip == null)
			{
				hoverTooltip = button.AddComponent<HoverShowTooltip>();
			}

			LocalizedString description = new LocalizedString(tableName, "tooltip.tower.shop.description");
			description.Arguments = new object[]
			{
				tower.Stats.statsSO.Damage.BaseValueInt,
				tower.Stats.statsSO.FireRate.BaseValueInt,
				tower.EffectiveRange,
				tower.Stats.statsSO.ProjectileSpeed.BaseValueInt,
				tower.TargetPriority.ToString(),
				tower.Stats.statsSO.Cost,
				tower.SellValue,
				tower.Stats.statsSO.Role.IsEmpty ? string.Empty : tower.Stats.statsSO.Role.GetLocalizedString(),
				tower.Stats.statsSO.DefensiveIdentity.IsEmpty ? string.Empty : tower.Stats.statsSO.DefensiveIdentity.GetLocalizedString()
			};

			hoverTooltip.title = tower.Title;
			hoverTooltip.message = description;
		}
	}
}