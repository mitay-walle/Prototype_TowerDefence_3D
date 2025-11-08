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
		public bool CanUpgrade => currentGrade == config.maxGrade;

		public int UpgradeCost
		{
			get => throw new NotImplementedException();
		}

		protected abstract void InitializeStats();
		[Button] protected abstract void RecalculateStats();

		private void Awake()
		{
			LogsFunc = () => logs;
			InitializeStats();

			if (config != null)
			{
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

		[Button]
		public void UpgradeGrade()
		{
			if (currentGrade < config.maxGrade)
			{
				currentGrade++;
				config.ApplyUpgrade(currentGrade, this);
				RecalculateStats();
				OnGradeUpgraded?.Invoke(currentGrade);
			}
		}

		public void SetConfig(StatsSO newStats)
		{
			if (newStats == null || newStats is TSO)
			{
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