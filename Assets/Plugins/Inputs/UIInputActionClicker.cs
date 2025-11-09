using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TD.Plugins.Inputs
{
	public class UIInputActionClicker : MonoBehaviour
	{
		public InputActionReference actionReference;

		private void OnEnable()
		{
			if (actionReference != null && actionReference.action != null)
			{
				actionReference.action.performed += OnActionPerformed;
				if (!actionReference.action.enabled)
					actionReference.action.Enable();
			}
		}

		private void OnDisable()
		{
			if (actionReference != null && actionReference.action != null)
				actionReference.action.performed -= OnActionPerformed;
		}

		private void OnActionPerformed(InputAction.CallbackContext context)
		{
			// Создаём PointerEventData для EventSystem
			var pointerData = new PointerEventData(EventSystem.current)
			{
				pointerId = -1 // виртуальный указатель
			};

			// Прокидываем событие по иерархии объекта
			ExecuteEvents.ExecuteHierarchy(gameObject, pointerData, ExecuteEvents.pointerClickHandler);
		}
	}
}