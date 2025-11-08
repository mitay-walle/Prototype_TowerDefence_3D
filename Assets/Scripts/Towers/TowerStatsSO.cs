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
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry FireRate = new BaseStatEntry(1f, AnimationCurve.Linear(0, 1, 1, 1.5f));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry Range = new BaseStatEntry(5f, AnimationCurve.Linear(0, 1, 1, 2));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry CritChance = new BaseStatEntry(0.1f, AnimationCurve.Linear(0, 1, 1, 1.2f));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry ProjectileSpeed = new BaseStatEntry(0.1f, AnimationCurve.Linear(0, 1, 1, 1.2f));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry RotateSpeed = new BaseStatEntry(180, AnimationCurve.Linear(0, 1, 1, 1.2f));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry UpgradeCost = new BaseStatEntry(20, AnimationCurve.Linear(0, 1, 1, 250));

		public int Cost = 25;

		public BaseStatEntry this[TowerStat type] => type switch
		{
			TowerStat.Damage => Damage,
			TowerStat.FireRate => FireRate,
			TowerStat.Range => Range,
			TowerStat.CritChance => CritChance,
			TowerStat.ProjectileSpeed => ProjectileSpeed,
			TowerStat.RotateSpeed => RotateSpeed,
			TowerStat.UpgradeCost => UpgradeCost,
			_ => throw new ArgumentException($"Unknown stat type: {type}")
		};

		public override IEnumerable<BaseStatEntry> GetStats()
		{
			yield return Damage;
			yield return FireRate;
			yield return Range;
			yield return CritChance;
			yield return ProjectileSpeed;
			yield return RotateSpeed;
			yield return UpgradeCost;
		}
	}

	public enum TowerStat
	{
		Damage,
		FireRate,
		Range,
		CritChance,
		ProjectileSpeed,
		RotateSpeed,
		UpgradeCost,
	}

	public static partial class TowerStatUtility
	{
		public static void Calculate(TowerStats stats)
		{
			stats.Damage.Calculate();
			stats.FireRate.Calculate();
			stats.Range.Calculate();
			stats.CritChance?.Calculate();
			stats.ProjectileSpeed?.Calculate();
			stats.RotateSpeed?.Calculate();
			stats.UpgradeCost?.Calculate();
		}

		public static Stat Indexer(TowerStats stats, TowerStat type) => type switch
		{
			TowerStat.Damage => stats.Damage,
			TowerStat.FireRate => stats.FireRate,
			TowerStat.Range => stats.Range,
			TowerStat.CritChance => stats.CritChance,
			TowerStat.ProjectileSpeed => stats.ProjectileSpeed,
			TowerStat.RotateSpeed => stats.RotateSpeed,
			TowerStat.UpgradeCost => stats.UpgradeCost,
			_ => throw new ArgumentException($"Unknown stat type: {type}")
		};

		public static void Initialize(TowerStats stats)
		{
			foreach (TowerStat type in Enum.GetValues(typeof(TowerStat)))
			{
				switch (type)
				{
					case TowerStat.Damage:
						stats.Damage.Init(stats, stats.statsSO.Damage);
						break;

					case TowerStat.FireRate:
						stats.FireRate.Init(stats, stats.statsSO.FireRate);
						break;

					case TowerStat.Range:
						stats.Range.Init(stats, stats.statsSO.Range);
						break;

					case TowerStat.CritChance:
						stats.CritChance.Init(stats, stats.statsSO.CritChance);
						break;

					case TowerStat.ProjectileSpeed:
						stats.ProjectileSpeed.Init(stats, stats.statsSO.ProjectileSpeed);
						break;

					case TowerStat.RotateSpeed:
						stats.RotateSpeed.Init(stats, stats.statsSO.ProjectileSpeed);
						break;

					case TowerStat.UpgradeCost:
						stats.UpgradeCost.Init(stats, stats.statsSO.UpgradeCost);
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
	}
}