using NUnit.Framework;
using TD.Levels;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TD.Tests
{
	public class ValidationTest
	{
		[SetUp]
		public void Setup()
		{
			EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
		}

		[Test]
		public void TestRotationAndValidation()
		{
			var validator = new TilePlacementValidator();

			var baseTile = new RoadTileDef
			{
				position = Vector2Int.zero,
				rotation = 0,
				name = "Base",
				connections = RoadConnections.North | RoadConnections.South | RoadConnections.East | RoadConnections.West
			};

			validator.AddBaseTile(Vector2Int.zero, baseTile);
			Debug.Log($"Base tile at (0,0): {baseTile.connections} → {baseTile}");

			var tile1 = new RoadTileDef
			{
				position = new Vector2Int(0, -1),
				rotation = 0,
				name = "North, East, West",
				connections = RoadConnections.North | RoadConnections.East | RoadConnections.West
			};

			var rotated = tile1.GetRotatedConnections(0);
			Debug.Log($"\nTile1 at (0,-1) rotation=0:");
			Debug.Log($"  Original: {tile1.connections}");
			Debug.Log($"  After GetRotatedConnections(0): {rotated}");
			Debug.Log($"  Visual: {tile1}");
			Debug.Log($"  North? {rotated.HasConnection(RoadSide.North)}");
			Debug.Log($"  South? {rotated.HasConnection(RoadSide.South)}");

			var checkBase = baseTile.GetRotatedConnections(0);
			Debug.Log($"\nBase connections after rotation:");
			Debug.Log($"  South? {checkBase.HasConnection(RoadSide.South)} (should connect to tile1's North)");

			var result = validator.CanPlace(new Vector2Int(0, -1), tile1, 0);
			Debug.Log($"\nCanPlace result: {result.isValid}, reason: {result.reason}");

			if (result.isValid)
			{
				validator.PlaceTile(new Vector2Int(0, -1), tile1, 0);
				Debug.Log("Tile placed successfully!");

				var tile2 = new RoadTileDef
				{
					position = new Vector2Int(1, 0),
					rotation = 1,
					name = "North, South",
					connections = RoadConnections.North | RoadConnections.South
				};

				var tile2Rotated = tile2.GetRotatedConnections(1);
				Debug.Log($"\nTile2 at (1,0) rotation=1:");
				Debug.Log($"  Original: {tile2.connections}");
				Debug.Log($"  After GetRotatedConnections(1): {tile2Rotated}");
				Debug.Log($"  Visual: {tile2}");
				Debug.Log($"  East? {tile2Rotated.HasConnection(RoadSide.East)} (should be true)");
				Debug.Log($"  West? {tile2Rotated.HasConnection(RoadSide.West)} (should connect to base)");

				var baseEast = baseTile.GetRotatedConnections(0);
				Debug.Log($"\nBase East connection: {baseEast.HasConnection(RoadSide.East)} (should connect to tile2's West)");

				var result2 = validator.CanPlace(new Vector2Int(1, 0), tile2, 1);
				Debug.Log($"\nCanPlace tile2 result: {result2.isValid}, reason: {result2.reason}");
			}
		}
	}
}
