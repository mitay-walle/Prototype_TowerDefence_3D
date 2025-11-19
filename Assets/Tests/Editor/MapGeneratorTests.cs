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
	}
}