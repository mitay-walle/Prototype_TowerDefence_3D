using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

namespace TD.UI.Information
{
	public class HoverShowTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
	{
		[Required] public LocalizedString title;
		[Required] public LocalizedString message;
		private AutoPositionalTooltip tooltip;

		void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) => OnSelect(eventData);
		void IPointerExitHandler.OnPointerExit(PointerEventData eventData) => OnDeselect(eventData);

		public void OnSelect(BaseEventData eventData)
		{
			if (tooltip == null)
			{
				tooltip = FindAnyObjectByType<AutoPositionalTooltip>();
			}

			tooltip.Show(transform as RectTransform, title, message);
		}

		public void OnDeselect(BaseEventData eventData)
		{
			if (tooltip == null)
			{
				tooltip = FindAnyObjectByType<AutoPositionalTooltip>();
			}

			tooltip.Hide();
		}
	}
}