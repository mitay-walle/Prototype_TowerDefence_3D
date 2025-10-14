using UnityEngine;

namespace TD
{
	public class Targetable : MonoBehaviour, ITargetable
	{
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