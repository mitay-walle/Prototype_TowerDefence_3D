using UnityEngine;

namespace TD.Weapons
{
    public class AoEWeapon : MonoBehaviour, IWeapon
    {
        private const string TOOLTIP_DAMAGE_RADIUS = "Radius of area-of-effect damage around the tower";
        private const string TOOLTIP_DAMAGE_INTERVAL = "Time between damage ticks";
        private const string TOOLTIP_TARGETS_PER_TICK = "Maximum number of targets to damage per tick (0 = all)";

        [Tooltip(TOOLTIP_DAMAGE_RADIUS)]
        [SerializeField] private float damageRadius = 5f;
        [Tooltip(TOOLTIP_DAMAGE_INTERVAL)]
        [SerializeField] private float damageInterval = 0.5f;
        [Tooltip(TOOLTIP_TARGETS_PER_TICK)]
        [SerializeField] private int maxTargetsPerTick = 0;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private bool Logs = false;

        private float damagePerTick;
        private float nextDamageTime;
        private bool isActive;

        public WeaponType WeaponType => WeaponType.AoE;

        private void Update()
        {
            if (!isActive) return;

            if (Time.time >= nextDamageTime)
            {
                ApplyAreaDamage();
                nextDamageTime = Time.time + damageInterval;
            }
        }

        public void Fire(Vector3 origin, Vector3 direction, Transform target, float damage)
        {
            damagePerTick = damage;
            isActive = true;

            if (Time.time >= nextDamageTime)
            {
                ApplyAreaDamage();
                nextDamageTime = Time.time + damageInterval;
            }
        }

        public void StopFiring()
        {
            isActive = false;
        }

        private void ApplyAreaDamage()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius, targetMask);

            int hitCount = 0;

            foreach (var hit in hits)
            {
                if (maxTargetsPerTick > 0 && hitCount >= maxTargetsPerTick) break;

                var health = hit.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    health.TakeDamage(damagePerTick);
                    hitCount++;

                    if (Logs) Debug.Log($"[AoEWeapon] Damaged {hit.name} for {damagePerTick:F1} (target {hitCount})");
                }
            }

            if (Logs && hitCount > 0) Debug.Log($"[AoEWeapon] Area damage tick: {hitCount} enemies hit for {damagePerTick:F1} each");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, damageRadius);
        }
    }
}
