using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TD.Stats
{
	[Serializable, InlineProperty]
	public sealed class BaseStatEntry
	{
		[HorizontalGroup(Width = .25f, LabelWidth = 15), LabelText(" ")] public float BaseValue = 1f;
		[HorizontalGroup(LabelWidth = 15), LabelText(" ")] public AnimationCurve Growth = AnimationCurve.Linear(0, 1, 10, 10);
		[HorizontalGroup(Width = .25f), HideLabel, NonSerialized, ShowInInspector] private float TestValue = 1f;

		public BaseStatEntry() { }

		public BaseStatEntry(float baseValue, AnimationCurve growth = null)
		{
			BaseValue = baseValue;
			Growth = growth ?? AnimationCurve.Linear(0, 1, 10, 10);
		}

		float GetValue(int grade, StatsSO stats) => BaseValue * Growth.Evaluate(grade * 1f / stats.maxGrade);

		public Func<float> GetFunc(IStats stats) => () =>
		{
			if (stats.LogsFunc())
			{
				Debug.Log($"BaseValue {BaseValue} GetValue {GetValue(stats.currentGrade, stats.config)}");
			}

			return GetValue(stats.currentGrade, stats.config);
		};

		public override string ToString() => $"Base: {BaseValue:F2}, Growth: {Growth.keys.Length} keys";

		public void SetTestGrowValue(int testGrade, StatsSO statsSo)
		{
			TestValue = GetValue(testGrade, statsSo);
		}
	}
}