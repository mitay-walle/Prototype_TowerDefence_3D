using System.Collections.Generic;
using UnityEngine;

namespace TD.Levels
{
    public class TileMapManager : MonoBehaviour
    {
        [SerializeField] private Transform tilesParent;
        [SerializeField] private float tileSize = 5f;

        private TilePlacementValidator validator;
        private Dictionary<Vector2Int, GameObject> placedTiles;
        private Vector3 basePosition;
        private List<Vector3> spawnPositions;

        public Vector3 BasePosition => basePosition;
        public List<Vector3> SpawnPositions => spawnPositions;

        private void Awake()
        {
            if (tilesParent == null)
                tilesParent = transform;

            validator = new TilePlacementValidator();
            placedTiles = new Dictionary<Vector2Int, GameObject>();
            spawnPositions = new List<Vector3>();

            InitializeBaseTile();
        }

        private void InitializeBaseTile()
        {
            basePosition = Vector3.zero;

            var baseTileDef = new RoadTileDef
            {
                name = "Base",
                connections = RoadConnections.North | RoadConnections.South | RoadConnections.East | RoadConnections.West
            };

            validator.AddBaseTile(Vector2Int.zero, baseTileDef);

            spawnPositions.Clear();
            spawnPositions.Add(new Vector3(0, 0, -10));
            spawnPositions.Add(new Vector3(10, 0, 0));
            spawnPositions.Add(new Vector3(0, 0, 10));
            spawnPositions.Add(new Vector3(-10, 0, 0));

            if (Logs) Debug.Log($"[TileMapManager] Base initialized at {basePosition}, spawners: {spawnPositions.Count}");
        }

        public void PlaceTile(Vector2Int gridPosition, RoadTileDef tileDef, int rotation, GameObject prefab)
        {
            var result = validator.CanPlace(gridPosition, tileDef, rotation);

            if (!result.isValid)
            {
                if (Logs) Debug.LogWarning($"[TileMapManager] Cannot place tile: {result.reason}");
                return;
            }

            validator.PlaceTile(gridPosition, tileDef, rotation);

            GameObject tileInstance = Instantiate(prefab, tilesParent);
            tileInstance.name = $"Tile_{gridPosition.x}_{gridPosition.y}";

            var roadTileComponent = tileInstance.GetComponent<RoadTileComponent>();
            if (roadTileComponent != null)
            {
                roadTileComponent.Initialize(tileDef.GetRotatedConnections(rotation));
            }

            tileInstance.transform.position = new Vector3(gridPosition.x * tileSize, 0, gridPosition.y * tileSize);
            tileInstance.transform.rotation = Quaternion.Euler(0, rotation * 90, 0);

            placedTiles[gridPosition] = tileInstance;

            UpdateSpawnerPositions();

            if (Logs) Debug.Log($"[TileMapManager] Tile placed at {gridPosition}");
        }

        public void RemoveTile(Vector2Int gridPosition)
        {
            if (!placedTiles.TryGetValue(gridPosition, out var tileGo))
                return;

            validator.RemoveTile(gridPosition);
            Destroy(tileGo);
            placedTiles.Remove(gridPosition);

            UpdateSpawnerPositions();

            if (Logs) Debug.Log($"[TileMapManager] Tile removed from {gridPosition}");
        }

        private void UpdateSpawnerPositions()
        {
            spawnPositions.Clear();

            var allTiles = validator.GetAllTiles();
            var tilesSet = new System.Collections.Generic.HashSet<Vector2Int>(allTiles.Keys);

            foreach (var kvp in allTiles)
            {
                var position = kvp.Key;
                var tileDef = kvp.Value;

                if (tileDef == null || position == Vector2Int.zero) continue;

                int rotation = validator.GetTileRotation(position);
                var connections = tileDef.GetRotatedConnections(rotation);

                float worldX = position.x * tileSize;
                float worldZ = position.y * tileSize;

                if (connections.HasConnection(RoadSide.North))
                {
                    var neighborPos = position + Vector2Int.up;
                    if (!tilesSet.Contains(neighborPos))
                        spawnPositions.Add(new Vector3(worldX, 0, worldZ - tileSize));
                }

                if (connections.HasConnection(RoadSide.South))
                {
                    var neighborPos = position + Vector2Int.down;
                    if (!tilesSet.Contains(neighborPos))
                        spawnPositions.Add(new Vector3(worldX, 0, worldZ + tileSize));
                }

                if (connections.HasConnection(RoadSide.East))
                {
                    var neighborPos = position + Vector2Int.right;
                    if (!tilesSet.Contains(neighborPos))
                        spawnPositions.Add(new Vector3(worldX + tileSize, 0, worldZ));
                }

                if (connections.HasConnection(RoadSide.West))
                {
                    var neighborPos = position + Vector2Int.left;
                    if (!tilesSet.Contains(neighborPos))
                        spawnPositions.Add(new Vector3(worldX - tileSize, 0, worldZ));
                }
            }

            if (spawnPositions.Count == 0)
            {
                if (Logs) Debug.LogWarning("[TileMapManager] No dead-end spawn points found!");
            }

            if (Logs) Debug.Log($"[TileMapManager] Updated spawner positions: {spawnPositions.Count} dead-end points");
        }

        public bool CanPlaceTile(Vector2Int gridPosition, RoadTileDef tileDef, int rotation)
        {
            var result = validator.CanPlace(gridPosition, tileDef, rotation);
            return result.isValid;
        }

        public RoadTileDef GetTile(Vector2Int gridPosition)
        {
            return validator.GetTile(gridPosition);
        }

        public IReadOnlyDictionary<Vector2Int, RoadTileDef> GetAllTiles()
        {
            return validator.GetAllTiles();
        }

        private bool Logs = true;
    }
}
