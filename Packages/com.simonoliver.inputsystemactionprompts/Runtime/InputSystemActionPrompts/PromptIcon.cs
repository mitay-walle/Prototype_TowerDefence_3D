using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
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
		public Image[] _other;
		public InputActionReference actionReference;
		public UnityEvent<bool> onToggleSprite;
		public UnityEvent<float> onHoldProgress;
		public bool InvertEvent;
		public bool _disableObject;
		public bool _disableComponent;
		/// <summary>
		/// The image to apply the prompt sprite to
		/// </summary>
		private Image m_Image;
		float holdDuration;
		float lastPressTime;

		[SerializeField] private bool _setNativeSize = true;

		void OnEnable()
		{
			m_Image = GetComponent<Image>();
			if (m_Image == null) return;

			RefreshIcon();

			if (actionReference != null)
			{
				var interactions = actionReference.action.interactions.Split(',');

				foreach (var interaction in interactions)
				{
					if (!interaction.StartsWith("Hold"))
					{
						continue;
					}

					if (!interaction.Contains('('))
					{
						holdDuration = InputSystem.settings.defaultHoldTime;
						continue;
					}

					var durationPart = interaction.Split('(').LastOrDefault()?.TrimEnd(')');
					if (string.IsNullOrEmpty(durationPart))
					{
						holdDuration = InputSystem.settings.defaultHoldTime;
						continue;
					}

					var args = durationPart.Split('=');
					if (args.Length == 2 && args[0] == "duration")
						if (float.TryParse(args[1], out var duration))
						{
							Debug.Log("Hold duration = " + duration);
							holdDuration = duration;
						}
				}
			}

			// Listen to device changing
			InputDevicePromptSystem.OnActiveDeviceChanged += DeviceChanged;
		}

		private void OnDisable()
		{
			// Remove listener
			InputDevicePromptSystem.OnActiveDeviceChanged -= DeviceChanged;
		}

		protected override void Update()
		{
			base.Update();
			UpdateProgress();
		}

		private void UpdateProgress()
		{
			if (!actionReference) return;
			if (holdDuration <= 0) return;

			if (actionReference.action.WasPressedThisFrame())
			{
				lastPressTime = Time.time;
			}

			if (!actionReference.action.IsPressed())
			{
				if (Time.time - lastPressTime < holdDuration)
				{
					onHoldProgress?.Invoke(0);
				}
				return;
			}

			onHoldProgress?.Invoke((Time.time - lastPressTime) / holdDuration);
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
			onHoldProgress?.Invoke(0);
			if (actionReference)
			{
				var action = actionReference.action;
				var map = action.actionMap;
				m_Action = $"{map.name}/{action.name}";
			}

			var sourceSprite = InputDevicePromptSystem.GetActionPathBindingSprite(m_Action);
			if (_disableObject)
			{
				gameObject.SetActive(sourceSprite);
			}

			onToggleSprite?.Invoke(sourceSprite != InvertEvent);
			if (_disableComponent)
			{
				m_Image.enabled = sourceSprite;

				foreach (Image image in _other)
				{
					image.enabled = sourceSprite;
				}
			}

			if (sourceSprite == null)
				return;

			m_Image.sprite = sourceSprite;

			foreach (Image image in _other)
			{
				image.sprite = sourceSprite;
			}

			if (_setNativeSize)
				m_Image.SetNativeSize();
		}

		public override void Refresh() => RefreshIcon();
	}
}