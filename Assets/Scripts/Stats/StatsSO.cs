using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TD.Stats
{
	public abstract class StatsSO : ScriptableObject
	{
		[TableColumnWidth(0), VerticalGroup("Stats")] public int maxGrade = 10;
		[ShowInInspector, PropertyRange(1, nameof(maxGrade))]
		[VerticalGroup("Statistcs"), OnValueChanged("OnStatsChangedEditor")]
		protected int TestGrade = 5;

		[OnValueChanged("OnStatsChangedEditor", true)]
		[SerializeReference] public List<UpgradeRule> upgradeRules = new();

		public event Action OnStatsChangedEvent;

		protected void OnStatsChanged() => OnStatsChangedEvent?.Invoke();

		protected virtual void OnStatsChangedEditor()
		{
			OnStatsChangedEvent?.Invoke();
			TestGradeCalculation();
		}

		public abstract IEnumerable<BaseStatEntry> GetStats();

		protected void TestGradeCalculation()
		{
			if (TestGrade <= 1) return;

			ApplyUpgradeRulesOnly(TestGrade, null);

			foreach (BaseStatEntry entry in GetStats())
			{
				entry.SetTestGrowValue(TestGrade, this);
			}
		}

		public void ApplyUpgradeRulesOnly(int grade, IStats stats)
		{
			foreach (var rule in upgradeRules)
			{
				rule.TryApply(grade, stats);
			}
		}
	}
}