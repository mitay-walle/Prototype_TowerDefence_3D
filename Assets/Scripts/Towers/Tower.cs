using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TD.GameLoop;
using TD.Interactions;
using TD.Monsters;
using TD.Plugins.Timing;
using TD.Stats;
using TD.UI;
using TD.Weapons;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;

namespace TD.Towers
{
	public class Tower : MonoBehaviour, ITargetable, ITooltipValues
	{
		private const string TOOLTIP_ROTATION_PART = "Transform that rotates to aim at targets (optional, uses main transform if not set)";
		private const string TOOLTIP_FIRE_POINTS = "Spawn positions for projectiles (optional, uses turret position if empty)";
		private const string TOOLTIP_ROTATION_SPEED = "Degrees per second for turret rotation";
		private const string TOOLTIP_SHOW_RANGE = "Show range sphere in Scene view when selected";

		[SerializeField, Required] private TowerStats stats;
		[SerializeField, Required] private Projectile projectilePrefab;
		public int SellValue;

		[Tooltip(TOOLTIP_ROTATION_PART)]
		[SerializeField] private Transform turretRotationPart;
		[Tooltip(TOOLTIP_FIRE_POINTS)]
		[SerializeField] private Transform[] firePoints;

		[Tooltip(TOOLTIP_ROTATION_SPEED)]
		[SerializeField] private float rotationSpeed = 180f;
		[Tooltip(TOOLTIP_SHOW_RANGE)]
		[SerializeField] private bool showRange = true;
		[SerializeField] private Color rangeColor = new Color(1, 0, 0, 0.3f);
		public TowerStatsVisual TowerStatsVisual;
		[SerializeField] public TargetPriority TargetPriority = TargetPriority.Nearest;
		[SerializeField] private MonoBehaviour weaponComponent;
		public IWeapon Weapon => weaponComponent as IWeapon;
		[SerializeField] private bool predictiveAiming = false;

		public UnityEvent<MonsterHealth> onTargetAcquired;
		public UnityEvent onTargetLost;
		public UnityEvent onFire;

		[SerializeField] private bool Logs = false;

		private MonsterHealth currentTarget;
		[ShowInInspector, ReadOnly] private Timer fireTimer;
		private List<MonsterHealth> enemiesInRange = new List<MonsterHealth>();

		public TowerStats Stats => stats;
		public MonsterHealth CurrentTarget => currentTarget;
		public bool HasTarget => currentTarget != null && currentTarget.IsAlive;
		private Collider[] colliders = new Collider[500];
		int foundCount;
		public bool IsTargetingDirty { get; set; }

		private void Start()
		{
			if (stats == null)
			{
				Debug.LogError($"Turret {name} has no stats assigned!", this);
				enabled = false;
				return;
			}

			fireTimer = new(1 / stats.FireRate);
		}

		private void Update()
		{
			UpdateEnemiesInRange();
			UpdateTarget();

			if (HasTarget)
			{
				RotateTowardsTarget();
				UpdateFiring();
			}
		}

		private void UpdateEnemiesInRange()
		{
			enemiesInRange.Clear();

			int inRangeCount = Physics.OverlapSphereNonAlloc(transform.position, stats.Range, colliders);
			for (var i = 0; i < inRangeCount; i++)
			{
				Collider col = colliders[i];
				var enemy = col.GetComponent<MonsterHealth>();
				if (enemy != null && enemy.IsAlive)
				{
					enemiesInRange.Add(enemy);
				}
			}
		}

		private void UpdateTarget()
		{
			if (HasTarget)
			{
				float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
				if (distanceToTarget > stats.Range)
				{
					LoseTarget();
				}

				return;
			}

			if (enemiesInRange.Count == 0)
			{
				return;
			}

			currentTarget = SelectTarget();
			if (currentTarget != null)
			{
				onTargetAcquired?.Invoke(currentTarget);
			}
		}

		private MonsterHealth SelectTarget()
		{
			if (enemiesInRange.Count == 0) return null;

			MonsterHealth target = null;

			switch (TargetPriority)
			{
				case TargetPriority.Nearest:
					target = GetNearestEnemy();
					break;

				case TargetPriority.Farthest:
					target = GetFarthestAlongPath();
					break;

				case TargetPriority.Strongest:
					target = GetStrongestEnemy();
					break;

				case TargetPriority.Weakest:
					target = GetWeakestEnemy();
					break;

				default:
					target = enemiesInRange[0];
					break;
			}

			if (Logs && target != null) Debug.Log($"[Turret] {name} selected target: {target.name} using {TargetPriority} priority");
			return target;
		}

		private MonsterHealth GetNearestEnemy()
		{
			MonsterHealth nearest = null;
			float minDistance = float.MaxValue;

			foreach (var enemy in enemiesInRange)
			{
				float distance = Vector3.Distance(transform.position, enemy.transform.position);
				if (distance < minDistance)
				{
					minDistance = distance;
					nearest = enemy;
				}
			}

			return nearest;
		}

		private MonsterHealth GetFarthestAlongPath()
		{
			MonsterHealth farthest = null;
			float minDistanceToBase = float.MaxValue;

			var baseTransform = FindFirstObjectByType<Base>()?.transform;
			if (baseTransform == null) return GetNearestEnemy();

			foreach (var enemy in enemiesInRange)
			{
				float distanceToBase = Vector3.Distance(enemy.transform.position, baseTransform.position);
				if (distanceToBase < minDistanceToBase)
				{
					minDistanceToBase = distanceToBase;
					farthest = enemy;
				}
			}

			return farthest ?? GetNearestEnemy();
		}

		private MonsterHealth GetStrongestEnemy()
		{
			MonsterHealth strongest = null;
			float maxHealth = 0;

			foreach (var enemy in enemiesInRange)
			{
				if (enemy.CurrentHealth > maxHealth)
				{
					maxHealth = enemy.CurrentHealth;
					strongest = enemy;
				}
			}

			return strongest;
		}

		private MonsterHealth GetWeakestEnemy()
		{
			MonsterHealth weakest = null;
			float minHealth = float.MaxValue;

			foreach (var enemy in enemiesInRange)
			{
				if (enemy.CurrentHealth < minHealth)
				{
					minHealth = enemy.CurrentHealth;
					weakest = enemy;
				}
			}

			return weakest;
		}

		private void RotateTowardsTarget()
		{
			if (turretRotationPart == null || currentTarget == null) return;

			Vector3 targetPosition = currentTarget.transform.position;
			Vector3 direction = (targetPosition - turretRotationPart.position).normalized;
			direction.y = 0; // Keep rotation only on Y axis

			if (direction != Vector3.zero)
			{
				Quaternion targetRotation = Quaternion.LookRotation(direction);
				turretRotationPart.rotation = Quaternion.RotateTowards(turretRotationPart.rotation, targetRotation, rotationSpeed * Time.deltaTime);
			}
		}

		private void UpdateFiring()
		{
			if (fireTimer.CheckAndRestart(1 / stats.FireRate))
			{
				Fire();
			}
		}

		private void Fire()
		{
			if (!HasTarget) return;

			if (Logs) Debug.Log($"[Turret] {name} firing at {currentTarget.name}");

			onFire?.Invoke();

			if (firePoints == null || firePoints.Length == 0)
			{
				FireFromPosition(turretRotationPart != null ? turretRotationPart.position : transform.position);
			}
			else
			{
				foreach (var firePoint in firePoints)
				{
					if (firePoint != null)
					{
						FireFromPosition(firePoint.position);
					}
				}
			}
		}

		private void FireFromPosition(Vector3 position)
		{
			if (Weapon != null)
			{
				Vector3 targetPosition = currentTarget.transform.position;
				Vector3 direction = (targetPosition - position).normalized;

				if (predictiveAiming)
				{
					targetPosition = PredictTargetPosition(currentTarget);
					direction = (targetPosition - position).normalized;
				}

				Weapon.Fire(position, direction, currentTarget.transform, stats.Damage);
			}
			else
			{
				Projectile projectile = GameObjectPool.Instance.Get(projectilePrefab);
				if (projectile != null)
				{
					Vector3 targetPosition = currentTarget.transform.position;

					if (predictiveAiming)
					{
						targetPosition = PredictTargetPosition(currentTarget);
					}

					projectile.transform.position = position;
					projectile.Launch(targetPosition, stats.Damage, stats.ProjectileSpeed, currentTarget);
				}
			}
		}

		private Vector3 PredictTargetPosition(MonsterHealth target)
		{
			var movement = target.GetComponent<MonsterMove>();
			if (movement == null) return target.transform.position;

			float distance = Vector3.Distance(transform.position, target.transform.position);
			float timeToReach = distance / stats.ProjectileSpeed;

			Vector3 targetVelocity = movement.Speed * target.transform.forward;
			return target.transform.position + targetVelocity * timeToReach;
		}

		private void LoseTarget()
		{
			currentTarget = null;
			onTargetLost?.Invoke();
		}

		public bool CanUpgrade()
		{
			return stats != null && stats.CanUpgrade;
		}

		public void Upgrade()
		{
			if (!CanUpgrade()) return;

			int currentTowerCost = Stats.UpgradeCost;
			if (ResourceManager.Instance != null && currentTowerCost > 0)
			{
				if (!ResourceManager.Instance.TrySpend(currentTowerCost))
				{
					Debug.Log("TowerUpgrade: Cannot afford upgrade tower");
					return;
				}
			}

			stats.UpgradeGrade();
			IsTargetingDirty = true;
		}

		public void OnSelected()
		{
			TowerStatsVisual.Show(Stats);
			if (Logs) Debug.Log($"[Turret] {name} OnSelected");
		}

		public void OnDeselected()
		{
			TowerStatsVisual.Hide();
		}


		private void OnDrawGizmosSelected()
		{
			if (stats == null || !showRange) return;

			Gizmos.color = rangeColor;
			Gizmos.DrawWireSphere(transform.position, stats.Range);

			if (HasTarget)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawLine(transform.position, currentTarget.transform.position);
			}
		}

		private void OnDestroy()
		{
			onTargetAcquired?.RemoveAllListeners();
			onTargetLost?.RemoveAllListeners();
			onFire?.RemoveAllListeners();
		}

		public LocalizedString Title => null;
		public LocalizedString Description => new LocalizedString("UI", "tooltip.tower.description")
		{
			Arguments = new object[]
			{
				Stats.Damage,
				Stats.FireRate,
				Stats.Range,
				TargetPriority.ToString(),
				CurrentTarget != null ? CurrentTarget.name : "-",
				CanUpgrade() ? Stats.UpgradeCost.ToString() : "-"
			},
		};
		public Action OnTooltipButtonClick => CanUpgrade() ? Upgrade : null;
		public LocalizedString TooltipButtonText => new LocalizedString("UI", "tooltip.tower.upgrade");
	}
}