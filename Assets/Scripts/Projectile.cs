using UnityEngine;

namespace TD
{
    public enum ProjectileMode
    {
        Instant,
        SphereCastWhileFlying,
        CheckOnImpact
    }

    public class Projectile : MonoBehaviour
    {
        private const string TOOLTIP_MODE = "Instant: no travel time | SphereCast: checks collision while flying | CheckOnImpact: checks only at destination";
        private const string TOOLTIP_SPHERE_RADIUS = "Radius for collision detection (SphereCast or impact check)";
        private const string TOOLTIP_AREA_DAMAGE = "Enable splash damage to multiple enemies";
        private const string TOOLTIP_AREA_RADIUS = "Radius of area damage effect";
        private const string TOOLTIP_AREA_PERCENT = "Damage falloff at edge of area (0.5 = 50% damage at max range)";
        private const string TOOLTIP_MAX_LIFETIME = "Projectile is destroyed after this many seconds";

        [SerializeField] private bool Logs = false;
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private MeshRenderer meshRenderer;

        [Tooltip(TOOLTIP_MODE)]
        [SerializeField] private ProjectileMode mode = ProjectileMode.SphereCastWhileFlying;
        [Tooltip(TOOLTIP_SPHERE_RADIUS)]
        [SerializeField] private float sphereRadius = 0.3f;
        [Tooltip(TOOLTIP_AREA_DAMAGE)]
        [SerializeField] private bool hasAreaDamage = false;
        [Tooltip(TOOLTIP_AREA_RADIUS)]
        [SerializeField] private float areaDamageRadius = 2f;
        [Tooltip(TOOLTIP_AREA_PERCENT)]
        [SerializeField] private float areaDamagePercent = 0.5f;

        [Tooltip(TOOLTIP_MAX_LIFETIME)]
        [SerializeField] private float maxLifetime = 5f;

        private Vector3 startPosition;
        private Vector3 targetPosition;
        private EnemyHealth targetEnemy;
        private float damage;
        private float speed;
        private float lifetime;
        private bool isLaunched = false;
        private bool hasHit = false;

        public void Launch(Vector3 target, float projectileDamage, float projectileSpeed, EnemyHealth enemy = null)
        {
            startPosition = transform.position;
            targetPosition = target;
            targetEnemy = enemy;
            damage = projectileDamage;
            speed = projectileSpeed;
            lifetime = 0;
            isLaunched = true;
            hasHit = false;

            if (trail != null)
            {
                trail.Clear();
            }

            Vector3 direction = (targetPosition - transform.position).normalized;
            transform.forward = direction;

            if (mode == ProjectileMode.Instant)
            {
                HandleInstantHit();
            }
        }

        private void Update()
        {
            if (!isLaunched) return;

            lifetime += Time.deltaTime;

            if (lifetime >= maxLifetime)
            {
                ReturnToPool();
                return;
            }

            MoveTowardsTarget();
        }

        private void MoveTowardsTarget()
        {
            if (hasHit) return;

            Vector3 direction = (targetPosition - transform.position).normalized;
            float distanceThisFrame = speed * Time.deltaTime;
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

            if (mode == ProjectileMode.SphereCastWhileFlying)
            {
                if (Physics.SphereCast(transform.position, sphereRadius, direction, out RaycastHit hit, distanceThisFrame))
                {
                    var enemyHealth = hit.collider.GetComponent<EnemyHealth>();
                    if (enemyHealth != null && enemyHealth.IsAlive)
                    {
                        transform.position = hit.point;
                        OnImpact(enemyHealth);
                        return;
                    }
                }
            }

            transform.position += direction * distanceThisFrame;

            if (distanceToTarget <= sphereRadius)
            {
                if (mode == ProjectileMode.CheckOnImpact)
                {
                    OnImpact(null);
                }
                else
                {
                    ReturnToPool();
                }
            }
        }

        private void HandleInstantHit()
        {
            if (Logs) Debug.Log($"[Projectile] Instant hit at {targetPosition}, damage: {damage}");

            hasHit = true;

            if (targetEnemy != null && targetEnemy.IsAlive)
            {
                targetEnemy.TakeDamage(damage);
                if (hasAreaDamage)
                {
                    DealAreaDamage(targetPosition);
                }
            }
            else if (hasAreaDamage)
            {
                DealAreaDamage(targetPosition);
            }

            ReturnToPool();
        }

        private void OnImpact(EnemyHealth hitEnemy)
        {
            if (hasHit) return;
            hasHit = true;

            if (Logs) Debug.Log($"[Projectile] Impact at {transform.position}, damage: {damage}, area: {hasAreaDamage}");

            if (hasAreaDamage)
            {
                DealAreaDamage(transform.position);
            }
            else
            {
                if (hitEnemy != null && hitEnemy.IsAlive)
                {
                    hitEnemy.TakeDamage(damage);
                }
                else
                {
                    DealDirectDamage();
                }
            }

            ReturnToPool();
        }

        private void DealDirectDamage()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, sphereRadius);
            foreach (var col in colliders)
            {
                var enemyHealth = col.GetComponent<EnemyHealth>();
                if (enemyHealth != null && enemyHealth.IsAlive)
                {
                    enemyHealth.TakeDamage(damage);
                    break;
                }
            }
        }

        private void DealAreaDamage(Vector3 center)
        {
            Collider[] colliders = Physics.OverlapSphere(center, areaDamageRadius);
            foreach (var col in colliders)
            {
                var enemyHealth = col.GetComponent<EnemyHealth>();
                if (enemyHealth != null && enemyHealth.IsAlive)
                {
                    float distancePercent = Vector3.Distance(center, col.transform.position) / areaDamageRadius;
                    float damageFalloff = Mathf.Lerp(1f, areaDamagePercent, distancePercent);
                    enemyHealth.TakeDamage(damage * damageFalloff);
                }
            }
        }

        private void ReturnToPool()
        {
            isLaunched = false;
            ProjectilePool.Instance?.Return(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, sphereRadius);

            if (hasAreaDamage)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, areaDamageRadius);
            }

            if (isLaunched)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, targetPosition);

                if (mode == ProjectileMode.SphereCastWhileFlying)
                {
                    Gizmos.color = Color.cyan;
                    Vector3 direction = (targetPosition - transform.position).normalized;
                    Gizmos.DrawWireSphere(transform.position + direction * speed * Time.deltaTime, sphereRadius);
                }
            }
        }
    }
}
