using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using TMPro;

namespace InputSystemActionPrompts
{
	[RequireComponent(typeof(TextMeshProUGUI))]
	public class PromptLocalizedText : PromptBase
	{
		[SerializeField] private InputActionReference m_InputActionAsset;
		[SerializeField] private LocalizedString m_LocalizedText;

		/// <summary>
		/// Cached TextMeshProUGUI component that we'll apply the prompt sprites to
		/// </summary>
		private TextMeshProUGUI m_TextField;

		/// <summary>
		/// The current raw localized string value
		/// </summary>
		private string m_CurrentLocalizedText;

		void OnEnable()
		{
			m_TextField = GetComponent<TextMeshProUGUI>();
			if (m_TextField == null) return;

			// Listen to localization string update
			m_LocalizedText.StringChanged -= OnLocalizedStringChanged;
			m_LocalizedText.StringChanged += OnLocalizedStringChanged;

			// Listen to device changing
			InputDevicePromptSystem.OnActiveDeviceChanged -= OnDeviceChanged;
			InputDevicePromptSystem.OnActiveDeviceChanged += OnDeviceChanged;

			// Apply first localized string
			UpdateLocalizedText(m_LocalizedText.GetLocalizedString());
		}

		private void OnDisable()
		{
			// Cleanup listeners
			m_LocalizedText.StringChanged -= OnLocalizedStringChanged;
			InputDevicePromptSystem.OnActiveDeviceChanged -= OnDeviceChanged;
		}

		/// <summary>
		/// Called when the localized string is updated (e.g., language changed)
		/// </summary>
		/// <param name="newValue"></param>
		private void OnLocalizedStringChanged(string newValue)
		{
			UpdateLocalizedText(newValue);
		}

		/// <summary>
		/// Called when the active input device changes
		/// </summary>
		/// <param name="device"></param>
		private void OnDeviceChanged(InputDevice device)
		{
			RefreshText();
		}

		/// <summary>
		/// Updates the cached localized text and refreshes the prompts
		/// </summary>
		/// <param name="localizedText"></param>
		private void UpdateLocalizedText(string localizedText)
		{
			m_CurrentLocalizedText = localizedText;
			RefreshText();
		}

		/// <summary>
		/// Applies the prompt sprites to the localized text
		/// </summary>
		private void RefreshText()
		{
			if (m_TextField == null || string.IsNullOrEmpty(m_CurrentLocalizedText)) return;
			m_TextField.text = InputDevicePromptSystem.InsertPromptSprites(m_CurrentLocalizedText);
		}

		public override void Refresh() => RefreshText();
	}
}