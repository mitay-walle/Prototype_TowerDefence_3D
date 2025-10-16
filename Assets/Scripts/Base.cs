using UnityEngine;
using UnityEngine.Events;

namespace TD
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Base : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 20;
        [SerializeField] private int currentHealth;

        public UnityEvent<int> onHealthChanged;
        public UnityEvent onBaseDestroyed;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public float HealthPercent => (float)currentHealth / maxHealth;
        public bool IsDestroyed => currentHealth <= 0;

        private void Awake()
        {
            currentHealth = maxHealth;

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        public void Initialize(int health)
        {
            maxHealth = health;
            currentHealth = health;
            onHealthChanged?.Invoke(currentHealth);
        }

        public void TakeDamage(int damage)
        {
            if (IsDestroyed) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            onHealthChanged?.Invoke(currentHealth);

            if (IsDestroyed)
            {
                OnBaseDestroyed();
            }
        }

        public void Repair(int amount)
        {
            if (IsDestroyed) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            onHealthChanged?.Invoke(currentHealth);
        }

        private void OnBaseDestroyed()
        {
            onBaseDestroyed?.Invoke();
        }

        private void OnDestroy()
        {
            onHealthChanged?.RemoveAllListeners();
            onBaseDestroyed?.RemoveAllListeners();
        }
    }
}
