using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TD.Stats
{
	public class StatsTower : MonoBehaviour, IStats
	{
		[OnValueChanged("OnGradeChanged")]
		public TowerStatsSO statsSO;

		[OnValueChanged("OnGradeChanged"), Range(0, 10)]
		[field: SerializeField] public int currentGrade { get; private set; }
		public StatsSO config => statsSO;
		public Func<bool> LogsFunc { get; private set; }

		public bool logs = false;

		public Stat damage = new Stat();
		public Stat fireRate = new Stat();
		public Stat range = new Stat();
		public Stat critChance = new Stat();

		public Stat this[StatType type] => type switch
		{
			StatType.Damage => damage,
			StatType.FireRate => fireRate,
			StatType.Range => range,
			StatType.CritChance => critChance,
			_ => throw new ArgumentException($"Unknown stat type: {type}")
		};

		public Action<int> OnGradeUpgraded { get; set; }

		private void Awake()
		{
			InitializeStats();

			if (statsSO != null)
			{
				statsSO.OnStatsChangedEvent += RecalculateStats;
			}
		}

		private void OnDestroy()
		{
			if (statsSO != null)
			{
				statsSO.OnStatsChangedEvent -= RecalculateStats;
			}
		}

		private void InitializeStats()
		{
			if (statsSO == null) return;

			LogsFunc = () => logs;
			foreach (StatType type in Enum.GetValues(typeof(StatType)))
			{
				switch (type)
				{
					case StatType.Damage:
						damage.Init(this, statsSO[StatType.Damage]);
						break;

					case StatType.FireRate:
						fireRate.Init(this, statsSO[StatType.FireRate]);
						break;

					case StatType.Range:
						range.Init(this, statsSO[StatType.Range]);
						break;

					case StatType.CritChance:
						critChance.Init(this, statsSO[StatType.CritChance]);
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		private void OnGradeChanged()
		{
			if (statsSO != null)
			{
				currentGrade = Mathf.Clamp(currentGrade, 0, statsSO.maxGrade);
				RecalculateStats();
			}
		}

		[Button]
		private void RecalculateStats()
		{
			damage?.Calculate();
			fireRate?.Calculate();
			range?.Calculate();
			critChance?.Calculate();
		}

		[Button]
		public void UpgradeGrade()
		{
			if (currentGrade < statsSO.maxGrade)
			{
				currentGrade++;
				statsSO.ApplyUpgrade(currentGrade, this);
				RecalculateStats();
				OnGradeUpgraded?.Invoke(currentGrade);
			}
		}

		public void SetConfig(StatsSO newStats)
		{
			if (newStats == null || newStats is TowerStatsSO)
			{
				statsSO = newStats as TowerStatsSO;
				RecalculateStats();
			}
			else
			{
				Debug.LogError("wrong config type", newStats);
			}
		}
	}
}