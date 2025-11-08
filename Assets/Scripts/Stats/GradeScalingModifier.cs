using System;

namespace TD.Stats
{
	[Serializable]
	public class GradeScalingModifier : BasicModifier
	{
		public float divisor = 1f;

		private IStats cachedTowerStats;
		private float CurrentValue => cachedTowerStats != null ? cachedTowerStats.currentGrade / divisor : 0f;

		public override float Calculate(float currentValue, float baseValue, float sumBeforeMultipliers)
		{
			value = CurrentValue;
			return base.Calculate(currentValue, baseValue, sumBeforeMultipliers);
		}

		public override void OnAdd(IStats towerStats)
		{
			cachedTowerStats = towerStats;
			towerStats.OnGradeUpgraded += OnGradeChanged;
		}

		public override void OnRemove(IStats towerStats)
		{
			if (cachedTowerStats != null)
			{
				cachedTowerStats.OnGradeUpgraded -= OnGradeChanged;
				cachedTowerStats = null;
			}
		}

		private void OnGradeChanged(int newGrade)
		{
			// Stat will recalculate automatically
		}

		public override string ToString() => $"Grade/{divisor:F1} ({type})";
	}
}