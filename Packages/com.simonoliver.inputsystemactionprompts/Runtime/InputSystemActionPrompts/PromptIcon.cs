using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace InputSystemActionPrompts
{
	[RequireComponent(typeof(Image))]
	public class PromptIcon : PromptBase
	{
		/// <summary>
		/// This should be the full path, including binding map and action, eg "Player/Move"
		/// </summary>
		[HideIf("actionReference")] [SerializeField] private string m_Action = "Player/Move";
		public bool InvertEvent;
		public UnityEvent<bool> onToggleSprite;
		public InputActionReference actionReference;
		public bool _disableObject;
		public bool _disableComponent;
		/// <summary>
		/// The image to apply the prompt sprite to
		/// </summary>
		private Image m_Image;

		[SerializeField] private bool _setNativeSize = true;

		void OnEnable()
		{
			m_Image = GetComponent<Image>();
			if (m_Image == null) return;

			RefreshIcon();

			// Listen to device changing
			InputDevicePromptSystem.OnActiveDeviceChanged += DeviceChanged;
		}

		private void OnDisable()
		{
			// Remove listener
			InputDevicePromptSystem.OnActiveDeviceChanged -= DeviceChanged;
		}

		

		/// <summary>
		/// Called when active input device changed
		/// </summary>
		/// <param name="obj"></param>
		private void DeviceChanged(InputDevice device)
		{
			RefreshIcon();
		}

		/// <summary>
		/// Sets the icon for the current action
		/// </summary>
		[Button]
		private void RefreshIcon()
		{
			if (actionReference)
			{
				var action = actionReference.action;
				var map = action.actionMap;
				m_Action = $"{map.name}/{action.name}";
			}

			var sourceSprite = InputDevicePromptSystem.GetActionPathBindingSprite(m_Action);
			if (_disableObject)
			{
				gameObject.SetActive(sourceSprite != null);
			}

			onToggleSprite?.Invoke(sourceSprite != InvertEvent);
			if (_disableComponent)
			{
				m_Image.enabled = sourceSprite != null;
			}

			if (sourceSprite == null)
				return;

			m_Image.sprite = sourceSprite;

			if (_setNativeSize)
				m_Image.SetNativeSize();
		}

		public override void Refresh() => RefreshIcon();
	}
}