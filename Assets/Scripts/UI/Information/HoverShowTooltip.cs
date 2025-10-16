using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

namespace Plugins.GUI.Information
{
	public class HoverShowTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField] private LocalizedString title;
		[SerializeField] private LocalizedString message;
		[SerializeField] private AutoPositionalTooltip tooltip; // назначаешь из инспектора

		public void OnPointerEnter(PointerEventData eventData)
		{
			tooltip.Show(transform as RectTransform, title, message);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			tooltip.Hide();
		}
	}
}