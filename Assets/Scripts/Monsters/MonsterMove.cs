using TD.Towers;
using UnityEngine;
using UnityEngine.AI;

namespace TD.Monsters
{
	[RequireComponent(typeof(NavMeshAgent))]
	[RequireComponent(typeof(MonsterHealth))]
	public class MonsterMove : MonoBehaviour
	{
		private static Base cachedBase;

		[field: SerializeField] public float baseSpeed { get; private set; } = 3f;
		private float calculatedSpeed;
		private NavMeshAgent agent;
		private MonsterHealth health;
		MonsterStats stats;
		
		public float Speed
		{
			get => agent != null ? agent.speed : baseSpeed;
			set
			{
				calculatedSpeed = value;
				if (agent != null) agent.speed = value;
			}
		}

		private void Awake()
		{
			agent = GetComponent<NavMeshAgent>();
			health = GetComponent<MonsterHealth>();

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

			if (TryGetComponent(out stats))
			{
				stats.OnRecalculateStatsFinished -= SetupValues;
				stats.OnRecalculateStatsFinished += SetupValues;
			}
		}

		private void SetupValues()
		{
			agent.speed = stats.MoveSpeed;
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