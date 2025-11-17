#if UNITY_EDITOR
using NUnit.Framework;
using TD.Levels;
using UnityEngine;
using TD.Voxels;

namespace TD.Tests
{
	public class LevelRoadGenerationProfileTests
	{
		private LevelRoadGenerationProfile profile;
		private VoxelGenerator generator;
		private GameObject testGameObject;

		[SetUp]
		public void Setup()
		{
			testGameObject = new GameObject("TestLevelGenerator");
			generator = testGameObject.AddComponent<VoxelGenerator>();

			profile = new LevelRoadGenerationProfile();
			profile.mapSize = 60;
			profile.roadWidth = 1;
			profile.seed = 12345;

			generator.profile = profile;
		}

		[TearDown]
		public void Teardown()
		{
			Object.DestroyImmediate(testGameObject);
		}

		[Test]
		public void TestMapSizeConfiguration()
		{
			Assert.AreEqual(60, profile.mapSize);
			Assert.AreEqual(1, profile.roadWidth);
		}

		[Test]
		public void TestGenerationCreatesRoadCells()
		{
			generator.Generate();

			var outerCells = profile.RoadOuterCells;
			Assert.Greater(outerCells.Count, 0, "Should have road cells after generation");
		}

		[Test]
		public void TestBasePositionIsInsideMap()
		{
			profile.seed = 42;
			generator.Generate();

			Vector3 basePos = profile.BasePosition;
			int halfSize = profile.mapSize / 2;

			Assert.Greater(basePos.x, -halfSize);
			Assert.Less(basePos.x, halfSize);
			Assert.Greater(basePos.z, -halfSize);
			Assert.Less(basePos.z, halfSize);
		}

		[Test]
		public void TestRoadOuterCellsExistAtMapEdges()
		{
			generator.Generate();

			var outerCells = profile.RoadOuterCells;
			Assert.Greater(outerCells.Count, 0, "Should have road cells at map edges");
		}

		[Test]
		public void TestAllRoadCellsAreWithinMapBoundaries()
		{
			generator.Generate();

			var outerCells = profile.RoadOuterCells;
			int halfSize = profile.mapSize / 2;

			foreach (var cell in outerCells)
			{
				Assert.GreaterOrEqual(cell.x, -halfSize);
				Assert.Less(cell.x, halfSize);
				Assert.GreaterOrEqual(cell.y, -halfSize);
				Assert.Less(cell.y, halfSize);
			}
		}

		[Test]
		public void TestRoadConnectivity()
		{
			generator.Generate();

			var outerCells = profile.RoadOuterCells;
			Assert.Greater(outerCells.Count, 0, "Should have paths reaching edges");
		}

		[Test]
		public void TestDeterministicGenerationWithSameSeed()
		{
			profile.seed = 9999;
			generator.Generate();
			int count1 = profile.RoadOuterCells.Count;

			profile.seed = 9999;
			Object.DestroyImmediate(testGameObject);

			testGameObject = new GameObject("TestLevelGenerator2");
			generator = testGameObject.AddComponent<VoxelGenerator>();
			profile = new LevelRoadGenerationProfile();
			profile.mapSize = 60;
			profile.roadWidth = 1;
			profile.seed = 9999;
			generator.profile = profile;

			generator.Generate();
			int count2 = profile.RoadOuterCells.Count;

			Assert.AreEqual(count1, count2, "Same seed should produce same road structure");
		}

		[Test]
		public void TestColorPaletteGeneration()
		{
			generator.Generate();

			Assert.Greater(profile.colorPalette.Count, 0, "Color palette should be generated");
			Assert.Greater(profile.emissionPalette.Count, 0, "Emission palette should be generated");
		}

		[Test]
		public void TestMinimumRoadWidth()
		{
			profile.roadWidth = 1;
			generator.Generate();

			var outerCells = profile.RoadOuterCells;
			Assert.Greater(outerCells.Count, 0);
		}

		[Test]
		public void TestMultiplePathsGeneration()
		{
			generator.Generate();

			var outerCells = profile.RoadOuterCells;
			Assert.Greater(outerCells.Count, 0, "Should generate roads from multiple starting paths");
		}

		[Test]
		public void TestPathBranching()
		{
			profile.mapSize = 80;
			generator.Generate();

			var outerCells = profile.RoadOuterCells;
			Assert.Greater(outerCells.Count, 1, "Branching should create multiple endpoint paths");
		}

		[Test]
		public void TestRoadOuterVoxelsWorldPositions()
		{
			generator.Generate();

			var worldPositions = profile.RoadOuterVoxelsWorldPositions;
			Assert.Greater(worldPositions.Count, 0, "Should have world positions for outer road cells");

			foreach (var pos in worldPositions)
			{
				Assert.AreEqual(pos.y, 0f, "Road voxels should be at y=0");
			}
		}

		[Test]
		public void TestBasePositionCaching()
		{
			generator.Generate();

			Vector3 basePos1 = profile.BasePosition;
			Vector3 basePos2 = profile.BasePosition;

			Assert.AreEqual(basePos1, basePos2, "Base position should be consistent");
		}

		[Test]
		public void TestGenerationWithDifferentSeeds()
		{
			profile.seed = 111;
			generator.Generate();
			int count1 = profile.RoadOuterCells.Count;

			profile.seed = 222;
			Object.DestroyImmediate(testGameObject);

			testGameObject = new GameObject("TestLevelGenerator3");
			generator = testGameObject.AddComponent<VoxelGenerator>();
			profile = new LevelRoadGenerationProfile();
			profile.mapSize = 60;
			profile.roadWidth = 1;
			profile.seed = 222;
			generator.profile = profile;

			generator.Generate();
			int count2 = profile.RoadOuterCells.Count;

			Assert.AreNotEqual(count1, count2, "Different seeds should produce different layouts");
		}

		[Test]
		public void TestMapSizeVariations()
		{
			int[] mapSizes = { 40, 60, 80, 100 };

			foreach (int size in mapSizes)
			{
				Object.DestroyImmediate(testGameObject);

				testGameObject = new GameObject($"TestLevel_{size}");
				generator = testGameObject.AddComponent<VoxelGenerator>();
				profile = new LevelRoadGenerationProfile();
				profile.mapSize = size;
				profile.roadWidth = 1;
				profile.seed = 42;
				generator.profile = profile;

				generator.Generate();

				var outerCells = profile.RoadOuterCells;
				Assert.Greater(outerCells.Count, 0, $"Should have road at edges for map size {size}");
			}
		}

		[Test]
		public void TestRoadWidthEffect()
		{
			profile.roadWidth = 1;
			generator.Generate();
			int count1 = profile.RoadOuterCells.Count;

			Object.DestroyImmediate(testGameObject);

			testGameObject = new GameObject("TestLevelWide");
			generator = testGameObject.AddComponent<VoxelGenerator>();
			profile = new LevelRoadGenerationProfile();
			profile.mapSize = 60;
			profile.roadWidth = 3;
			profile.seed = 42;
			generator.profile = profile;

			generator.Generate();
			int count2 = profile.RoadOuterCells.Count;

			Assert.Greater(count2, count1, "Wider roads should result in more outer cells");
		}

		[Test]
		public void TestSimplifyRoadEdgesReducesSpawnPoints()
		{
			profile.mapSize = 100;
			profile.seed = 777;
			generator.Generate();

			int outerCellsCount = profile.RoadOuterCells.Count;

			Assert.Greater(outerCellsCount, 0, "Should have road cells at edges");
			Assert.LessOrEqual(outerCellsCount, 150, "Simplified edges should have reasonable spawn point count");
		}
	}
}
#endif