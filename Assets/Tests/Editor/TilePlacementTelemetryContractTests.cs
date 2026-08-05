using System.Collections.Generic;
using NUnit.Framework;
using TD.Levels;
using UnityEditor;
using UnityEngine;

namespace TD.Tests
{
	public class TilePlacementTelemetryContractTests
	{
		[Test]
		public void PlacementOptionsPublishSelectionAndCancellation()
		{
			var systemObject = new GameObject("TilePlacementTelemetryContractTests.System");
			var prefab = new GameObject("TilePlacementTelemetryContractTests.Prefab");
			var prefabComponent = prefab.AddComponent<RoadTileComponent>();
			var system = systemObject.AddComponent<TilePlacementSystem>();
			var selectedIndices = new List<int>();
			var cancelledIndex = -1;

			var choiceA = CreateChoice(prefabComponent, "ChoiceA", new Vector2Int(1, 0));
			var choiceB = CreateChoice(prefabComponent, "ChoiceB", new Vector2Int(0, 1));

			try
			{
				system.onPlacementChoiceSelected.AddListener(index => selectedIndices.Add(index));
				system.onPlacementCancelled.AddListener(index => cancelledIndex = index);

				system.StartTilePlacementOptions(new[] { choiceA, choiceB });
				Assert.That(system.HasSelectedChoice, Is.True);
				Assert.That(system.SelectedChoiceIndex, Is.EqualTo(0));
				Assert.That(selectedIndices, Is.EqualTo(new[] { 0 }));

				system.SelectNextOption();
				Assert.That(system.SelectedChoiceIndex, Is.EqualTo(1));
				Assert.That(selectedIndices, Is.EqualTo(new[] { 0, 1 }));

				system.CancelPlacement();
				Assert.That(system.IsPlacing, Is.False);
				Assert.That(cancelledIndex, Is.EqualTo(1));
			}
			finally
			{
				system.CancelPlacement();
				Object.DestroyImmediate(systemObject);
				Object.DestroyImmediate(prefab);
			}
		}

		[Test]
		public void PlacementChoicesPreferDistinctTopologyOutcomes()
		{
			var managerObject = new GameObject("TilePlacementTelemetryContractTests.Map");
			var prefabs = new List<RoadTileComponent>();
			try
			{
				var manager = managerObject.AddComponent<TileMapManager>();
				Assert.That(manager.PlaceTileLogic(Vector2Int.zero, CreateDefinition("Base", RoadConnections.North | RoadConnections.South | RoadConnections.East | RoadConnections.West), 0), Is.True);
				Assert.That(manager.PlaceTileLogic(new Vector2Int(0, -1), CreateDefinition("South", RoadConnections.North | RoadConnections.South), 0), Is.True);
				Assert.That(manager.PlaceTileLogic(new Vector2Int(0, 1), CreateDefinition("North", RoadConnections.North | RoadConnections.South), 0), Is.True);
				Assert.That(manager.PlaceTileLogic(new Vector2Int(1, 0), CreateDefinition("East", RoadConnections.East | RoadConnections.West), 0), Is.True);
				Assert.That(manager.PlaceTileLogic(new Vector2Int(-1, 0), CreateDefinition("West", RoadConnections.East | RoadConnections.West), 0), Is.True);

				prefabs.Add(CreatePrefab("Straight", RoadConnections.North | RoadConnections.South));
				prefabs.Add(CreatePrefab("Turn", RoadConnections.North | RoadConnections.East));
				prefabs.Add(CreatePrefab("Cross_3", RoadConnections.North | RoadConnections.East | RoadConnections.West));
				prefabs.Add(CreatePrefab("Cross_4", RoadConnections.North | RoadConnections.South | RoadConnections.East | RoadConnections.West));

				var choices = manager.BuildPlacementChoices(prefabs, 3);
				Assert.That(choices.Count, Is.EqualTo(3));
				Assert.That(choices[0].OpenRoadEndCountAfter, Is.LessThanOrEqualTo(choices[1].OpenRoadEndCountAfter));
				Assert.That(choices[1].OpenRoadEndCountAfter, Is.LessThanOrEqualTo(choices[2].OpenRoadEndCountAfter));
				Assert.That(choices[0].OpenRoadEndCountAfter, Is.Not.EqualTo(choices[2].OpenRoadEndCountAfter));
			}
			finally
			{
				Object.DestroyImmediate(managerObject);
				for (var i = 0; i < prefabs.Count; i++)
					Object.DestroyImmediate(prefabs[i].gameObject);
			}
		}

		[Test]
		public void HypotheticalSpawnPositionsReadbackDoesNotMutateMap()
		{
			var managerObject = new GameObject("TilePlacementTelemetryContractTests.HypotheticalMap");
			try
			{
				var manager = managerObject.AddComponent<TileMapManager>();
				Assert.That(manager.PlaceTileLogic(Vector2Int.zero, CreateDefinition("Base", RoadConnections.North | RoadConnections.South | RoadConnections.East | RoadConnections.West), 0), Is.True);

				var definition = CreateDefinition("Straight", RoadConnections.North | RoadConnections.South);
				var choice = new TilePlacementChoice(
					true,
					string.Empty,
					definition,
					null,
					new Vector2Int(0, 1),
					0,
					definition.connections,
					1,
					new List<Vector2Int>(),
					new List<Vector2Int> { new Vector2Int(0, 2) },
					new List<Vector2Int> { new Vector2Int(0, 2) });

				var spawnPositionsBefore = new List<Vector3>(manager.SpawnPositions);
				var spawnPositionsAfter = manager.GetSpawnPositionsAfter(choice);

				Assert.That(spawnPositionsBefore, Is.Empty);
				Assert.That(spawnPositionsAfter.Count, Is.EqualTo(1));
				Assert.That(manager.GetAllTiles().Count, Is.EqualTo(1));
			}
			finally
			{
				Object.DestroyImmediate(managerObject);
			}
		}

		[Test]
		public void SelectedChoiceCoverageReadbackExposesBeforeAndAfter()
		{
			var managerObject = new GameObject("TilePlacementTelemetryContractTests.Coverage");
			var prefab = CreatePrefab("Straight", RoadConnections.North | RoadConnections.South);
			try
			{
				var manager = managerObject.AddComponent<TileMapManager>();
				Assert.That(manager.PlaceTileLogic(
					Vector2Int.zero,
					CreateDefinition("Base", RoadConnections.North | RoadConnections.South | RoadConnections.East | RoadConnections.West),
					0), Is.True);

				var definition = CreateDefinition("Straight", RoadConnections.North | RoadConnections.South);
				var choice = new TilePlacementChoice(
					true,
					string.Empty,
					definition,
					prefab,
					new Vector2Int(0, 1),
					0,
					definition.connections,
					1,
					new List<Vector2Int>(),
					new List<Vector2Int> { new Vector2Int(0, 2) },
					new List<Vector2Int> { new Vector2Int(0, 2) });

				var system = managerObject.AddComponent<TilePlacementSystem>();
				var serializedSystem = new SerializedObject(system);
				serializedSystem.FindProperty("tileMapManager").objectReferenceValue = manager;
				serializedSystem.FindProperty("ghostPrefab").objectReferenceValue = prefab.gameObject;
				serializedSystem.ApplyModifiedPropertiesWithoutUndo();
				system.StartTilePlacementOptions(new[] { choice });

				Assert.That(system.HasSelectedChoice, Is.True);
				Assert.That(system.SelectedChoiceTotalEntrancesBefore, Is.EqualTo(manager.SpawnPositions.Count));
				Assert.That(system.SelectedChoiceTotalEntrancesAfter, Is.EqualTo(1));
				Assert.That(system.SelectedChoiceCoveredEntrancesBefore, Is.EqualTo(0));
				Assert.That(system.SelectedChoiceCoveredEntrancesAfter, Is.EqualTo(0));
				Assert.That(manager.GetAllTiles().Count, Is.EqualTo(1));

				system.CancelPlacement();
				Assert.That(system.SelectedChoiceTotalEntrancesBefore, Is.EqualTo(0));
				Assert.That(system.SelectedChoiceTotalEntrancesAfter, Is.EqualTo(0));
			}
			finally
			{
				Object.DestroyImmediate(managerObject);
				Object.DestroyImmediate(prefab.gameObject);
			}
		}

		private static TilePlacementChoice CreateChoice(RoadTileComponent prefab, string name, Vector2Int gridPosition)
		{
			var definition = new RoadTileDef
			{
				name = name,
				connections = RoadConnections.North | RoadConnections.South
			};

			return new TilePlacementChoice(
				true,
				string.Empty,
				definition,
				prefab,
				gridPosition,
				0,
				definition.connections,
				1,
				new List<Vector2Int> { Vector2Int.left },
				new List<Vector2Int> { Vector2Int.right },
				new List<Vector2Int> { gridPosition });
		}

		private static RoadTileDef CreateDefinition(string name, RoadConnections connections)
		{
			return new RoadTileDef
			{
				name = name,
				connections = connections
			};
		}

		private static RoadTileComponent CreatePrefab(string name, RoadConnections connections)
		{
			var prefab = new GameObject(name).AddComponent<RoadTileComponent>();
			prefab.Initialize(connections);
			return prefab;
		}
	}
}
