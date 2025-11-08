using System;
using TD.Stats;
using UnityEngine;

namespace TD.Towers
{
	[Serializable]
	public class TowerStatsVisual
	{
		[SerializeField] GameObject _range;

		public void Show(TowerStats stats)
		{
			_range.transform.localScale = Vector3.one * stats.Range * 2;
			_range.SetActive(true);
		}

		public void Hide()
		{
			_range.SetActive(false);
		}
	}
}