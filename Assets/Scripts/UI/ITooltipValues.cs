using System;
using UnityEngine.Localization;

namespace TD.UI
{
	public interface ITooltipValues
	{
		LocalizedString Title { get; }
		LocalizedString Description { get; }
		Action OnTooltipButtonClick { get; }
		LocalizedString TooltipButtonText { get; }
	}
}