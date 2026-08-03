using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TD.Levels
{
	public class TileMapManager : MonoBehaviour
	{
		[SerializeField] private Transform tilesParent;
		[SerializeField] private float tileSize = 5f;
		[SerializeField] private bool Logs;

		private TilePlacementValidator validator = new TilePlacementValidator();
		[ShowInInspector] private Dictionary<Vector2Int, GameObject> placedTiles = new Dictionary<Vector2Int, GameObject>();
		private Vector3 basePosition;
		private List<Vector3> spawnPositions = new();

		public Vector3 BasePosition => basePosition;
		public List<Vector3> SpawnPositions => spawnPositions;

		public Vector2Int WorldToGrid(Vector3 worldPosition)
		{
			return new Vector2Int(
				Mathf.RoundToInt(worldPosition.x / tileSize),
				Mathf.RoundToInt(worldPosition.z / tileSize));
		}

		public Vector3 GridToWorld(Vector2Int gridPosition)
		{
			return new Vector3(gridPosition.x * tileSize, 0f, gridPosition.y * tileSize);
		}

		public bool TryGetGridPoint(Ray ray, out Vector3 worldPoint)
		{
			var gridPlane = new Plane(Vector3.up, GridToWorld(Vector2Int.zero));
			if (!gridPlane.Raycast(ray, out float distance) || distance < 0f)
			{
				worldPoint = default;
				return false;
			}

			worldPoint = ray.GetPoint(distance);
			return true;
		}

		public bool TryGetTileSurfacePoint(Ray ray, out Vector3 worldPoint, out Vector2Int gridPosition)
		{
			worldPoint = default;
			gridPosition = default;
			if (placedTiles == null || placedTiles.Count == 0)
				return false;

			var hits = Physics.RaycastAll(ray, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
			var nearestDistance = float.MaxValue;
			var found = false;
			for (var hitIndex = 0; hitIndex < hits.Length; hitIndex++)
			{
				var tileComponent = hits[hitIndex].collider.GetComponentInParent<RoadTileComponent>();
				if (tileComponent == null || hits[hitIndex].distance >= nearestDistance)
					continue;

				foreach (var placedTile in placedTiles)
				{
					if (placedTile.Value != tileComponent.gameObject)
						continue;

					nearestDistance = hits[hitIndex].distance;
					worldPoint = hits[hitIndex].point;
					gridPosition = placedTile.Key;
					found = true;
					break;
				}
			}

			return found;
		}

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
			spawnPositions.Clear();

			if (Logs) Debug.Log($"[TileMapManager] Base position initialized at {basePosition}");
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

			tileInstance.transform.position = GridToWorld(gridPosition);
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
			DestroyImmediate(tileGo);
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
					var spawnPos = GridToWorld(position);

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

		public List<TilePlacementChoice> BuildPlacementChoices(IReadOnlyList<RoadTileComponent> tilePrefabs, int minimumOptions)
		{
			var choices = new List<TilePlacementChoice>();
			if (tilePrefabs == null || tilePrefabs.Count == 0 || minimumOptions <= 0)
				return choices;

			var openRoadEndsBefore = GetOpenRoadEnds();
			var candidatePositions = new HashSet<Vector2Int>(openRoadEndsBefore);
			var signatures = new HashSet<string>();

			foreach (var gridPosition in candidatePositions)
			{
				foreach (var tilePrefab in tilePrefabs)
				{
					if (tilePrefab == null)
						continue;

					var tileDefinition = new RoadTileDef
					{
						position = gridPosition,
						connections = tilePrefab.GetConnections(),
						name = tilePrefab.name
					};

					for (var rotation = 0; rotation < 4; rotation++)
					{
						if (!CanPlaceTile(gridPosition, tileDefinition, rotation))
							continue;

						var rotatedConnections = tileDefinition.GetRotatedConnections(rotation);
						var signature = $"{gridPosition.x}:{gridPosition.y}:{tilePrefab.name}:{(int)rotatedConnections}";
						if (!signatures.Add(signature))
							continue;

						var openRoadEndsAfter = GetOpenRoadEndsAfter(tileDefinition, gridPosition, rotation);
						var affectedOpenRoadEnds = GetAffectedOpenRoadEnds(openRoadEndsBefore, openRoadEndsAfter);
						choices.Add(new TilePlacementChoice(
							true,
							string.Empty,
							tileDefinition,
							tilePrefab,
							gridPosition,
							rotation,
							rotatedConnections,
							CountConnectedNeighbors(gridPosition, rotatedConnections),
							openRoadEndsBefore,
							openRoadEndsAfter,
							affectedOpenRoadEnds));

						if (choices.Count >= minimumOptions)
							return choices;
					}
				}
			}

			return choices;
		}

		public List<Vector2Int> GetOpenRoadEnds()
		{
			var tiles = validator.GetAllTiles();
			var rotations = new Dictionary<Vector2Int, int>();
			foreach (var tile in tiles)
				rotations[tile.Key] = validator.GetTileRotation(tile.Key);

			return CollectOpenRoadEnds(tiles, rotations);
		}

		private List<Vector2Int> GetOpenRoadEndsAfter(RoadTileDef tileDefinition, Vector2Int gridPosition, int rotation)
		{
			var tiles = new Dictionary<Vector2Int, RoadTileDef>(validator.GetAllTiles());
			var rotations = new Dictionary<Vector2Int, int>();
			foreach (var tile in tiles)
				rotations[tile.Key] = validator.GetTileRotation(tile.Key);

			tiles[gridPosition] = tileDefinition;
			rotations[gridPosition] = rotation;
			return CollectOpenRoadEnds(tiles, rotations);
		}

		private List<Vector2Int> CollectOpenRoadEnds(
			IReadOnlyDictionary<Vector2Int, RoadTileDef> tiles,
			IReadOnlyDictionary<Vector2Int, int> rotations)
		{
			var openRoadEnds = new List<Vector2Int>();
			foreach (var tile in tiles)
			{
				var rotation = rotations.TryGetValue(tile.Key, out var storedRotation) ? storedRotation : 0;
				var connections = tile.Value.GetRotatedConnections(rotation);
				for (var sideIndex = 0; sideIndex < 4; sideIndex++)
				{
					var side = (RoadSide)sideIndex;
					var neighborPosition = tile.Key + GetOffset(side);
					if (connections.HasConnection(side) && !tiles.ContainsKey(neighborPosition))
						openRoadEnds.Add(neighborPosition);
				}
			}

			return openRoadEnds;
		}

		private List<Vector2Int> GetAffectedOpenRoadEnds(
			IReadOnlyList<Vector2Int> before,
			IReadOnlyList<Vector2Int> after)
		{
			var beforeSet = new HashSet<Vector2Int>(before);
			var afterSet = new HashSet<Vector2Int>(after);
			var affected = new List<Vector2Int>();

			foreach (var position in beforeSet)
			{
				if (!afterSet.Contains(position))
					affected.Add(position);
			}

			foreach (var position in afterSet)
			{
				if (!beforeSet.Contains(position))
					affected.Add(position);
			}

			return affected;
		}

		private int CountConnectedNeighbors(Vector2Int gridPosition, RoadConnections connections)
		{
			var connectedNeighbors = 0;
			var tiles = validator.GetAllTiles();
			for (var sideIndex = 0; sideIndex < 4; sideIndex++)
			{
				var side = (RoadSide)sideIndex;
				var neighborPosition = gridPosition + GetOffset(side);
				if (!tiles.TryGetValue(neighborPosition, out var neighborTile))
					continue;

				var neighborRotation = validator.GetTileRotation(neighborPosition);
				var oppositeSide = RoadConnectionsExtensions.GetOppositeSide(side);
				if (connections.HasConnection(side) && neighborTile.GetRotatedConnections(neighborRotation).HasConnection(oppositeSide))
					connectedNeighbors++;
			}

			return connectedNeighbors;
		}

		private Vector2Int GetOffset(RoadSide side)
		{
			return side switch
			{
				RoadSide.North => Vector2Int.up,
				RoadSide.South => Vector2Int.down,
				RoadSide.East => Vector2Int.right,
				RoadSide.West => Vector2Int.left,
				_ => Vector2Int.zero
			};
		}
	}
}
