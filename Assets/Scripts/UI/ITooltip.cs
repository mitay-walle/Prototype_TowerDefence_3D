using UnityEngine.Localization;

namespace TD.UI
{
	public interface ITooltip
	{
		LocalizedString Title { get; }
		LocalizedString Description { get; }
	}
}