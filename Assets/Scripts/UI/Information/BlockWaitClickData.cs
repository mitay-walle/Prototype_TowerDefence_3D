using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Localization;

namespace Plugins.GUI.Information
{
	[CreateAssetMenu(fileName = "New BlockWaitClick Data", menuName = "Onboarding/BlockWaitClick Data")]
	public class BlockWaitClickData : ScriptableObject
	{
		[SerializeField, TextArea(2, 4)] public string description;
		[SerializeField, Required] public string transformPath;
		[SerializeField] public LocalizedString tooltipTitle;
		[SerializeField] public LocalizedString tooltipMessage;
	}
}