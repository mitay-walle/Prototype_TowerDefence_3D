using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TD.Stats
{
	[OnValueChanged("OnStatsChanged", true)]
	public abstract class StatsSO : ScriptableObject
	{
		public int maxGrade = 10;
		[ShowInInspector, PropertyRange(1, nameof(maxGrade)), OnValueChanged("TestGradeCalculation")]
		private int TestGrade = 5;

		public event Action OnStatsChangedEvent;

		private void OnStatsChanged() => OnStatsChangedEvent?.Invoke();

		public abstract IEnumerable<BaseStatEntry> GetStats();

		private void TestGradeCalculation()
		{
			foreach (BaseStatEntry entry in GetStats())
			{
				entry.SetTestGrowValue(TestGrade,this);
			}
		}
	}
}