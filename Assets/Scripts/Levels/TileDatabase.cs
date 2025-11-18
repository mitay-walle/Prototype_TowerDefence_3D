using System.Collections.Generic;
using UnityEngine;

namespace TD.Levels
{
    public class TileDatabase : MonoBehaviour
    {
        [System.Serializable]
        public class TileDatabaseEntry
        {
            public RoadConnections connections;
            public GameObject prefab;
        }

        [SerializeField] private List<TileDatabaseEntry> tiles = new List<TileDatabaseEntry>();

        private Dictionary<RoadConnections, GameObject> tilePrefabs;

        public static TileDatabase Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePrefabs();
        }

        private void InitializePrefabs()
        {
            tilePrefabs = new Dictionary<RoadConnections, GameObject>();

            foreach (var entry in tiles)
            {
                if (entry.prefab != null)
                {
                    tilePrefabs[entry.connections] = entry.prefab;
                }
            }
        }

        public GameObject GetRandomTilePrefab()
        {
            var keys = new List<RoadConnections>(tilePrefabs.Keys);
            if (keys.Count == 0) return null;
            return tilePrefabs[keys[Random.Range(0, keys.Count)]];
        }
    }
}
