using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TD.Stats
{
	[Serializable, InlineProperty, HideReferenceObjectPicker]
	public sealed class BaseStatEntry
	{
		[HorizontalGroup(Width = .25f, LabelWidth = 15), HideLabel] public float BaseValue = 1f;
		[HorizontalGroup(LabelWidth = 15), HideReferenceObjectPicker, HideLabel] public AnimationCurve Growth = AnimationCurve.Linear(0, 1, 10, 10);
		[HorizontalGroup(Width = .25f), HideLabel, NonSerialized, ShowInInspector, ReadOnly] private float TestValue = 1f;
		[HorizontalGroup(Width = .15f), SerializeField, HideInInlineEditors, LabelText("R")] private bool RoundToInt;

		public BaseStatEntry() { }

		public BaseStatEntry(float maxValue)
		{
			Growth = AnimationCurve.Linear(0, 1, 1, maxValue);
		}

		public BaseStatEntry(float baseValue, float maxValue)
		{
			BaseValue = baseValue;
			Growth = AnimationCurve.Linear(0, 1, 1, maxValue);
		}

		public BaseStatEntry(float baseValue, AnimationCurve growth = null)
		{
			BaseValue = baseValue;
			Growth = growth ?? AnimationCurve.Linear(0, 1, 10, 10);
		}

		public float GetValue(int grade, StatsSO stats) => BaseValue * Growth.Evaluate(grade * 1f / stats.maxGrade);
		public int GetValueInt(int grade, StatsSO stats) => Mathf.RoundToInt(GetValue(grade, stats));

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
			if (RoundToInt)
			{
				TestValue = GetValueInt(testGrade, statsSo);
			}
			else
			{
				TestValue = RoundTo(GetValue(testGrade, statsSo), 2);
			}
		}

		public static float RoundTo(float value, int digits)
		{
			return (float)Math.Round(value, digits, MidpointRounding.AwayFromZero);
		}

		public BaseStatEntry Clone()
		{
			return MemberwiseClone() as BaseStatEntry;
		}
	}
}