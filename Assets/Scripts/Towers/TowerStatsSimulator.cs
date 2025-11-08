using System;
using System.Collections.Generic;
using System.Linq;
using TD.Stats;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TD.Towers
{
	/// <summary>
	/// Служебный класс для расчёта всех производных значений башни (DPS, Efficiency)
	/// с учётом всех UpgradeRule и модификаторов.
	/// </summary>
	public static class TowerStatsSimulator
	{
		private static TowerStatsRuntime simStats;

		public static void SimulateUpgrades(TowerStatsSO baseStats, int grade, out float totalDPS, out float totalEfficiency, out int totalCost)
		{
			if (baseStats == null)
			{
				totalDPS = totalEfficiency = totalCost = 0;
				return;
			}

			// ✅ Кэшируем, но пересоздаём при смене башни
			if (simStats == null || simStats.Original != baseStats)
				simStats = new TowerStatsRuntime(baseStats);
			else
				simStats.ResetToBase(); // сброс значений и grade

			// 2️⃣ Применяем все апгрейды до текущего grade
			for (int g = 1; g <= grade; g++)
			{
				foreach (var rule in baseStats.upgradeRules)
					rule.TryApply(g, simStats);

				simStats.SimulatedGrade = g;
			}

			// 3️⃣ Расчёт финальных значений
			var p = baseStats.BalanceProfile;
			float dmg = simStats.GetStatValue(TowerStat.Damage, grade);
			float rate = simStats.GetStatValue(TowerStat.FireRate, grade);
			float crit = simStats.GetStatValue(TowerStat.CritChance, grade);
			float range = simStats.GetStatValue(TowerStat.Range, grade);
			float proj = simStats.GetStatValue(TowerStat.ProjectileSpeed, grade);
			float rot = simStats.GetStatValue(TowerStat.RotateSpeed, grade);

			float avgCrit = 1f + (crit * (p.CritDamageMultiplier - 1f));
			float dps = dmg * rate * avgCrit;
			float combatScore = dps * (1f + p.RangeWeight * range + p.ProjectileSpeedWeight * proj + p.RotateSpeedWeight * rot);

			// 4️⃣ Стоимость (учёт бесплатных апгрейдов)
			totalCost = baseStats.Cost;
			int skipedCount = 0;
			for (int g = 1; g <= grade; g++)
			{
				if (baseStats.upgradeRules.FirstOrDefault(r => r is UpgradeRuleAdditionalGrade graded && graded.CanApplyToGrade(g)) is
				    UpgradeRuleAdditionalGrade found)
				{
					skipedCount = found.additionalGrade;
				}

				if (skipedCount > 0)
				{
					//Debug.Log($"skip cost {Mathf.RoundToInt(simStats.GetStatValue(TowerStat.UpgradeCost, g))} for free grade {g}");
					skipedCount--;
					continue;
				}

				totalCost += Mathf.RoundToInt(simStats.GetStatValue(TowerStat.UpgradeCost, g));
			}

			totalDPS = combatScore;
			totalEfficiency = combatScore / Mathf.Max(1, totalCost);
		}
	}

	/// <summary>
	/// Простая копия TowerStatsSO для временных вычислений без изменения оригинала.
	/// </summary>
	public sealed class TowerStatsRuntime : IStats
	{
		public TowerStatsSO Original { get; private set; }
		public TowerStatsSO Copy { get; private set; }

		public int SimulatedGrade { get; set; }

		public Action<int> OnGradeUpgraded { get; set; }
		public Action OnRecalculateStatsFinished { get; set; }

		public int currentGrade { get; }
		public StatsSO config => Original;

		public Func<bool> LogsFunc { get; } = () => false;

		private readonly Dictionary<Enum, Stat> _stats = new();
		private int _evalGrade;

		public TowerStatsRuntime(TowerStatsSO source)
		{
			SetConfig(Copy = Object.Instantiate(source));
		}

		public void SetConfig(StatsSO newStats)
		{
			if (newStats is not TowerStatsSO towerStats) return;

			Original = towerStats;
			_stats.Clear();
			Copy.maxGrade = Original.maxGrade;
			Copy.Cost = Original.Cost;
			
			foreach (TowerStat st in Enum.GetValues(typeof(TowerStat)))
			{
				var runtimeStat = new Stat();
				runtimeStat.Init(this, towerStats[st]);
				_stats[st] = runtimeStat;
			}
		}

		/// <summary>
		/// Возвращает текущее значение статуса с учётом grade.
		/// </summary>
		public float GetStatValue(Enum stat, int grade)
		{
			_evalGrade = grade;
			if (_stats.TryGetValue(stat, out var s))
				return s.Value;

			return 0f;
		}

		/// <summary>
		/// Применяет модификатор к нужному стату, если он есть.
		/// </summary>
		public void ApplyModifier(Enum stat, StatModifier modifier)
		{
			if (_stats.TryGetValue(stat, out var s))
				s.AddModifier(modifier);
		}

		/// <summary>
		/// Сбрасывает значения всех Stat к базовым (чистым) без модификаторов.
		/// </summary>
		public void ResetToBase()
		{
			foreach (var s in _stats.Values)
				s.ClearModifiers();

			SimulatedGrade = 0;
			_evalGrade = 0;
		}

		public IReadOnlyDictionary<Enum, Stat> GetAllStats() => _stats;
	}
}