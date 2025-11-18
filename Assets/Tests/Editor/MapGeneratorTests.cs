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
}