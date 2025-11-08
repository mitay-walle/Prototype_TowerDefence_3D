using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TD.Stats
{
	[CreateAssetMenu(fileName = "New Tower Stats", menuName = "Tower Defence/Tower Stats")]
	public sealed class TowerStatsSO : StatsSO
	{
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry Damage = new BaseStatEntry(10f, AnimationCurve.Linear(0, 1, 1, 2));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry FireDelay = new BaseStatEntry(1f, AnimationCurve.Linear(0, 1, 1, 1.5f));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry Range = new BaseStatEntry(5f, AnimationCurve.Linear(0, 1, 1, 2));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry CritChance = new BaseStatEntry(0.1f, AnimationCurve.Linear(0, 1, 1, 1.2f));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry ProjectileSpeed = new BaseStatEntry(0.1f, AnimationCurve.Linear(0, 1, 1, 1.2f));

		public BaseStatEntry this[TowerStat type] => type switch
		{
			TowerStat.Damage => Damage,
			TowerStat.FireRate => FireDelay,
			TowerStat.Range => Range,
			TowerStat.CritChance => CritChance,
			TowerStat.ProjectileSpeed => ProjectileSpeed,
			_ => throw new ArgumentException($"Unknown stat type: {type}")
		};

		public override IEnumerable<BaseStatEntry> GetStats()
		{
			yield return Damage;
			yield return FireDelay;
			yield return Range;
			yield return CritChance;
			yield return ProjectileSpeed;
		}
	}

	public enum TowerStat
	{
		Damage,
		FireRate,
		Range,
		CritChance,
		ProjectileSpeed,
	}

	public static class StatUtility
	{
		public static void Calculate(TowerStats stats)
		{
			stats.Damage.Calculate();
			stats.FireDelay.Calculate();
			stats.Range.Calculate();
			stats.CritChance?.Calculate();
			stats.ProjectileSpeed?.Calculate();
		}

		public static Stat Indexer(TowerStats stats, TowerStat type) => type switch
		{
			TowerStat.Damage => stats.Damage,
			TowerStat.FireRate => stats.FireDelay,
			TowerStat.Range => stats.Range,
			TowerStat.CritChance => stats.CritChance,
			TowerStat.ProjectileSpeed => stats.ProjectileSpeed,
			_ => throw new ArgumentException($"Unknown stat type: {type}")
		};

		public static void Initialize(TowerStats stats)
		{
			foreach (TowerStat type in Enum.GetValues(typeof(TowerStat)))
			{
				switch (type)
				{
					case TowerStat.Damage:
						stats.Damage.Init(stats, stats.statsSO[TowerStat.Damage]);
						break;

					case TowerStat.FireRate:
						stats.FireDelay.Init(stats, stats.statsSO[TowerStat.FireRate]);
						break;

					case TowerStat.Range:
						stats.Range.Init(stats, stats.statsSO[TowerStat.Range]);
						break;

					case TowerStat.CritChance:
						stats.CritChance.Init(stats, stats.statsSO[TowerStat.CritChance]);
						break;

					case TowerStat.ProjectileSpeed:
						stats.ProjectileSpeed.Init(stats, stats.statsSO[TowerStat.ProjectileSpeed]);
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
	}
}