using UnityEngine;
using Sirenix.OdinInspector;

namespace TD.Levels
{
    public class LevelGenerator : MonoBehaviour
    {
        [SerializeField] private TileMapManager tileMapManager;
        [SerializeField] private int initialTileRadius = 1;

        private bool Logs = true;

        private void OnEnable()
        {
            if (tileMapManager == null)
                tileMapManager = GetComponent<TileMapManager>();
        }

        public void GenerateLevel()
        {
            if (tileMapManager == null)
            {
                Debug.LogError("[LevelGenerator] TileMapManager not found!");
                return;
            }

            if (Logs) Debug.Log("[LevelGenerator] === TILE-BASED LEVEL GENERATION STARTED ===");

            GenerateInitialTiles();
            ValidateLevel();

            if (Logs) Debug.Log("[LevelGenerator] === TILE-BASED LEVEL GENERATION COMPLETE ===");
        }

        private void GenerateInitialTiles()
        {
            if (Logs) Debug.Log($"[LevelGenerator] Generating initial road network (radius: {initialTileRadius})");

            var straight = Resources.Load<RoadTileDef>("TileDefs/Straight");
            var turn = Resources.Load<RoadTileDef>("TileDefs/Turn");
            var cross3 = Resources.Load<RoadTileDef>("TileDefs/Cross_3");

            var straightPrefab = LoadTilePrefab("Straight");
            var turnPrefab = LoadTilePrefab("Turn");
            var cross3Prefab = LoadTilePrefab("Cross_3");

            if (straight == null || turn == null || cross3 == null ||
                straightPrefab == null || turnPrefab == null || cross3Prefab == null)
            {
                if (Logs) Debug.LogWarning("[LevelGenerator] Could not load tile definitions or prefabs");
                return;
            }

            int tileCount = 0;

            for (int x = -initialTileRadius; x <= initialTileRadius; x++)
            {
                for (int z = -initialTileRadius; z <= initialTileRadius; z++)
                {
                    if (x == 0 && z == 0) continue;
                    if (x != 0 && z != 0) continue;

                    Vector2Int pos = new Vector2Int(x, z);
                    RoadTileDef tileDef;
                    GameObject tilePrefab;
                    int rotation;

                    if (x > 0 && z == 0)
                    {
                        tileDef = cross3;
                        tilePrefab = cross3Prefab;
                        rotation = 3;
                    }
                    else if (x < 0 && z == 0)
                    {
                        tileDef = cross3;
                        tilePrefab = cross3Prefab;
                        rotation = 1;
                    }
                    else if (x == 0 && z > 0)
                    {
                        tileDef = cross3;
                        tilePrefab = cross3Prefab;
                        rotation = 2;
                    }
                    else
                    {
                        tileDef = cross3;
                        tilePrefab = cross3Prefab;
                        rotation = 0;
                    }

                    if (!tileMapManager.CanPlaceTile(pos, tileDef, rotation))
                    {
                        if (Logs) Debug.Log($"[LevelGenerator] Skipped tile at {pos}");
                        continue;
                    }

                    tileMapManager.PlaceTile(pos, tileDef, rotation, tilePrefab);
                    tileCount++;

                    if (Logs) Debug.Log($"[LevelGenerator] Placed {tileDef.name} at {pos} with rotation {rotation}");
                }
            }

            if (Logs) Debug.Log($"[LevelGenerator] Initial road network created: {tileCount} tiles");
        }

        private void ValidateLevel()
        {
            if (Logs) Debug.Log("[LevelGenerator] Validating level...");

            var allTiles = tileMapManager.GetAllTiles();
            var spawnPositions = tileMapManager.SpawnPositions;

            if (Logs) Debug.Log($"[LevelGenerator] Level validation complete:");
            if (Logs) Debug.Log($"  - Tiles placed: {allTiles.Count}");
            if (Logs) Debug.Log($"  - Base position: {tileMapManager.BasePosition}");
            if (Logs) Debug.Log($"  - Spawn positions: {spawnPositions.Count}");

            foreach (var spawn in spawnPositions)
            {
                if (Logs) Debug.Log($"    • {spawn}");
            }
        }

        [Button("Generate Level")]
        public void GenerateLevelButton()
        {
            GenerateLevel();
        }

        [Button("Clear Level")]
        public void ClearLevel()
        {
            if (tileMapManager == null)
            {
                Debug.LogError("[LevelGenerator] TileMapManager not found!");
                return;
            }

            var allTiles = tileMapManager.GetAllTiles();
            var tilePositions = new System.Collections.Generic.List<Vector2Int>(allTiles.Keys);

            foreach (var pos in tilePositions)
            {
                if (pos != Vector2Int.zero)
                {
                    tileMapManager.RemoveTile(pos);
                }
            }

            if (Logs) Debug.Log("[LevelGenerator] Level cleared - base tile remains");
        }

        [Button("Reload Level")]
        public void ReloadLevel()
        {
            ClearLevel();
            GenerateLevel();
        }

        public TileMapManager GetTileMapManager() => tileMapManager;

        private GameObject LoadTilePrefab(string name)
        {
            var prefab = Resources.Load<GameObject>($"Prefabs/Tiles/{name}");
            if (prefab != null)
                return prefab;

            #if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Tiles/{name}.prefab");
            if (prefab != null)
                return prefab;
            #endif

            return null;
        }
    }
}
