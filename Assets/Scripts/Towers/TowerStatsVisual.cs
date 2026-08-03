using System;
using UnityEngine;

namespace TD.Towers
{
	[Serializable]
	public class TowerStatsVisual
	{
		[SerializeField] GameObject _range;

		public void Show(Tower tower)
		{
			_range.transform.localScale = Vector3.one * tower.EffectiveRange * 2;
			_range.SetActive(true);
		}

		public void Hide()
		{
			_range.SetActive(false);
		}
	}
}