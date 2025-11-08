using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TD.Stats
{
	public abstract class StatsSO : ScriptableObject
	{
		public int maxGrade = 10;
		[ShowInInspector, PropertyRange(1, nameof(maxGrade)), OnValueChanged("TestGradeCalculation")]
		private int TestGrade = 5;

		[SerializeReference] public List<UpgradeRule> upgradeRules = new();

		public event Action OnStatsChangedEvent;

		private void OnStatsChanged() => OnStatsChangedEvent?.Invoke();

		public abstract IEnumerable<BaseStatEntry> GetStats();

		private void TestGradeCalculation()
		{
			ApplyUpgrade(TestGrade, null);

			foreach (BaseStatEntry entry in GetStats())
			{
				entry.SetTestGrowValue(TestGrade, this);
			}
		}

		public void ApplyUpgrade(int grade, IStats stats)
		{
			foreach (var rule in upgradeRules)
			{
				rule.TryApply(grade, stats);
			}
		}
	}
}