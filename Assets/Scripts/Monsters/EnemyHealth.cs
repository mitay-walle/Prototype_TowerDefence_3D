using UnityEngine;
using UnityEngine.Events;

namespace TD.Monsters
{
    public class EnemyHealth : MonoBehaviour
    {
        private const string TOOLTIP_MAX_HEALTH = "Maximum health points for this enemy";
        private const string TOOLTIP_DEATH_DELAY = "Delay before destroying the enemy GameObject after death";
        private const string TOOLTIP_GIVE_REWARD = "Whether this enemy gives currency reward on death";
        private const string TOOLTIP_REWARD_AMOUNT = "Amount of currency given on death";
        private const string TOOLTIP_CHANGE_COLOR = "Flash red color when taking damage";
        private const string TOOLTIP_COLOR_DURATION = "Duration of damage color flash effect";
        private const string TOOLTIP_EARLY_KILL_BONUS = "Bonus multiplier for killing enemy with >50% health remaining";
        private const string TOOLTIP_EARLY_KILL_THRESHOLD = "Health percentage threshold for early kill bonus (0.5 = 50%)";

        [Tooltip(TOOLTIP_MAX_HEALTH)]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Tooltip(TOOLTIP_DEATH_DELAY)]
        [SerializeField] private float deathDelay = 0.5f;
        [Tooltip(TOOLTIP_GIVE_REWARD)]
        [SerializeField] private bool giveReward = true;
        [Tooltip(TOOLTIP_REWARD_AMOUNT)]
        [SerializeField] private int rewardAmount = 10;
        [Tooltip(TOOLTIP_EARLY_KILL_BONUS)]
        [SerializeField] private float earlyKillBonusMultiplier = 1.5f;
        [Tooltip(TOOLTIP_EARLY_KILL_THRESHOLD)]
        [SerializeField] private float earlyKillThreshold = 0.5f;

        public UnityEvent<float> onHealthChanged;
        public UnityEvent onDeath;
        public UnityEvent<int> onRewardGiven;

        [Tooltip(TOOLTIP_CHANGE_COLOR)]
        [SerializeField] private bool changeColorOnDamage = true;
        [Tooltip(TOOLTIP_COLOR_DURATION)]
        [SerializeField] private float colorChangeDuration = 0.2f;
        private MeshRenderer[] meshRenderers;
        private Color[][] originalColors;
        private float colorChangeTimer;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercent => currentHealth / maxHealth;
        public bool IsAlive => currentHealth > 0;

        private void Awake()
        {
            currentHealth = maxHealth;

            if (changeColorOnDamage)
            {
                meshRenderers = GetComponentsInChildren<MeshRenderer>();
                CacheOriginalColors();
            }
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
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            onHealthChanged?.Invoke(currentHealth);

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
            onHealthChanged?.Invoke(currentHealth);
        }

        private void Die()
        {
            onDeath?.Invoke();

            if (giveReward)
            {
                int finalReward = rewardAmount;

                if (HealthPercent >= earlyKillThreshold)
                {
                    finalReward = Mathf.RoundToInt(rewardAmount * earlyKillBonusMultiplier);
                }

                onRewardGiven?.Invoke(finalReward);
            }

            Destroy(gameObject, deathDelay);
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
            onRewardGiven?.RemoveAllListeners();
        }
    }
}
