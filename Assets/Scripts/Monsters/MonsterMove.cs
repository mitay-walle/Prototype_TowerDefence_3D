using TD.Towers;
using UnityEngine;
using UnityEngine.AI;

namespace TD.Monsters
{
	[RequireComponent(typeof(NavMeshAgent))]
	[RequireComponent(typeof(MonsterHealth))]
	public class MonsterMove : MonoBehaviour
	{
		private const float RepathInterval = 0.5f;
		private const float StuckTimeout = 1.5f;
		private const float MaximumTraversableAgentRadius = 0.35f;
		private const float TankSpeedMultiplier = 0.8f;
		private const float RunnerBurstMultiplier = 1.4f;
		private const float RunnerBurstDuration = 1.25f;
		private const float RunnerBurstCycle = 4f;
		private const float BerserkerMinSpeedMultiplier = 0.95f;
		private const float BerserkerMaxSpeedMultiplier = 1.45f;
		private const float SkirmisherAmplitude = 0.08f;
		private const float SkirmisherFrequency = 2.2f;
		private const float SkirmisherMaxLateralSpeed = 0.25f;
		private const float MinimumProgressDistance = 0.05f;
		private const float StallRecoveryStepDistance = 0.15f;

		private PlayerBase targetBase;

		[field: SerializeField] public float baseSpeed { get; private set; } = 3f;
		private float calculatedSpeed;
		private NavMeshAgent agent;
		private MonsterHealth health;
		MonsterStats stats;
		private float nextRepathTime;
		private float lastProgressTime;
		private float bestPathDistance = float.PositiveInfinity;
		private float bestBaseDistance = float.PositiveInfinity;
		private float movementPhaseOffset;
		private float previousLateralAmount;
		private MonsterArchetype archetype = MonsterArchetype.Standard;
		private bool terminalTriggered;

		public MonsterArchetype Archetype => archetype;
		public PlayerBase BaseTarget => targetBase;

		public static bool IsRouteProgressFinite(float remainingDistance, float baseDistance)
		{
			return IsFinite(remainingDistance) && IsFinite(baseDistance);
		}

		public float Speed
		{
			get => calculatedSpeed > 0f ? calculatedSpeed : baseSpeed;
			set
			{
				calculatedSpeed = Mathf.Max(0f, value);
				ApplyMovementConfiguration();
			}
		}

		private void Awake()
		{
			agent = GetComponent<NavMeshAgent>();
			health = GetComponent<MonsterHealth>();

			if (agent != null)
			{
				agent.speed = baseSpeed;
				agent.stoppingDistance = 0.1f;
				agent.autoRepath = true;
				agent.radius = Mathf.Min(agent.radius, MaximumTraversableAgentRadius);
				agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
			}
			calculatedSpeed = baseSpeed;
			movementPhaseOffset = Mathf.Abs(GetInstanceID() % 1000) * 0.001f;
			lastProgressTime = Time.time;

			if (health != null)
			{
				health.onDeath.AddListener(OnDeath);
			}

			if (TryGetComponent(out stats))
			{
				stats.OnRecalculateStatsFinished -= SetupValues;
				stats.OnRecalculateStatsFinished += SetupValues;
				SetupValues();
			}
		}

		public bool Initialize(PlayerBase target)
		{
			if (target == null)
				return false;

			targetBase = target;
			if (agent != null && agent.isOnNavMesh)
				RequestBasePath();

			return true;
		}

		private void SetupValues()
		{
			if (stats == null || stats.statsSO == null)
				return;

			archetype = stats.statsSO.Archetype;
			calculatedSpeed = Mathf.Max(0f, stats.MoveSpeed);
			ApplyMovementConfiguration();
		}

		private void Update()
		{
			if (terminalTriggered || health == null || !health.IsAlive || agent == null || targetBase == null)
				return;

			ApplyMovementConfiguration();

			if (HasReachedBase())
			{
				ReachBase(targetBase);
				return;
			}

			if (!agent.isOnNavMesh)
				return;

			if (agent.pathPending)
			{
				if (Time.time - lastProgressTime < StuckTimeout)
					return;

				RequestBasePath();
				return;
			}

			ApplySkirmisherOffset();
			if (RecordRouteProgress())
				return;

			var pathDistance = GetPathDistance();
			var stalled = agent.pathStatus != NavMeshPathStatus.PathComplete || !agent.hasPath || !IsRouteProgressFinite(pathDistance, GetBaseDistance()) ||
				Time.time - lastProgressTime >= StuckTimeout;
			if (stalled && Time.time >= nextRepathTime)
			{
				if (TryRecoverStalledAgent())
				{
					nextRepathTime = Time.time + RepathInterval;
					return;
				}

				RequestBasePath();
			}
		}

		private bool TryRecoverStalledAgent()
		{
			if (agent == null || !agent.isOnNavMesh || !agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete)
				return false;

			if (!IsFinite(agent.remainingDistance))
			{
				RequestBasePath();
				return agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathComplete;
			}

			var direction = agent.steeringTarget - agent.nextPosition;
			direction.y = 0f;
			if (direction.sqrMagnitude < 0.0025f)
				return false;

			var distance = Mathf.Min(StallRecoveryStepDistance, Mathf.Max(agent.speed * Time.deltaTime, 0.05f));
			var target = agent.nextPosition + direction.normalized * distance;
			if (!NavMesh.SamplePosition(target, out NavMeshHit hit, 0.15f, agent.areaMask))
				return false;

			agent.isStopped = false;
			agent.Move(hit.position - agent.nextPosition);
			if (!agent.hasPath)
				RequestBasePath();

			return true;
		}

		private void RequestBasePath()
		{
			nextRepathTime = Time.time + RepathInterval;
			lastProgressTime = Time.time;
			previousLateralAmount = 0f;

			if (agent == null || !agent.isOnNavMesh || targetBase == null)
				return;

			agent.isStopped = false;
			agent.SetDestination(targetBase.transform.position);
		}

		private void ApplyMovementConfiguration()
		{
			if (agent == null)
				return;

			if (stats != null && stats.statsSO != null)
				archetype = stats.statsSO.Archetype;

			agent.acceleration = archetype == MonsterArchetype.Tank ? 5f : 12f;
			agent.angularSpeed = archetype == MonsterArchetype.Skirmisher ? 540f : 360f;

			var speedMultiplier = 1f;
			switch (archetype)
			{
				case MonsterArchetype.Tank:
					speedMultiplier = TankSpeedMultiplier;
					break;

				case MonsterArchetype.Runner:
					var runnerPhase = Mathf.Repeat(Time.time + movementPhaseOffset, RunnerBurstCycle);
					if (runnerPhase <= RunnerBurstDuration)
						speedMultiplier = RunnerBurstMultiplier;
					break;

				case MonsterArchetype.Berserker:
					var healthLoss = health != null ? 1f - Mathf.Clamp01(health.HealthPercent) : 0f;
					speedMultiplier = Mathf.Lerp(BerserkerMinSpeedMultiplier, BerserkerMaxSpeedMultiplier, healthLoss);
					break;
			}

			agent.speed = calculatedSpeed * speedMultiplier;
		}

		private void ApplySkirmisherOffset()
		{
			if (archetype != MonsterArchetype.Skirmisher || !agent.hasPath ||
				Time.time - lastProgressTime >= StuckTimeout || agent.desiredVelocity.sqrMagnitude < 0.01f)
				return;

			var forward = agent.desiredVelocity;
			forward.y = 0f;
			if (forward.sqrMagnitude < 0.01f)
				return;

			forward.Normalize();
			var lateral = Vector3.Cross(Vector3.up, forward).normalized;
			var amplitude = Mathf.Min(SkirmisherAmplitude, agent.radius * 0.25f);
			var targetLateralAmount = Mathf.Sin((Time.time + movementPhaseOffset) * SkirmisherFrequency) * amplitude;
			var nextLateralAmount = Mathf.MoveTowards(previousLateralAmount, targetLateralAmount,
				SkirmisherMaxLateralSpeed * Time.deltaTime);
			var delta = lateral * (nextLateralAmount - previousLateralAmount);
			previousLateralAmount = nextLateralAmount;

			if (delta.sqrMagnitude < 0.000001f)
				return;

			if (NavMesh.SamplePosition(agent.nextPosition + delta, out NavMeshHit hit, 0.1f, agent.areaMask))
				agent.Move(hit.position - agent.nextPosition);
		}

		private bool RecordRouteProgress()
		{
			var progress = false;
			var pathDistance = GetPathDistance();
			var baseDistance = GetBaseDistance();
			if (IsFinite(pathDistance) && pathDistance < bestPathDistance - MinimumProgressDistance)
			{
				bestPathDistance = pathDistance;
				progress = true;
			}

			if (IsFinite(baseDistance) && baseDistance < bestBaseDistance - MinimumProgressDistance)
			{
				bestBaseDistance = baseDistance;
				progress = true;
			}

			if (progress)
				lastProgressTime = Time.time;

			return progress;
		}

		private float GetPathDistance()
		{
			if (agent == null || !agent.hasPath)
				return float.PositiveInfinity;

			if (IsFinite(agent.remainingDistance))
				return agent.remainingDistance;

			var corners = agent.path.corners;
			if (corners == null || corners.Length < 2)
				return float.PositiveInfinity;

			var bestLateralDistanceSqr = float.PositiveInfinity;
			var bestDistance = float.PositiveInfinity;
			var distanceToEnd = 0f;
			for (var index = corners.Length - 2; index >= 0; index--)
			{
				var start = corners[index];
				var end = corners[index + 1];
				var segment = end - start;
				var segmentLength = segment.magnitude;
				if (segmentLength < 0.001f)
					continue;

				var normalizedSegment = segment / segmentLength;
				var projection = Mathf.Clamp01(Vector3.Dot(agent.nextPosition - start, normalizedSegment) / segmentLength);
				var closestPoint = start + segment * projection;
				var lateralDistanceSqr = (agent.nextPosition - closestPoint).sqrMagnitude;
				var segmentDirection = Vector3.Dot(agent.desiredVelocity, normalizedSegment);
				var isForwardSegment = segmentDirection >= -0.1f || index == corners.Length - 2;
				if (isForwardSegment && lateralDistanceSqr < bestLateralDistanceSqr)
				{
					bestLateralDistanceSqr = lateralDistanceSqr;
					bestDistance = (1f - projection) * segmentLength + distanceToEnd;
				}
				distanceToEnd += segmentLength;
			}

			return bestDistance;
		}

		private float GetBaseDistance()
		{
			return targetBase != null ? Vector3.Distance(transform.position, targetBase.transform.position) : float.PositiveInfinity;
		}

		private static bool IsFinite(float value)
		{
			return !float.IsNaN(value) && !float.IsInfinity(value);
		}

		private bool HasReachedBase()
		{
			var baseCollider = targetBase.GetComponent<Collider>();
			if (baseCollider != null)
				return Vector3.Distance(transform.position, baseCollider.ClosestPoint(transform.position)) <= agent.radius + 0.05f;

			return Vector3.Distance(transform.position, targetBase.transform.position) <= Mathf.Max(agent.stoppingDistance, 0.25f);
		}

		private void ReachBase(PlayerBase baseComponent)
		{
			if (terminalTriggered || health == null || !health.IsAlive)
				return;

			if (agent != null && agent.isOnNavMesh)
				agent.isStopped = true;

			var damage = stats != null ? stats.Damage.ValueInt : 0;
			if (!health.TryLeak(() => baseComponent.TakeDamage(damage)))
				return;

			terminalTriggered = true;
			if (Application.isPlaying)
				Destroy(gameObject, 0.1f);
			else
				DestroyImmediate(gameObject);
		}
		
		private void OnTriggerEnter(Collider other)
		{
			var baseComponent = other.GetComponent<PlayerBase>();
			if (baseComponent != null && health.IsAlive)
			{
				ReachBase(baseComponent);
			}
		}

		private void OnDeath()
		{
			terminalTriggered = true;
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

			if (stats != null)
			{
				stats.OnRecalculateStatsFinished -= SetupValues;
			}
		}
	}
}
