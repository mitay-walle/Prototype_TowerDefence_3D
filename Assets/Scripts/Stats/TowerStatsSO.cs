using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD.Stats
{
	[CreateAssetMenu(fileName = "New Tower Stats", menuName = "Tower Defence/Tower Stats")]
	public sealed class TowerStatsSO : StatsSO
	{
		public BaseStatEntry damage = new BaseStatEntry(10f, AnimationCurve.Linear(0, 1, 1, 2));
		public BaseStatEntry fireRate = new BaseStatEntry(1f, AnimationCurve.Linear(0, 1, 1, 1.5f));
		public BaseStatEntry range = new BaseStatEntry(5f, AnimationCurve.Linear(0, 1, 1, 2));
		public BaseStatEntry critChance = new BaseStatEntry(0.1f, AnimationCurve.Linear(0, 1, 1, 1.2f));

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