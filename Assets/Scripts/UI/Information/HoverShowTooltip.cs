using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

namespace Plugins.GUI.Information
{
	public class HoverShowTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		[Required] public LocalizedString title;
		[Required] public LocalizedString message;
		private AutoPositionalTooltip tooltip;

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (tooltip == null)
			{
				tooltip = FindAnyObjectByType<AutoPositionalTooltip>();
			}

			tooltip.Show(transform as RectTransform, title, message);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (tooltip == null)
			{
				tooltip = FindAnyObjectByType<AutoPositionalTooltip>();
			}

			tooltip.Hide();
		}
	}
}