using System;
using System.Collections.Generic;
using UnityEngine.Localization;

namespace TD.UI
{
	public interface ITooltipValues
	{
		LocalizedString Title { get; }
		LocalizedString Description { get; }
		IEnumerable<(Action, LocalizedString)> OnTooltipButtonClick { get; }
	}
}