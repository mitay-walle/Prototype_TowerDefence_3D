using TD.Monsters;
using UnityEngine;

namespace TD.Weapons
{
    public class InstantWeapon : MonoBehaviour, IWeapon
    {
        private const string TOOLTIP_MAX_RANGE = "Maximum range of the instant hit weapon";
        private const string TOOLTIP_LAYER_MASK = "Layers that can be hit by this weapon";
        private const string TOOLTIP_SPLASH_RADIUS = "Radius for splash damage (0 = no splash)";
        private const string TOOLTIP_SPLASH_FALLOFF = "Damage falloff for splash (1 = full damage at edges, 0 = no damage at edges)";

        [Tooltip(TOOLTIP_MAX_RANGE)]
        [SerializeField] private float maxRange = 100f;
        [Tooltip(TOOLTIP_LAYER_MASK)]
        [SerializeField] private LayerMask hitMask = ~0;
        [Tooltip(TOOLTIP_SPLASH_RADIUS)]
        [SerializeField] private float splashRadius = 0f;
        [Tooltip(TOOLTIP_SPLASH_FALLOFF)]
        [SerializeField, Range(0f, 1f)] private float splashFalloff = 0.5f;

        [SerializeField] private LineRenderer tracer;
        [SerializeField] private float tracerDuration = 0.1f;
        [SerializeField] private bool Logs = false;

        public WeaponType WeaponType => WeaponType.Instant;

        public void Fire(Vector3 origin, Vector3 direction, Transform target, float damage)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRange, hitMask))
            {
                if (Logs) Debug.Log($"[InstantWeapon] Hit {hit.collider.name} at {hit.point}");

                if (splashRadius > 0f)
                {
                    ApplySplashDamage(hit.point, damage);
                }
                else
                {
                    var health = hit.collider.GetComponent<MonsterHealth>();
                    if (health != null)
                    {
                        health.TakeDamage(damage);
                    }
                }

                if (tracer != null)
                {
                    ShowTracer(origin, hit.point);
                }
            }
            else
            {
                if (Logs) Debug.Log($"[InstantWeapon] Missed - no hit within {maxRange}m");

                if (tracer != null)
                {
                    ShowTracer(origin, origin + direction * maxRange);
                }
            }
        }

        private void ApplySplashDamage(Vector3 center, float damage)
        {
            Collider[] hits = Physics.OverlapSphere(center, splashRadius, hitMask);

            foreach (var hit in hits)
            {
                var health = hit.GetComponent<MonsterHealth>();
                if (health != null)
                {
                    float distance = Vector3.Distance(center, hit.transform.position);
                    float damageMultiplier = 1f - (distance / splashRadius) * (1f - splashFalloff);
                    float finalDamage = damage * Mathf.Clamp01(damageMultiplier);

                    health.TakeDamage(finalDamage);

                    if (Logs) Debug.Log($"[InstantWeapon] Splash hit {hit.name} for {finalDamage:F1} damage (distance: {distance:F2}m)");
                }
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
