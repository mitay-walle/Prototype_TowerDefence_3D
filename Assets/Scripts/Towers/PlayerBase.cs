using UnityEngine;
using UnityEngine.Events;

namespace TD.Towers
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class PlayerBase : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 20;
        [SerializeField] private int currentHealth;

        public bool Logs = true;
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
            if (IsDestroyed)
            {
                if (Logs) Debug.LogWarning($"[Base] TakeDamage failed. IsDestroyed: {IsDestroyed}");
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - damage);
            onHealthChanged?.Invoke(currentHealth);
            if (Logs) Debug.Log($"[Base] TakeDamage {currentHealth} currentHealth");
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
            if (Logs) Debug.Log("[Base] OnBaseDestroyed");
            onBaseDestroyed?.Invoke();
        }

        private void OnDestroy()
        {
            onHealthChanged?.RemoveAllListeners();
            onBaseDestroyed?.RemoveAllListeners();
        }
    }
}
