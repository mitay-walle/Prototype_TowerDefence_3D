using UnityEngine;

namespace TD
{
	public interface ITargetable
	{
		GameObject gameObject { get; }
		void OnSelected();
		void OnDeselected();
	}
}