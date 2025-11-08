using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TD.Stats
{
	[CreateAssetMenu(fileName = "New Tower Stats", menuName = "Tower Defence/Tower Stats")]
	public sealed class TowerStatsSO : StatsSO
	{
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry damage = new BaseStatEntry(10f, AnimationCurve.Linear(0, 1, 1, 2));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry fireRate = new BaseStatEntry(1f, AnimationCurve.Linear(0, 1, 1, 1.5f));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry range = new BaseStatEntry(5f, AnimationCurve.Linear(0, 1, 1, 2));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry critChance = new BaseStatEntry(0.1f, AnimationCurve.Linear(0, 1, 1, 1.2f));

		public BaseStatEntry this[StatType type] => type switch
		{
			StatType.Damage => damage,
			StatType.FireRate => fireRate,
			StatType.Range => range,
			StatType.CritChance => critChance,
			_ => throw new ArgumentException($"Unknown stat type: {type}")
		};

		public override IEnumerable<BaseStatEntry> GetStats()
		{
			yield return damage;
			yield return fireRate;
			yield return range;
			yield return critChance;
		}
	}
}