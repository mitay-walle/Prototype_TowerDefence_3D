using System.Collections.Generic;
using UnityEngine;

namespace TD.Levels
{
	public class TileMapManager : MonoBehaviour
	{
		[SerializeField] private Transform tilesParent;
		[SerializeField] private float tileSize = 5f;

		private TilePlacementValidator validator = new TilePlacementValidator();
		private Dictionary<Vector2Int, GameObject> placedTiles = new Dictionary<Vector2Int, GameObject>();
		private Vector3 basePosition;
		private List<Vector3> spawnPositions = new();

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

			var baseTilePrefab = TileDatabase.Instance?.GetRandomTilePrefab();
			if (baseTilePrefab == null)
			{
				if (Logs) Debug.LogError("[TileMapManager] TileDatabase not available, using default base tile");
				var baseTileDef = new RoadTileDef
				{
					position = Vector2Int.zero,
					rotation = 0,
					name = "Base",
					connections = RoadConnections.North | RoadConnections.South | RoadConnections.East | RoadConnections.West
				};

				validator.AddBaseTile(Vector2Int.zero, baseTileDef);
			}
			else
			{
				var baseTileDef = new RoadTileDef
				{
					position = Vector2Int.zero,
					rotation = 0,
					name = "Base",
					connections = baseTilePrefab.GetConnections()
				};

				validator.AddBaseTile(Vector2Int.zero, baseTileDef);
			}

			spawnPositions.Clear();
			spawnPositions.Add(new Vector3(0, 0, -10));
			spawnPositions.Add(new Vector3(10, 0, 0));
			spawnPositions.Add(new Vector3(0, 0, 10));
			spawnPositions.Add(new Vector3(-10, 0, 0));

			if (Logs) Debug.Log($"[TileMapManager] Base initialized at {basePosition}, spawners: {spawnPositions.Count}");
		}

public void PlaceTile(Vector2Int gridPosition, RoadTileDef tileDef, int rotation, GameObject prefab)
	{
		if (!PlaceTileLogic(gridPosition, tileDef, rotation))
			return;

		PlaceTilePrefab(gridPosition, tileDef, rotation, prefab);
	}

	public bool PlaceTileLogic(Vector2Int gridPosition, RoadTileDef tileDef, int rotation)
	{
		var result = validator.CanPlace(gridPosition, tileDef, rotation);

		if (!result.isValid)
		{
			if (Logs) Debug.LogWarning($"[TileMapManager] Cannot place tile: {result.reason}");
			return false;
		}

		tileDef.position = gridPosition;
		tileDef.rotation = rotation;

		validator.PlaceTile(gridPosition, tileDef, rotation);

		if (Logs) Debug.Log($"[TileMapManager] Tile logic placed at {gridPosition}");
		return true;
	}

	private void PlaceTilePrefab(Vector2Int gridPosition, RoadTileDef tileDef, int rotation, GameObject prefab)
	{
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

		if (Logs) Debug.Log($"[TileMapManager] Tile prefab instantiated at {gridPosition}");
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
			var spawnPointsSet = new System.Collections.Generic.HashSet<Vector3>();

			foreach (var kvp in allTiles)
			{
				var position = kvp.Key;
				var tileDef = kvp.Value;

				if (tileDef.name == null || position == Vector2Int.zero) continue;

				int rotation = validator.GetTileRotation(position);
				var connections = tileDef.GetRotatedConnections(rotation);

				bool hasOpenEdge = false;

				if (connections.HasConnection(RoadSide.North) && !tilesSet.Contains(position + Vector2Int.up))
					hasOpenEdge = true;

				if (connections.HasConnection(RoadSide.South) && !tilesSet.Contains(position + Vector2Int.down))
					hasOpenEdge = true;

				if (connections.HasConnection(RoadSide.East) && !tilesSet.Contains(position + Vector2Int.right))
					hasOpenEdge = true;

				if (connections.HasConnection(RoadSide.West) && !tilesSet.Contains(position + Vector2Int.left))
					hasOpenEdge = true;

				if (hasOpenEdge)
				{
					float worldX = position.x * tileSize;
					float worldZ = position.y * tileSize;
					var spawnPos = new Vector3(worldX, 0, worldZ);

					if (spawnPointsSet.Add(spawnPos))
					{
						spawnPositions.Add(spawnPos);
					}
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

		public RoadTileDef? GetTile(Vector2Int gridPosition)
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