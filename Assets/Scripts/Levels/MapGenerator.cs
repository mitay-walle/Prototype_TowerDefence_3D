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
			var openRoadEnds = new Queue<(Vector2Int position, RoadSide requiredConnection)>();

			openRoadEnds.Enqueue((new Vector2Int(0, -1), RoadSide.North));
			openRoadEnds.Enqueue((new Vector2Int(0, 1), RoadSide.South));
			openRoadEnds.Enqueue((new Vector2Int(1, 0), RoadSide.West));
			openRoadEnds.Enqueue((new Vector2Int(-1, 0), RoadSide.East));

			var processedPositions = new HashSet<Vector2Int>();

			while (openRoadEnds.Count > 0 && placedCount < tilesToGenerate)
			{
				var (nextPos, requiredSide) = openRoadEnds.Dequeue();

				if (processedPositions.Contains(nextPos))
					continue;

				processedPositions.Add(nextPos);

				if (validator.GetTile(nextPos) != null)
					continue;

				var shuffledTiles = new List<RoadConnections>(tileComponents);
				ShuffleList(shuffledTiles);

				bool placed = false;

				foreach (var tileKind in shuffledTiles)
				{
					var tileDef = new RoadTileDef
					{
						name = tileKind.ToString(),
						connections = tileKind
					};

					for (int rotation = 0; rotation < 4; rotation++)
					{
						var rotatedConnections = tileDef.GetRotatedConnections(rotation);

						if (!rotatedConnections.HasConnection(requiredSide))
							continue;

						var canPlaceResult = validator.CanPlace(nextPos, tileDef, rotation);
						if (!canPlaceResult.isValid)
							continue;

						tileDef.position = nextPos;
						tileDef.rotation = rotation;
						validator.PlaceTile(nextPos, tileDef, rotation);
						placedCount++;
						placed = true;

						if (logs) Debug.Log($"[MapGenerator] Placed {tileDef.name} at {nextPos} with rotation {rotation}");

						if (rotatedConnections.HasConnection(RoadSide.North))
							openRoadEnds.Enqueue((nextPos + Vector2Int.up, RoadSide.South));

						if (rotatedConnections.HasConnection(RoadSide.South))
							openRoadEnds.Enqueue((nextPos + Vector2Int.down, RoadSide.North));

						if (rotatedConnections.HasConnection(RoadSide.East))
							openRoadEnds.Enqueue((nextPos + Vector2Int.right, RoadSide.West));

						if (rotatedConnections.HasConnection(RoadSide.West))
							openRoadEnds.Enqueue((nextPos + Vector2Int.left, RoadSide.East));

						break;
					}

					if (placed)
						break;
				}

				if (!placed && logs)
				{
					Debug.LogWarning($"[MapGenerator] Failed to place any tile at {nextPos} requiring connection on {requiredSide}");
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

		private void ShuffleList<T>(List<T> list)
		{
			for (int i = list.Count - 1; i > 0; i--)
			{
				int randomIndex = Random.Range(0, i + 1);
				T temp = list[i];
				list[i] = list[randomIndex];
				list[randomIndex] = temp;
			}
		}
	}
}
