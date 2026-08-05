using System.Collections.Generic;
using UnityEngine;

namespace TD.Levels
{
	public class MapGenerator
	{
		private static readonly System.Random seedRandom = new System.Random();
		private TilePlacementValidator validator;
		private int tilesToGenerate;
		private bool logs;
		private readonly System.Random random;
		private const int BranchTileWeight = 1;
		private const int StraightOrCornerTileWeight = 4;
		private const int CompactLayoutTileCount = 4;
		private const int MinimumStartingDirectionCount = 2;
		private const int MaximumStartingDirectionCount = 4;

		public int Seed { get; }
		public int StartingTileDifficulty { get; }
		public int StartingTileDirectionCount => GetStartingDirectionCount(StartingTileDifficulty);

		public MapGenerator(int tilesToGenerate = 10, bool logs = true, int seed = 0, int difficulty = MaximumStartingDirectionCount)
		{
			this.tilesToGenerate = tilesToGenerate;
			this.logs = logs;
			var resolvedSeed = seed == 0 ? CreateRandomSeed() : seed;
			Seed = resolvedSeed;
			StartingTileDifficulty = GetStartingDirectionCount(difficulty);
			random = new System.Random(resolvedSeed);
			validator = new TilePlacementValidator();
			InitializeBaseTile();
		}

		public static int GetStartingDirectionCount(int difficulty)
		{
			return Mathf.Clamp(difficulty, MinimumStartingDirectionCount, MaximumStartingDirectionCount);
		}

		private void InitializeBaseTile()
		{
			var connections = BuildStartingConnections(StartingTileDirectionCount);
			var baseTileDef = new RoadTileDef
			{
				position = Vector2Int.zero,
				rotation = 0,
				name = "Base",
				connections = connections
			};

			validator.AddBaseTile(Vector2Int.zero, baseTileDef);
			if (logs)
				Debug.Log($"[MapGenerator] Base tile difficulty={StartingTileDifficulty};directions={connections.GetConnectionCount()};connections={connections}");
		}

		public Dictionary<Vector2Int, RoadTileDef> GenerateMap(List<RoadConnections> tileComponents)
		{
			if (logs) Debug.Log("[MapGenerator] === TILE-BASED MAP GENERATION STARTED ===");
			if (logs && tilesToGenerate <= CompactLayoutTileCount)
				Debug.Log($"[MapGenerator] Compact layout excludes branch tiles; target open entrances <= {CompactLayoutTileCount}");

			int placedCount = 0;
			var openRoadEnds = new Queue<(Vector2Int position, RoadSide requiredConnection)>();

			var baseConnections = validator.GetTile(Vector2Int.zero).Value.GetRotatedConnections(0);
			var directions = new (RoadSide side, Vector2Int position, RoadSide requiredSide)[]
			{
				(RoadSide.North, new Vector2Int(0, 1), RoadSide.South),
				(RoadSide.South, new Vector2Int(0, -1), RoadSide.North),
				(RoadSide.East, new Vector2Int(1, 0), RoadSide.West),
				(RoadSide.West, new Vector2Int(-1, 0), RoadSide.East)
			};
			foreach (var direction in directions)
			{
				if (baseConnections.HasConnection(direction.side))
					openRoadEnds.Enqueue((direction.position, direction.requiredSide));
			}

			var processedPositions = new HashSet<Vector2Int>();

			while (openRoadEnds.Count > 0 && placedCount < tilesToGenerate)
			{
				var (nextPos, requiredSide) = openRoadEnds.Dequeue();

				if (processedPositions.Contains(nextPos))
					continue;

				if (validator.GetTile(nextPos) != null)
				{
					processedPositions.Add(nextPos);
					continue;
				}

				var shuffledTiles = BuildWeightedTileOrder(tileComponents);

				bool placed = false;

				foreach (var tileKind in shuffledTiles)
				{
					if (tilesToGenerate <= CompactLayoutTileCount && tileKind.GetConnectionCount() >= 3)
						continue;

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

				processedPositions.Add(nextPos);
			}

			if (logs) Debug.Log($"[MapGenerator] Road network created: {placedCount} tiles placed");

			return new Dictionary<Vector2Int, RoadTileDef>(validator.GetAllTiles());
		}


		public IReadOnlyDictionary<Vector2Int, RoadTileDef> GetCurrentMap()
		{
			return validator.GetAllTiles();
		}

		public bool ValidateTopology(out string reason)
		{
			if (!validator.ValidateTopology(Vector2Int.zero, out reason))
				return false;

			return validator.ValidateRoutesToOpenEnds(Vector2Int.zero, out reason);
		}

		public void Clear()
		{
			validator = new TilePlacementValidator();
			InitializeBaseTile();
		}

		private RoadConnections BuildStartingConnections(int directionCount)
		{
			var sides = new List<RoadSide>
			{
				RoadSide.North,
				RoadSide.South,
				RoadSide.East,
				RoadSide.West
			};
			ShuffleList(sides);

			var connections = RoadConnections.None;
			for (var i = 0; i < Mathf.Min(directionCount, sides.Count); i++)
				connections |= (RoadConnections)(1 << (int)sides[i]);

			return connections;
		}

		public static int CreateRandomSeed()
		{
			lock (seedRandom)
			{
				return seedRandom.Next(1, int.MaxValue);
			}
		}

		private void ShuffleList<T>(List<T> list)
		{
			for (int i = list.Count - 1; i > 0; i--)
			{
				int randomIndex = random.Next(i + 1);
				T temp = list[i];
				list[i] = list[randomIndex];
				list[randomIndex] = temp;
			}
		}

		private List<RoadConnections> BuildWeightedTileOrder(List<RoadConnections> tileComponents)
		{
			var weighted = new List<RoadConnections>();
			foreach (var tileKind in tileComponents)
			{
				var connectionCount = tileKind.GetConnectionCount();
				var weight = connectionCount >= 3 ? BranchTileWeight : StraightOrCornerTileWeight;
				for (var i = 0; i < weight; i++)
					weighted.Add(tileKind);
			}

			ShuffleList(weighted);
			var ordered = new List<RoadConnections>();
			foreach (var tileKind in weighted)
			{
				if (!ordered.Contains(tileKind))
					ordered.Add(tileKind);
			}

			return ordered;
		}
	}
}
