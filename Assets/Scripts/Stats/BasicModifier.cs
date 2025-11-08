using System;
using UnityEngine;

namespace TD.Stats
{
	[Serializable]
	public class BasicModifier : StatModifier
	{
		public enum ModifierType
		{
			Add,
			MultiplySum,
			ResetToBase,
			ResetToSumBeforeMultipliers,
			SetToValue,
			AddPercent,
			Clamp
		}

		public ModifierType type;
		public float value = 2;
		public float minValue;
		public float maxValue = 100;

		public override bool IsMultplicative => type switch
		{
			ModifierType.Add => false,
			ModifierType.MultiplySum => true,
			ModifierType.ResetToBase => true,
			ModifierType.ResetToSumBeforeMultipliers => false,
			ModifierType.SetToValue => true,
			ModifierType.AddPercent => true,
			ModifierType.Clamp => true,
			_ => throw new ArgumentOutOfRangeException()
		};

		public override float Calculate(float currentValue, float baseValue, float sumBeforeMultipliers)
		{
			return type switch
			{
				ModifierType.Add => currentValue + value,
				ModifierType.MultiplySum => currentValue * value,
				ModifierType.ResetToBase => baseValue,
				ModifierType.ResetToSumBeforeMultipliers => sumBeforeMultipliers,
				ModifierType.SetToValue => value,
				ModifierType.AddPercent => currentValue + (baseValue * value),
				ModifierType.Clamp => Mathf.Clamp(currentValue, minValue, maxValue),
				_ => currentValue
			};
		}

		public override string ToString() => $"{type}: {value:F2}";
	}
}