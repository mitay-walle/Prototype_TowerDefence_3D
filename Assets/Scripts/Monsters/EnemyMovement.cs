using TD.Towers;
using UnityEngine;
using UnityEngine.AI;

namespace TD.Monsters
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyMovement : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 3f;

        private NavMeshAgent agent;
        private EnemyHealth health;
        private static Base cachedBase;

        public float Speed
        {
            get => agent != null ? agent.speed : baseSpeed;
            set
            {
                baseSpeed = value;
                if (agent != null) agent.speed = value;
            }
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<EnemyHealth>();

            agent.speed = baseSpeed;
            agent.stoppingDistance = 0.1f;

            if (cachedBase == null)
            {
                cachedBase = FindFirstObjectByType<Base>();
            }

            if (cachedBase != null)
            {
                agent.SetDestination(cachedBase.transform.position);
            }

            if (health != null)
            {
                health.onDeath.AddListener(OnDeath);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var baseComponent = other.GetComponent<Base>();
            if (baseComponent != null && health.IsAlive)
            {
                baseComponent.TakeDamage(1);
                health.onDeath?.Invoke();
                Destroy(gameObject, 0.1f);
            }
        }

        private void OnDeath()
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.onDeath.RemoveListener(OnDeath);
            }
        }
    }
}
