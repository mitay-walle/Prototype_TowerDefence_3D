using System.Collections.Generic;
using UnityEngine;

namespace TD
{
    public class ProjectilePool : MonoBehaviour
    {
        private const string TOOLTIP_INITIAL_SIZE = "Number of projectiles pre-instantiated at start";
        private const string TOOLTIP_MAX_SIZE = "Maximum number of projectiles that can exist";
        private const string TOOLTIP_AUTO_EXPAND = "Create new projectiles when pool is empty (up to max size)";

        public static ProjectilePool Instance { get; private set; }

        [SerializeField] private Projectile projectilePrefab;
        [Tooltip(TOOLTIP_INITIAL_SIZE)]
        [SerializeField] private int initialPoolSize = 50;
        [Tooltip(TOOLTIP_MAX_SIZE)]
        [SerializeField] private int maxPoolSize = 200;
        [Tooltip(TOOLTIP_AUTO_EXPAND)]
        [SerializeField] private bool autoExpand = true;

        private Queue<Projectile> availableProjectiles = new Queue<Projectile>();
        private HashSet<Projectile> activeProjectiles = new HashSet<Projectile>();

        public int AvailableCount => availableProjectiles.Count;
        public int ActiveCount => activeProjectiles.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializePool();
        }

        public void Initialize(Projectile prefab, int initial, int max)
        {
            projectilePrefab = prefab;
            initialPoolSize = initial;
            maxPoolSize = max;

            InitializePool();
        }

        private void InitializePool()
        {
            if (projectilePrefab == null)
            {
                Debug.LogError("ProjectilePool: No projectile prefab assigned!");
                return;
            }

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateProjectile();
            }
        }

        private Projectile CreateProjectile()
        {
            Projectile projectile = Instantiate(projectilePrefab, transform);
            projectile.gameObject.SetActive(false);
            availableProjectiles.Enqueue(projectile);
            return projectile;
        }

        public Projectile Get()
        {
            Projectile projectile;

            if (availableProjectiles.Count > 0)
            {
                projectile = availableProjectiles.Dequeue();
            }
            else if (autoExpand && activeProjectiles.Count + availableProjectiles.Count < maxPoolSize)
            {
                projectile = CreateProjectile();
                availableProjectiles.Dequeue();
            }
            else
            {
                Debug.LogWarning("ProjectilePool: No available projectiles and max pool size reached!");
                return null;
            }

            projectile.gameObject.SetActive(true);
            activeProjectiles.Add(projectile);
            return projectile;
        }

        public void Return(Projectile projectile)
        {
            if (projectile == null) return;

            if (activeProjectiles.Remove(projectile))
            {
                projectile.gameObject.SetActive(false);
                projectile.transform.SetParent(transform);
                availableProjectiles.Enqueue(projectile);
            }
        }

        public void ReturnAll()
        {
            var projectilesToReturn = new List<Projectile>(activeProjectiles);
            foreach (var projectile in projectilesToReturn)
            {
                Return(projectile);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
