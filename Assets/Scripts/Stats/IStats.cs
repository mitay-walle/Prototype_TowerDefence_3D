using System;

namespace TD.Stats
{
	public interface IStats
	{
		public Action<int> OnGradeUpgraded { get; set; }
		public Action OnRecalculateStatsFinished { get; set; }
		int currentGrade { get; }
		public StatsSO config { get; }
		Func<bool> LogsFunc { get; }
		void SetConfig(StatsSO newStats);
	}
}