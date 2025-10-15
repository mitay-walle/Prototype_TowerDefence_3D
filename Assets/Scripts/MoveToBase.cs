using UnityEngine;
using UnityEngine.AI;

namespace TD
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    public class MoveToBase : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 3f;
        [SerializeField] private float stoppingDistance = 0.5f;

        private NavMeshAgent agent;
        private EnemyHealth health;
        private Base targetBase;
        private bool hasReachedBase = false;

        public float Speed
        {
            get => agent != null ? agent.speed : baseSpeed;
            set
            {
                baseSpeed = value;
                if (agent != null) agent.speed = value;
            }
        }

        public bool HasReachedBase => hasReachedBase;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<EnemyHealth>();

            agent.speed = baseSpeed;
            agent.stoppingDistance = stoppingDistance;
        }

        private void Start()
        {
            FindAndSetTarget();
        }

        private void Update()
        {
            if (!health.IsAlive)
            {
                agent.isStopped = true;
                return;
            }

            if (targetBase == null)
            {
                FindAndSetTarget();
                return;
            }

            CheckIfReachedBase();
        }

        public void SetTarget(Base target)
        {
            targetBase = target;
            if (target != null && agent != null)
            {
                agent.SetDestination(target.transform.position);
            }
        }

        private void FindAndSetTarget()
        {
            targetBase = FindFirstObjectByType<Base>();
            if (targetBase != null)
            {
                SetTarget(targetBase);
            }
        }

        private void CheckIfReachedBase()
        {
            if (hasReachedBase) return;

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                hasReachedBase = true;
                OnReachedBase();
            }
        }

        private void OnReachedBase()
        {
            if (targetBase != null)
            {
                targetBase.TakeDamage(1);
            }

            Destroy(gameObject, 0.1f);
        }

        public void SetSpeed(float speed)
        {
            Speed = speed;
        }

        private void OnDisable()
        {
            if (agent != null)
            {
                agent.isStopped = true;
            }
        }
    }
}
