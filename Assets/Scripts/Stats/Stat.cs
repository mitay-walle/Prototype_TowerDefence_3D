using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD.Stats
{
	[Serializable]
	public class Stat
	{
		public struct Info
		{
			public float BeforeValue;
			public float AfterValue;
			public float DeltaValue;

			public override string ToString() => $"Before: {BeforeValue:F2} → After: {AfterValue:F2} (Δ {DeltaValue:F2})";
		}

		private Func<float> baseValueGetter;
		private Func<bool> logsGetter;
		private List<StatModifier> modifiers = new List<StatModifier>();
		private float cachedValue;
		private IStats cachedTowerStats;

		public event Action<Info> OnChanged;

		public float Value => cachedValue;
		public float BaseValue => baseValueGetter();
		public int ModifierCount => modifiers.Count;

		public void Init(IStats stats, BaseStatEntry baseStatEntry)
		{
			cachedTowerStats = stats;
			baseValueGetter = baseStatEntry.GetFunc(stats);
			logsGetter = stats.LogsFunc;
			Calculate();
		}

		public void AddModifier(StatModifier modifier)
		{
			if (!modifiers.Contains(modifier))
			{
				modifiers.Add(modifier);
				modifiers.Sort((a, b) => a.priority.CompareTo(b.priority));
				modifier.OnAdd(cachedTowerStats);
			}
		}

		public void RemoveModifier(StatModifier modifier)
		{
			if (modifiers.Remove(modifier))
			{
				modifier.OnRemove(cachedTowerStats);
			}
		}

		public void ClearModifiers() => modifiers.Clear();

		public void Calculate()
		{
			float oldValue = cachedValue;
			float baseValue = baseValueGetter();
			float currentValue = baseValue;
			float sumBeforeMultipliers = baseValue;

			// Аддитивные модификаторы
			foreach (var mod in modifiers)
			{
				if (!mod.IsMultplicative)
				{
					currentValue = mod.Calculate(currentValue, baseValue, sumBeforeMultipliers);
				}
			}

			sumBeforeMultipliers = currentValue;

			// Остальные модификаторы
			foreach (var mod in modifiers)
			{
				if (mod.IsMultplicative)
				{
					currentValue = mod.Calculate(currentValue, baseValue, sumBeforeMultipliers);
				}
			}

			cachedValue = currentValue;

			var info = new Info
			{
				BeforeValue = oldValue,
				AfterValue = cachedValue,
				DeltaValue = cachedValue - oldValue
			};

			if (logsGetter())
			{
				Debug.Log($"[Stat] {info}");
			}

			OnChanged?.Invoke(info);
		}

		public override string ToString() => $"Value: {Value:F2} (Base: {BaseValue:F2}, Mods: {ModifierCount})";
	}
}