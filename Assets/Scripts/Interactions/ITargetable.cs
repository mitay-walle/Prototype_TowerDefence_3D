using UnityEngine;

namespace TD.Interactions
{
	public interface ITargetable
	{
		GameObject gameObject { get; }
		void OnSelected();
		void OnDeselected();
	}
}