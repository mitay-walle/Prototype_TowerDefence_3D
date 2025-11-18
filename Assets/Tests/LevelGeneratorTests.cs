#if UNITY_EDITOR
using NUnit.Framework;
using TD.Levels;
using UnityEngine;

namespace TD.Tests
{
	public class LevelGeneratorTests
	{
		[Test]
		public void TestLevelGeneration()
		{
			var LevelGenerator = Object.FindAnyObjectByType<LevelGenerator>();
			if (LevelGenerator == null)
			{
				Debug.LogError("[LevelGenerator Test] LevelGenerator not found!");
				Assert.Fail("LevelGenerator not found");
				return;
			}

			var tileMapManager = Object.FindAnyObjectByType<TileMapManager>();
			if (tileMapManager == null)
			{
				Debug.LogError("[LevelGenerator Test] TileMapManager not found!");
				Assert.Fail("TileMapManager not found");
				return;
			}

			LevelGenerator.ClearLevel();
			LevelGenerator.GenerateLevel();

			var allTiles = tileMapManager.GetAllTiles();
			var spawnPositions = tileMapManager.SpawnPositions;

			Debug.Log($"[LevelGenerator Test] Generation completed: {allTiles.Count} tiles, {spawnPositions.Count} spawn points");

			Assert.IsTrue(allTiles.Count > 1, "Should have generated at least 2 tiles (base + generated)");
			Assert.IsTrue(spawnPositions.Count > 0, "Should have at least one spawn point");

			LevelGenerator.mapVisualizer.VisualizeCurrentMap();
		}
	}
}
#endif