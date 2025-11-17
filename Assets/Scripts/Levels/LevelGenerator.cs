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

            var straightH = Resources.Load<RoadTileDef>("TileDefs/Straight_H");
            var straightV = Resources.Load<RoadTileDef>("TileDefs/Straight_V");

            if (straightH == null || straightV == null)
            {
                if (Logs) Debug.LogWarning("[LevelGenerator] Could not load tile definitions from Resources/TileDefs");
                return;
            }

            var straightHPrefab = LoadTilePrefab("Straight_H");
            var straightVPrefab = LoadTilePrefab("Straight_V");

            if (straightHPrefab == null || straightVPrefab == null)
            {
                if (Logs) Debug.LogWarning("[LevelGenerator] Could not load tile prefabs");
                return;
            }

            int tileCount = 0;

            for (int x = -initialTileRadius; x <= initialTileRadius; x++)
            {
                for (int z = -initialTileRadius; z <= initialTileRadius; z++)
                {
                    if (x == 0 && z == 0) continue;

                    Vector2Int pos = new Vector2Int(x, z);
                    RoadTileDef tileDef;
                    GameObject tilePrefab;

                    if (x == 0)
                    {
                        tileDef = straightV;
                        tilePrefab = straightVPrefab;
                    }
                    else if (z == 0)
                    {
                        tileDef = straightH;
                        tilePrefab = straightHPrefab;
                    }
                    else
                    {
                        continue;
                    }

                    if (!tileMapManager.CanPlaceTile(pos, tileDef, 0))
                    {
                        if (Logs) Debug.Log($"[LevelGenerator] Skipped tile at {pos}");
                        continue;
                    }

                    tileMapManager.PlaceTile(pos, tileDef, 0, tilePrefab);
                    tileCount++;

                    if (Logs) Debug.Log($"[LevelGenerator] Placed {tileDef.name} at {pos}");
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
