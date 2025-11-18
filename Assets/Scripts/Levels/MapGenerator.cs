using System.Collections.Generic;
using UnityEngine;

namespace TD.Levels
{
	public class MapGenerator
	{
		private TilePlacementValidator validator;
		private int tilesToGenerate;
		private bool logs;

		public MapGenerator(int tilesToGenerate = 10, bool logs = true)
		{
			this.tilesToGenerate = tilesToGenerate;
			this.logs = logs;
			validator = new TilePlacementValidator();
			InitializeBaseTile();
		}

		private void InitializeBaseTile()
		{
			var baseTileDef = new RoadTileDef
			{
				position = Vector2Int.zero,
				rotation = 0,
				name = "Base",
				connections = RoadConnections.North | RoadConnections.South | RoadConnections.East | RoadConnections.West
			};

			validator.AddBaseTile(Vector2Int.zero, baseTileDef);
		}

		public Dictionary<Vector2Int, RoadTileDef> GenerateMap(List<RoadConnections> tileComponents)
		{
			if (logs) Debug.Log("[MapGenerator] === TILE-BASED MAP GENERATION STARTED ===");

			int placedCount = 0;
			var openEdges = new Queue<Vector2Int>();
			openEdges.Enqueue(new Vector2Int(0, -1));
			openEdges.Enqueue(new Vector2Int(0, 1));
			openEdges.Enqueue(new Vector2Int(1, 0));
			openEdges.Enqueue(new Vector2Int(-1, 0));

			var processedEdges = new HashSet<Vector2Int>();

			while (openEdges.Count > 0 && placedCount < tilesToGenerate)
			{
				var nextPos = openEdges.Dequeue();

				if (processedEdges.Contains(nextPos))
					continue;

				processedEdges.Add(nextPos);

				if (validator.GetTile(nextPos) != null)
					continue;

				var randomTileComponent = tileComponents[Random.Range(0, tileComponents.Count)];
				var tileConnections = randomTileComponent;

				var tileDef = new RoadTileDef
				{
					connections = tileConnections
				};

				for (int rotation = 0; rotation < 4; rotation++)
				{
					var canPlaceResult = validator.CanPlace(nextPos, tileDef, rotation);
					if (!canPlaceResult.isValid)
						continue;

					tileDef.position = nextPos;
					tileDef.rotation = rotation;
					validator.PlaceTile(nextPos, tileDef, rotation);
					placedCount++;

					if (logs) Debug.Log($"[MapGenerator] Placed {tileDef.name} at {nextPos} with rotation {rotation}");

					var newConnections = tileDef.GetRotatedConnections(rotation);
					if (newConnections.HasConnection(RoadSide.North))
						openEdges.Enqueue(nextPos + Vector2Int.up);

					if (newConnections.HasConnection(RoadSide.South))
						openEdges.Enqueue(nextPos + Vector2Int.down);

					if (newConnections.HasConnection(RoadSide.East))
						openEdges.Enqueue(nextPos + Vector2Int.right);

					if (newConnections.HasConnection(RoadSide.West))
						openEdges.Enqueue(nextPos + Vector2Int.left);

					break;
				}
			}

			if (logs) Debug.Log($"[MapGenerator] Road network created: {placedCount} tiles placed");

			return new Dictionary<Vector2Int, RoadTileDef>(validator.GetAllTiles());
		}

		public IReadOnlyDictionary<Vector2Int, RoadTileDef> GetCurrentMap()
		{
			return validator.GetAllTiles();
		}

		public void Clear()
		{
			validator = new TilePlacementValidator();
			InitializeBaseTile();
		}
	}
}