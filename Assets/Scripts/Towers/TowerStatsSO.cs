using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TD.Stats;
using UnityEditor;
using UnityEngine;

namespace TD.Towers
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

		[InlineEditor, Required] public TowerBalanceProfileSO BalanceProfile;

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

		#region Statistics
		public float CalculateDPS(int grade)
		{
			var p = BalanceProfile;
			float dmg = Damage.GetValue(grade, this);
			float rate = FireRate.GetValue(grade, this);
			float crit = CritChance.GetValue(grade, this);
			float avgCrit = 1f + (crit * (p.CritDamageMultiplier - 1f));
			return dmg * rate * avgCrit;
		}

		public float CalculateDPSScore(int grade)
		{
			var p = BalanceProfile;
			float dps = CalculateDPS(grade);
			float range = Range.GetValue(grade, this);
			float proj = ProjectileSpeed.GetValue(grade, this);
			float rotate = RotateSpeed.GetValue(grade, this);
			return dps * (1f + p.RangeWeight * range + p.ProjectileSpeedWeight * proj + p.RotateSpeedWeight * rotate);
		}

		public float CalculateEfficiency(int grade)
		{
			int totalCost = Cost;
			for (int i = 0; i <= grade; i++)
				totalCost += Mathf.RoundToInt(UpgradeCost.GetValue(i, this));

			float score = CalculateDPSScore(grade);
			return score / totalCost;
		}

#if UNITY_EDITOR
		[OnInspectorGUI]
		private void OnInspectorGUI()
		{
			if (!BalanceProfile)
			{
				EditorGUILayout.HelpBox("Assign a TowerBalanceProfileSO for global normalization.", MessageType.Warning);
				return;
			}

			int test = TestGrade;

			TowerStatsSimulator.SimulateUpgrades(this, test, out float dps, out float eff, out int totalCost);

			EditorGUILayout.LabelField($"Total Cost: {totalCost}");
			EditorGUILayout.LabelField($"Grade {test} (Simulated)", EditorStyles.boldLabel);
			EditorGUILayout.LabelField($"DPS (with rules): {dps:F2}");
			EditorGUILayout.LabelField($"Efficiency (with rules): {eff:F4}");

			GUILayout.Space(10);
			DrawGlobalGraph();
		}

		private void DrawGlobalGraph()
		{
			var p = BalanceProfile;
			int grades = Mathf.Max(1, p.GlobalMaxGrade);

			// пересчёт глобальных максимумов
			for (int i = 0; i <= grades; i++)
			{
				p.MaxDpsReference = Mathf.Max(p.MaxDpsReference, CalculateDPSScore(i));
				p.MaxEfficiencyReference = Mathf.Max(p.MaxEfficiencyReference, CalculateEfficiency(i));
			}

			const float padding = 8f;
			float graphWidth = EditorGUIUtility.currentViewWidth - 60f;
			float graphHeight = 100f;

			Rect rect = GUILayoutUtility.GetRect(graphWidth, graphHeight);
			EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 1f));

			Handles.color = Color.green;
			Vector3 prev = Vector3.zero;
			for (int i = 0; i <= grades; i++)
			{
				float val = CalculateDPSScore(i);
				float x = rect.x + padding + (float)i / grades * (rect.width - padding * 2);
				float y = rect.yMax - padding - (val / p.MaxDpsReference * (rect.height - padding * 2));
				Vector3 curr = new(x, y);
				if (i > 0) Handles.DrawLine(prev, curr);
				prev = curr;
			}

			Handles.color = Color.cyan;
			prev = Vector3.zero;
			for (int i = 0; i <= grades; i++)
			{
				float val = CalculateEfficiency(i);
				float x = rect.x + padding + (float)i / grades * (rect.width - padding * 2);
				float y = rect.yMax - padding - (val / p.MaxEfficiencyReference * (rect.height - padding * 2));
				Vector3 curr = new(x, y);
				if (i > 0) Handles.DrawLine(prev, curr);
				prev = curr;
			}

			EditorGUI.LabelField(new Rect(rect.x + 6, rect.y + 4, 200, 18), "Green: DPS | Cyan: Efficiency (normalized)", EditorStyles.miniBoldLabel);
		}
#endif
	  #endregion
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