using TD.Monsters;
using UnityEngine;

namespace TD.Weapons
{
    public class PierceWeapon : MonoBehaviour, IWeapon
    {
        private const string TOOLTIP_MAX_RANGE = "Maximum range of the pierce weapon";
        private const string TOOLTIP_MAX_PIERCE = "Maximum number of enemies that can be pierced";
        private const string TOOLTIP_DAMAGE_FALLOFF = "Damage reduction per enemy hit (0.1 = 10% less each hit)";

        [Tooltip(TOOLTIP_MAX_RANGE)]
        [SerializeField] private float maxRange = 100f;
        [Tooltip(TOOLTIP_MAX_PIERCE)]
        [SerializeField] private int maxPierceCount = 5;
        [Tooltip(TOOLTIP_DAMAGE_FALLOFF)]
        [SerializeField, Range(0f, 1f)] private float damageFalloffPerHit = 0.1f;
        [SerializeField] private LayerMask hitMask = ~0;

        [SerializeField] private LineRenderer tracer;
        [SerializeField] private float tracerDuration = 0.15f;
        [SerializeField] private bool Logs = false;

        public WeaponType WeaponType => WeaponType.Pierce;

        public void Fire(Vector3 origin, Vector3 direction, Transform target, float damage)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxRange, hitMask);

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            int hitCount = 0;
            float currentDamage = damage;
            Vector3 lastHitPoint = origin + direction * maxRange;

            foreach (var hit in hits)
            {
                if (hitCount >= maxPierceCount) break;

                var health = hit.collider.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    health.TakeDamage(currentDamage);
                    hitCount++;

                    if (Logs) Debug.Log($"[PierceWeapon] Hit {hit.collider.name} for {currentDamage:F1} damage (pierce #{hitCount})");

                    currentDamage *= (1f - damageFalloffPerHit);
                    lastHitPoint = hit.point;
                }
            }

            if (Logs) Debug.Log($"[PierceWeapon] Pierced {hitCount} enemies, damage went from {damage:F1} to {currentDamage:F1}");

            if (tracer != null)
            {
                ShowTracer(origin, lastHitPoint);
            }
        }

        private void ShowTracer(Vector3 start, Vector3 end)
        {
            tracer.SetPosition(0, start);
            tracer.SetPosition(1, end);
            tracer.enabled = true;
            Invoke(nameof(HideTracer), tracerDuration);
        }

        private void HideTracer()
        {
            if (tracer != null)
            {
                tracer.enabled = false;
            }
        }
    }
}
