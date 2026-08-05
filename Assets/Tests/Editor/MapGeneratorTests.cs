using System;
using System.Collections.Generic;
using NUnit.Framework;
using TD.Levels;
using UnityEditor.SceneManagement;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TD.Tests
{
	public class MapGeneratorTests
	{
		[SetUp]
		public void Setup()
		{
			EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
		}

		[Test]
		public void TestMapGeneration_PureLogicNoGameObjects()
		{
			for (int i = 0; i < 10; i++)
			{
				Random.InitState(DateTime.Now.Millisecond);
				var mapGenerator = new MapGenerator(tilesToGenerate: 10, logs: true);

				var tileDatabase = TileDatabase.Instance;
				Assert.NotNull(tileDatabase, "TileDatabase not available");

				var tileComponents = tileDatabase.GetAllTileKinds();
				Assert.NotZero(tileComponents.Count, "No tile components available");

				var generatedMap = mapGenerator.GenerateMap(tileComponents);
				Assert.NotZero(generatedMap.Count, "Generated map should contain at least one tile");

				string map = MapVisualizer.LogMap(generatedMap);
				string[] lines = map.Split('\n');

				int count = 0;

				foreach (string line in lines)
				{
					foreach (char character in line)
					{
						if (character != ' ')
						{
							count++;
						}
					}
				}

				Assert.IsTrue(generatedMap.ContainsKey(Vector2Int.zero), "Should have base tile at origin");
				Assert.AreEqual(11, generatedMap.Count, "Should have generated 10 tiles + 1 base tile");
			}
		}

		[Test]
		public void TestMapGeneration_AllConnectionsValid()
		{
			for (int i = 0; i < 10; i++)
			{
				Random.InitState(DateTime.Now.Millisecond);

				var mapGenerator = new MapGenerator(tilesToGenerate: 20, logs: true);

				var tileDatabase = TileDatabase.Instance;
				var tileComponents = tileDatabase.GetAllTileKinds();
				var generatedMap = mapGenerator.GenerateMap(tileComponents);

				string map = MapVisualizer.LogMap(generatedMap);
				Debug.Log($"Generated map:\n{map}");

				int invalidConnections = 0;
				var errorMessages = new System.Text.StringBuilder();

				foreach (var kvp in generatedMap)
				{
					Vector2Int pos = kvp.Key;
					RoadTileDef tile = kvp.Value;
					var rotatedConnections = tile.GetRotatedConnections(tile.rotation);

					var directions = new (RoadSide side, Vector2Int offset, string name)[]
					{
						(RoadSide.North, Vector2Int.up, "North"),
						(RoadSide.South, Vector2Int.down, "South"),
						(RoadSide.East, Vector2Int.right, "East"),
						(RoadSide.West, Vector2Int.left, "West")
					};

					foreach (var (side, offset, name) in directions)
					{
						var neighborPos = pos + offset;
						bool hasConnection = rotatedConnections.HasConnection(side);

						if (generatedMap.TryGetValue(neighborPos, out var neighbor))
						{
							var neighborRotatedConnections = neighbor.GetRotatedConnections(neighbor.rotation);
							var oppositeSide = RoadConnectionsExtensions.GetOppositeSide(side);
							bool neighborHasConnection = neighborRotatedConnections.HasConnection(oppositeSide);

							if (hasConnection != neighborHasConnection)
							{
								invalidConnections++;
								errorMessages.AppendLine(
									$"INVALID at {pos} -> {name}: tile has road={hasConnection}, neighbor at {neighborPos} has road={neighborHasConnection}");

								errorMessages.AppendLine($"  Tile: {tile} (rotation={tile.rotation}, connections={tile.connections})");
								errorMessages.AppendLine(
									$"  Neighbor: {neighbor} (rotation={neighbor.rotation}, connections={neighbor.connections})");
							}
						}
					}
				}

				if (invalidConnections > 0)
				{
					Debug.LogError($"Found {invalidConnections} invalid connections:\n{errorMessages}");
				}

				Assert.AreEqual(0, invalidConnections, $"All connections should be valid. Found {invalidConnections} errors:\n{errorMessages}");
			}
		}

		[Test]
		public void TestMapGeneration_SeedControlsLayout()
		{
			var tileDatabase = TileDatabase.Instance;
			Assert.NotNull(tileDatabase, "TileDatabase not available");

			var tileComponents = tileDatabase.GetAllTileKinds();
			var firstMap = new MapGenerator(tilesToGenerate: 10, logs: false, seed: 12345).GenerateMap(tileComponents);
			var sameSeedMap = new MapGenerator(tilesToGenerate: 10, logs: false, seed: 12345).GenerateMap(tileComponents);
			var differentSeedMap = new MapGenerator(tilesToGenerate: 10, logs: false, seed: 12346).GenerateMap(tileComponents);

			Assert.AreEqual(BuildMapSignature(firstMap), BuildMapSignature(sameSeedMap));
			Assert.AreNotEqual(BuildMapSignature(firstMap), BuildMapSignature(differentSeedMap));
		}

		[Test]
		public void TestMapGeneration_AutoSeedChangesAcrossRapidGenerators()
		{
			var firstGenerator = new MapGenerator(tilesToGenerate: 10, logs: false);
			var secondGenerator = new MapGenerator(tilesToGenerate: 10, logs: false);

			Assert.AreNotEqual(firstGenerator.Seed, secondGenerator.Seed);
		}

		[Test]
		public void TestMapGeneration_TopologyStaysConnectedAcrossSeeds()
		{
			var tileDatabase = TileDatabase.Instance;
			Assert.NotNull(tileDatabase, "TileDatabase not available");
			var tileComponents = tileDatabase.GetAllTileKinds();
			var seeds = new[] { 1, 2, 3, 17, 42, 12345, 987654321 };

			foreach (var seed in seeds)
			{
				var generator = new MapGenerator(tilesToGenerate: 20, logs: false, seed: seed);
				generator.GenerateMap(tileComponents);

				Assert.IsTrue(generator.ValidateTopology(out var reason), $"Seed {seed}: {reason}");
			}
		}

		[Test]
		public void TestMapGeneration_BoundsOpenRoadEndsAcrossSeeds()
		{
			var tileDatabase = TileDatabase.Instance;
			Assert.NotNull(tileDatabase, "TileDatabase not available");
			var tileComponents = tileDatabase.GetAllTileKinds();

			foreach (var seed in new[] { 1, 2, 3, 17, 42, 12345, 987654321 })
			{
				var generator = new MapGenerator(tilesToGenerate: 4, logs: false, seed: seed);
				var generatedMap = generator.GenerateMap(tileComponents);

				Assert.LessOrEqual(generator.GetCurrentMap().Count, 5, $"Seed {seed} exceeded compact map size");
				Assert.LessOrEqual(CountOpenRoadEnds(generatedMap), 4, $"Seed {seed} exposed too many compact-map entrances");
				Assert.IsTrue(generator.ValidateTopology(out var reason), $"Seed {seed}: {reason}");
			}
		}

		[Test]
		public void TestMapGeneration_StartingTileDirectionsFollowDifficulty()
		{
			var tileDatabase = TileDatabase.Instance;
			Assert.NotNull(tileDatabase, "TileDatabase not available");
			var tileComponents = tileDatabase.GetAllTileKinds();

			for (var difficulty = 2; difficulty <= 4; difficulty++)
			{
				var generator = new MapGenerator(tilesToGenerate: 4, logs: false, seed: 100 + difficulty, difficulty: difficulty);
				var generatedMap = generator.GenerateMap(tileComponents);
				var baseTile = generatedMap[Vector2Int.zero];

				Assert.AreEqual(difficulty, baseTile.GetRotatedConnections(baseTile.rotation).GetConnectionCount());
			}
		}

		[Test]
		public void TestMapGeneration_StartingTileDirectionsClampToValidRoadRange()
		{
			Assert.AreEqual(2, MapGenerator.GetStartingDirectionCount(0));
			Assert.AreEqual(4, MapGenerator.GetStartingDirectionCount(8));
		}

		[Test]
		public void TestMapGeneration_ValidationRequiresEveryOpenEndToBeReachableFromBase()
		{
			var validator = new TilePlacementValidator();
			validator.AddBaseTile(Vector2Int.zero, new RoadTileDef
			{
				position = Vector2Int.zero,
				connections = RoadConnections.North | RoadConnections.South | RoadConnections.East | RoadConnections.West
			});
			validator.PlaceTile(Vector2Int.up, new RoadTileDef
			{
				position = Vector2Int.up,
				connections = RoadConnections.North | RoadConnections.South
			}, 0);

			validator.PlaceTile(new Vector2Int(4, 0), new RoadTileDef
			{
				position = new Vector2Int(4, 0),
				connections = RoadConnections.East | RoadConnections.West
			}, 0);

			Assert.IsFalse(validator.ValidateRoutesToOpenEnds(Vector2Int.zero, out var reason));
			StringAssert.Contains("not reachable from base", reason);
		}

		private static int CountOpenRoadEnds(Dictionary<Vector2Int, RoadTileDef> map)
		{
			var openRoadEnds = 0;
			foreach (var pair in map)
			{
				var connections = pair.Value.GetRotatedConnections(pair.Value.rotation);
				foreach (RoadSide side in Enum.GetValues(typeof(RoadSide)))
				{
					if (connections.HasConnection(side) && !map.ContainsKey(pair.Key + GetGridDirection(side)))
						openRoadEnds++;
				}
			}

			return openRoadEnds;
		}

		private static Vector2Int GetGridDirection(RoadSide side)
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

		private static string BuildMapSignature(Dictionary<Vector2Int, RoadTileDef> map)
		{
			var entries = new List<string>();
			foreach (var pair in map)
			{
				entries.Add($"{pair.Key.x}:{pair.Key.y}:{pair.Value.rotation}:{(int)pair.Value.connections}");
			}

			entries.Sort(StringComparer.Ordinal);
			return string.Join("|", entries);
		}
	}
}
