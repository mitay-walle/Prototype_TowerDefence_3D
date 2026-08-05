using TD.Monsters;
using TD.Plugins.Timing;
using TD.Stats;
using TD.Towers;
using UnityEngine;

namespace TD.Weapons
{
	public class AoEWeapon : MonoBehaviour, IWeapon
	{
		[SerializeField] private int maxTargetsPerTick = 0;
		[SerializeField] private LayerMask targetMask = ~0;
		[SerializeField] private bool Logs = false;

		private float damagePerTick;
		private Timer nextDamageTime;
		private bool isActive;
		Collider[] results = new Collider[100];
		private TowerStats towerStats;
		private Tower tower;

		public WeaponType WeaponType => WeaponType.AoE;

		private void Awake()
		{
			TryGetComponent(out towerStats);
			TryGetComponent(out tower);
		}

		private void Update()
		{
			if (!isActive || towerStats == null || tower == null) return;

			float damageInterval = 1f / towerStats.FireRate;
			if (nextDamageTime.CheckAndRestart(damageInterval))
			{
				ApplyAreaDamage();
			}
		}

		public void Fire(Vector3 origin, Vector3 direction, Transform target, float damage)
		{
			damagePerTick = damage;
			isActive = true;
			ApplyAreaDamage();
		}

		public void StopFiring()
		{
			isActive = false;
		}

		private void ApplyAreaDamage()
		{
			if (towerStats == null || tower == null) return;

			var areaRange = tower.EffectiveRange;
			int size = Physics.OverlapSphereNonAlloc(transform.position, areaRange, results, targetMask);

			int hitCount = 0;

			for (var i = 0; i < size; i++)
			{
				Collider hit = results[i];
				if (maxTargetsPerTick > 0 && hitCount >= maxTargetsPerTick) break;

				var health = hit.GetComponentInParent<MonsterHealth>();
				if (health != null)
				{
					health.TakeDamage(damagePerTick);
					hitCount++;

					if (Logs) Debug.Log($"[AoEWeapon] Damaged {hit.name} for {damagePerTick:F1} (target {hitCount})");
				}
			}

			if (Logs && (size > 0 || hitCount > 0))
				Debug.Log($"[AoEWeapon] Area damage tick: overlaps={size};resolvedTargets={hitCount};damage={damagePerTick:F1};range={areaRange:F1}");
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;
			float radius = 1;
			if (!Application.IsPlaying(this))
			{
				radius = GetComponent<TowerStats>().statsSO.Range.BaseValue;
			}
			else
			{
				radius = GetComponent<TowerStats>().Range;
			}

			Gizmos.DrawWireSphere(transform.position, radius);
		}
	}
}
