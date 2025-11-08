using UnityEngine;

namespace TD.Interactions
{
	public class Targetable : MonoBehaviour, ITargetable
	{
		public bool IsTargetingDirty { get; set; }

		public void OnSelected()
		{
			Debug.Log($"Selected '{name}'");
		}

		public void OnDeselected()
		{
			Debug.Log($"Deselected '{name}'");
		}
	}
}