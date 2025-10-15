using UnityEngine;

namespace Plugins.Utilities
{
	public class LogOnEnable : MonoBehaviour
	{
		private void OnEnable() => Debug.Log($"{name} OnEnable");
		private void OnDisable() => Debug.Log($"{name} OnDisable");
	}
}