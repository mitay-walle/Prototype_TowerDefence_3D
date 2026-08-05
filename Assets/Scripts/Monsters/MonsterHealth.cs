using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TD.Interactions;
using TD.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;

namespace TD.Monsters
{
	public enum MonsterTerminalReason
	{
		None,
		Kill,
		Leak
	}

	public class MonsterHealth : MonoBehaviour, ITooltipValues
	{
		[SerializeField] private float maxHealth = 100f;
		[SerializeField] private float currentHealth;
		[SerializeField] private float deathDelay = 0.5f;
		[SerializeField] private float earlyKillBonusMultiplier = 1.5f;
		[SerializeField] private float earlyKillThreshold = 0.5f;

		[FoldoutGroup("Events")] public UnityEvent<float> onHealthChanged;
		[FoldoutGroup("Events")] public UnityEvent<float> onDamageTaken;
		[FoldoutGroup("Events")] public UnityEvent onDeath;
		[FoldoutGroup("Events")] public UnityEvent onLeak;
		[FoldoutGroup("Events")] public UnityEvent<int> onRewardGiven;

		[SerializeField] private bool changeColorOnDamage = true;
		[SerializeField] private float colorChangeDuration = 0.2f;
		private MeshRenderer[] meshRenderers;
		private Color[][] originalColors;
		private float colorChangeTimer;
		MonsterStats stats;
		private MonsterTerminalReason terminalReason;

		public float MaxHealth => maxHealth;
		public float CurrentHealth => currentHealth;
		public float HealthPercent => currentHealth / maxHealth;
		public bool IsAlive => currentHealth > 0;
		public MonsterTerminalReason TerminalReason => terminalReason;

		private void Awake()
		{
			if (onHealthChanged == null) onHealthChanged = new UnityEvent<float>();
			if (onDamageTaken == null) onDamageTaken = new UnityEvent<float>();
			if (onDeath == null) onDeath = new UnityEvent();
			if (onLeak == null) onLeak = new UnityEvent();
			if (onRewardGiven == null) onRewardGiven = new UnityEvent<int>();

			currentHealth = maxHealth;

			if (changeColorOnDamage)
			{
				meshRenderers = GetComponentsInChildren<MeshRenderer>();
				CacheOriginalColors();
			}

			if (TryGetComponent(out stats))
			{
				stats.OnRecalculateStatsFinished -= SetupValues;
				stats.OnRecalculateStatsFinished += SetupValues;
			}
		}

		private void SetupValues()
		{
			maxHealth = stats.Health;
			currentHealth = Mathf.Min(maxHealth);
		}

		private void Update()
		{
			if (colorChangeTimer > 0)
			{
				colorChangeTimer -= Time.deltaTime;
				if (colorChangeTimer <= 0)
				{
					RestoreOriginalColors();
				}
			}
		}

		public void Initialize(float health)
		{
			maxHealth = health;
			currentHealth = health;
			terminalReason = MonsterTerminalReason.None;
		}

		public bool TryLeak(System.Action applyLeakEffect)
		{
			if (!IsAlive || terminalReason != MonsterTerminalReason.None)
				return false;

			terminalReason = MonsterTerminalReason.Leak;
			currentHealth = 0f;
			OnHealthChanged();
			applyLeakEffect?.Invoke();
			onLeak?.Invoke();
			return true;
		}

		public void TakeDamage(float damage)
		{
			if (!IsAlive) return;

			var appliedDamage = Mathf.Min(currentHealth, Mathf.Max(0f, damage));
			currentHealth = Mathf.Max(0, currentHealth - appliedDamage);
			OnHealthChanged();
			if (appliedDamage > 0f)
				onDamageTaken?.Invoke(appliedDamage);

			if (changeColorOnDamage)
			{
				FlashDamageColor();
			}

			if (!IsAlive)
			{
				Die();
			}
		}

		public void Heal(float amount)
		{
			if (!IsAlive) return;

			currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
			OnHealthChanged();
		}

		private void OnHealthChanged()
		{
			if (TryGetComponent(out ITargetable targetable))
			{
				targetable.IsTargetingDirty = true;
			}

			onHealthChanged?.Invoke(currentHealth);
		}

		private void Die()
		{
			if (terminalReason != MonsterTerminalReason.None)
				return;

			terminalReason = MonsterTerminalReason.Kill;
			onDeath?.Invoke();

			int rewardAmount = stats.InstantReward.ValueInt;
			int finalReward = rewardAmount;

			if (HealthPercent >= earlyKillThreshold)
			{
				finalReward = Mathf.RoundToInt(rewardAmount * earlyKillBonusMultiplier);
			}

			onRewardGiven?.Invoke(finalReward);

			if (Application.isPlaying)
				Destroy(gameObject, deathDelay);
			else
				DestroyImmediate(gameObject);
		}

		private void CacheOriginalColors()
		{
			if (meshRenderers == null) return;

			originalColors = new Color[meshRenderers.Length][];
			for (int i = 0; i < meshRenderers.Length; i++)
			{
				if (meshRenderers[i] != null)
				{
					var materials = meshRenderers[i].materials;
					originalColors[i] = new Color[materials.Length];
					for (int j = 0; j < materials.Length; j++)
					{
						if (materials[j].HasProperty("_Color"))
						{
							originalColors[i][j] = materials[j].color;
						}
					}
				}
			}
		}

		private void FlashDamageColor()
		{
			if (meshRenderers == null) return;

			colorChangeTimer = colorChangeDuration;

			foreach (var renderer in meshRenderers)
			{
				if (renderer != null)
				{
					foreach (var material in renderer.materials)
					{
						if (material.HasProperty("_Color"))
						{
							material.color = Color.red;
						}
					}
				}
			}
		}

		private void RestoreOriginalColors()
		{
			if (meshRenderers == null || originalColors == null) return;

			for (int i = 0; i < meshRenderers.Length; i++)
			{
				if (meshRenderers[i] != null && i < originalColors.Length)
				{
					var materials = meshRenderers[i].materials;
					for (int j = 0; j < materials.Length && j < originalColors[i].Length; j++)
					{
						if (materials[j].HasProperty("_Color"))
						{
							materials[j].color = originalColors[i][j];
						}
					}
				}
			}
		}

		private void OnDestroy()
		{
			onHealthChanged?.RemoveAllListeners();
			onDeath?.RemoveAllListeners();
			onLeak?.RemoveAllListeners();
			onRewardGiven?.RemoveAllListeners();
		}

		public IEnumerable<(Action, LocalizedString)> OnTooltipButtonClick { get; }

		[SerializeField] LocalizedString _title;

		public LocalizedString Title
		{
			get
			{
				return new LocalizedString("UI", "MonsterStyle{0}")
				{
					Arguments = new object[] { _title.GetLocalizedString() }
				};
			}
		}

		public LocalizedString Description => new LocalizedString("UI", "tooltip.enemy.description")
		{
			Arguments = new object[]
			{
				CurrentHealth,
				MaxHealth
			},
		};
	}
}
