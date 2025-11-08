using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TD.Stats;
using UnityEngine;

namespace TD.Monsters
{
	[CreateAssetMenu(fileName = "MonsterStats", menuName = "TD/MonsterStats", order = 1)]
	public sealed class MonsterStatsSO : StatsSO
	{
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry Damage = new BaseStatEntry(1, AnimationCurve.Linear(0, 1, 1, 2));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry MoveSpeed = new BaseStatEntry(3, AnimationCurve.Linear(0, 1, 1, 2));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry Health = new BaseStatEntry(25, AnimationCurve.Linear(0, 1, 1, 2));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry InstantReward = new BaseStatEntry(50, AnimationCurve.Linear(0, 1, 1, 2));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry IncomeReward = new BaseStatEntry(10, AnimationCurve.Linear(0, 1, 1, 2));
		[OnValueChanged("OnStatsChanged", true)] public BaseStatEntry EarlyKillModifier = new BaseStatEntry(1.2f, AnimationCurve.Linear(0, 1, 1, 2));

		public override IEnumerable<BaseStatEntry> GetStats()
		{
			yield return Damage;
			yield return MoveSpeed;
			yield return Health;
			yield return InstantReward;
			yield return IncomeReward;
			yield return EarlyKillModifier;
		}
	}

	public enum MonsterStat
	{
		Damage,
		MoveSpeed,
		Health,
		InstantReward,
		IncomeReward,
		EarlyKillModifier,
	}

	public static class MonsterStatUtility
	{
		public static void Calculate(MonsterStats stats)
		{
			stats.Damage.Calculate();
			stats.MoveSpeed.Calculate();
			stats.Health.Calculate();
			stats.InstantReward.Calculate();
			stats.IncomeReward.Calculate();
			stats.EarlyKillModifier.Calculate();
		}

		public static Stat Indexer(MonsterStats stats, MonsterStat type) => type switch
		{
			MonsterStat.Damage => stats.Damage,
			MonsterStat.MoveSpeed => stats.MoveSpeed,
			MonsterStat.Health => stats.Health,
			MonsterStat.InstantReward => stats.InstantReward,
			MonsterStat.IncomeReward => stats.IncomeReward,
			MonsterStat.EarlyKillModifier => stats.EarlyKillModifier,
			_ => throw new ArgumentException($"Unknown stat type: {type}")
		};

		public static void Initialize(MonsterStats stats)
		{
			foreach (MonsterStat type in Enum.GetValues(typeof(MonsterStat)))
			{
				switch (type)
				{
					case MonsterStat.Damage:
						stats.Damage.Init(stats, stats.statsSO.Damage);
						break;

					case MonsterStat.MoveSpeed:
						stats.MoveSpeed.Init(stats, stats.statsSO.MoveSpeed);
						break;

					case MonsterStat.Health:
						stats.Health.Init(stats, stats.statsSO.Health);
						break;

					case MonsterStat.InstantReward:
						stats.InstantReward.Init(stats, stats.statsSO.InstantReward);
						break;

					case MonsterStat.IncomeReward:
						stats.IncomeReward.Init(stats, stats.statsSO.IncomeReward);
						break;

					case MonsterStat.EarlyKillModifier:
						stats.EarlyKillModifier.Init(stats, stats.statsSO.EarlyKillModifier);
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
	}
}