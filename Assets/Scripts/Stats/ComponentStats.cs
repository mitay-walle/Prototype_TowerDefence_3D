using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TD.Stats
{
	public abstract class ComponentStats<TSO> : MonoBehaviour, IStats where TSO : StatsSO
	{
		[OnValueChanged("OnGradeChanged")]
		public TSO statsSO;

		[OnValueChanged("OnGradeChanged"), Range(0, 10)]
		[field: SerializeField] public int currentGrade { get; private set; }
		public StatsSO config => statsSO;
		public Func<bool> LogsFunc { get; private set; }

		public bool logs = false;

		public Action<int> OnGradeUpgraded { get; set; }
		public bool CanUpgrade => currentGrade < config?.maxGrade;
		public Action OnRecalculateStatsFinished { get; set; }

		protected abstract void InitializeStats();

		[Button] protected void RecalculateStats()
		{
			OnRecalculateStats();
			OnRecalculateStatsFinished?.Invoke();
		}

		protected abstract void OnRecalculateStats();

		private void Awake()
		{
			LogsFunc = () => logs;
			InitializeStats();

			if (config != null)
			{
				config.OnStatsChangedEvent -= RecalculateStats;
				config.OnStatsChangedEvent += RecalculateStats;
			}
		}

		private void OnDestroy()
		{
			if (config != null)
			{
				config.OnStatsChangedEvent -= RecalculateStats;
			}
		}

		private void OnGradeChanged()
		{
			if (config != null)
			{
				currentGrade = Mathf.Clamp(currentGrade, 0, config.maxGrade);
				RecalculateStats();
			}
		}

		[Button, EnableIf("CanUpgrade")]
		public void UpgradeGrade()
		{
			if (currentGrade < config.maxGrade)
			{
				currentGrade++;
				config.ApplyUpgradeRulesOnly(currentGrade, this);
				RecalculateStats();
				OnGradeUpgraded?.Invoke(currentGrade);
			}
		}

		public void SetConfig(StatsSO newStats)
		{
			if (newStats == null || newStats is TSO)
			{
				if (statsSO)
				{
					statsSO.OnStatsChangedEvent -= RecalculateStats;
				}

				statsSO = newStats as TSO;
				RecalculateStats();
			}
			else
			{
				Debug.LogError("wrong config type", newStats);
			}
		}
	}
}