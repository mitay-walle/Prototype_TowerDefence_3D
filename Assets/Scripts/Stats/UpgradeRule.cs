using System;
using Sirenix.OdinInspector;
using TD.Towers;
using UnityEngine;

namespace TD.Stats
{
	[Serializable]
	public abstract class UpgradeRule
	{
		public int startGrade = 2; // с какого уровня применять
		public int repeatEvery = 0; // 0 = один раз, >0 = повторяемый
		public int repeatMax = 0; // 0 = один раз, >0 = повторяемый
		protected abstract void Apply(IStats stats);

		public bool CanApplyToGrade(int grade)
		{
			if (grade < startGrade) return false;

			if (repeatEvery > 0)
			{
				if ((grade - startGrade) % repeatEvery == 0)
				{
					return true;
				}
			}
			else if (grade == startGrade)
			{
				return true;
			}
			return false;
		}

		public void TryApply(int grade, IStats stats)
		{
			if (grade < startGrade) return;

			if (repeatEvery > 0)
			{
				if ((grade - startGrade) % repeatEvery == 0)
				{
					Apply(stats);
				}
			}
			else if (grade == startGrade)
			{
				Apply(stats);
			}
		}
	}

	[Serializable]
	public class UpgradeRuleStatModifier : UpgradeRule
	{
		public TowerStat targetStat;

		public override string ToString() => $"TargetStat: {targetStat}, Modifier: {modifier}";

		[SerializeReference] public StatModifier modifier; // любой наследник Modifier

		protected override void Apply(IStats stats)
		{
			if (stats == null)
			{
				Debug.LogWarning(this);
				return;
			}

			if (stats is TowerStats statsTower)
			{
				statsTower[targetStat].AddModifier(modifier);
			}
		}
	}

	[Serializable]
	public class UpgradeRuleChangeStats : UpgradeRule
	{
		public StatsSO newStats;

		protected override void Apply(IStats stats)
		{
			if (newStats == null)
			{
				Debug.LogError("newStats is null");
				return;
			}

			if (stats == null)
			{
				Debug.LogWarning($"{GetType().Name} {newStats.name}");
				return;
			}

			stats.SetConfig(newStats);
		}
	}

	[Serializable]
	public class UpgradeRuleAdditionalGrade : UpgradeRule
	{
		[MinValue(1)] public int additionalGrade = 1;

		protected override void Apply(IStats stats)
		{
			if (stats == null)
			{
				Debug.LogWarning(GetType().Name);
				return;
			}

			if (stats is TowerStats statsTower)
			{
				for (int i = 0; i < additionalGrade; i++)
				{
					statsTower.GetComponent<Tower>().UpgradeFree();
				}
			}
		}
	}
}