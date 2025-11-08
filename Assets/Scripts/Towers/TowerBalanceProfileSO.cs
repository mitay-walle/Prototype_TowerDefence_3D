using UnityEngine;

namespace TD.Towers
{
	[CreateAssetMenu(fileName = "Tower Balance Profile", menuName = "Tower Defence/Balance Profile")]
	public class TowerBalanceProfileSO : ScriptableObject
	{
		[Header("Weights")]
		public float CritDamageMultiplier = 1.5f;
		public float RangeWeight = 0.2f;
		public float RotateSpeedWeight = 0.1f;
		public float ProjectileSpeedWeight = 0.05f;

		[Header("Global Limits")]
		[Tooltip("Глобальный диапазон уровней для анализа всех башен")]
		public int GlobalMaxGrade = 10;

		[Tooltip("Глобальная нормализация графика DPS/Efficiency, обновляется автоматически")]
		public float MaxDpsReference = 0f;
		public float MaxEfficiencyReference = 0f;

#if UNITY_EDITOR
		// служебный метод для сброса статистики перед обновлением
		[ContextMenu("Reset Normalization")]
		public void ResetNormalization()
		{
			MaxDpsReference = 0f;
			MaxEfficiencyReference = 0f;
		}
#endif
	}
}