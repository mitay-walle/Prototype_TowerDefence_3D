using System.Reflection;
using NUnit.Framework;
using TD.Levels;
using UnityEngine;

namespace TD.Tests
{
	public class TileSurfacePlacementContractTests
	{
		[Test]
		public void TileSurfaceRaycastReturnsPlacedTileGridWithoutGroundPlane()
		{
			var managerObject = new GameObject("TileSurfacePlacementContractTests");
			var manager = managerObject.AddComponent<TileMapManager>();
			var tilesParentField = typeof(TileMapManager).GetField("tilesParent", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(tilesParentField, Is.Not.Null);
			tilesParentField.SetValue(manager, managerObject.transform);

			var prefab = new GameObject("TileSurfacePrefab");
			prefab.AddComponent<RoadTileComponent>();
			var collider = prefab.AddComponent<BoxCollider>();
			collider.size = new Vector3(4f, 1f, 4f);
			var tileDefinition = new RoadTileDef
			{
				connections = RoadConnections.North | RoadConnections.South | RoadConnections.East | RoadConnections.West,
				name = "TileSurface"
			};
			manager.PlaceTile(new Vector2Int(2, 3), tileDefinition, 0, prefab);
			Physics.SyncTransforms();

			var ray = new Ray(new Vector3(10f, 10f, 15f), Vector3.down);
			var hit = manager.TryGetTileSurfacePoint(ray, out var worldPoint, out var gridPosition);

			Assert.That(hit, Is.True);
			Assert.That(gridPosition, Is.EqualTo(new Vector2Int(2, 3)));
			Assert.That(worldPoint.y, Is.GreaterThan(0f));

			Object.DestroyImmediate(managerObject);
			Object.DestroyImmediate(prefab);
		}
	}
}