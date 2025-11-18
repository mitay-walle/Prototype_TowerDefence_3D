using System.Collections.Generic;
using UnityEngine;

namespace TD.Levels
{
	public static class MapVisualizer
	{
		public static string LogCurrentMap()
		{
			var tileMapManager = Object.FindAnyObjectByType<TileMapManager>();

			if (tileMapManager == null)
			{
				Debug.LogError("[MapVisualizer] TileMapManager not found!");
				return null;
			}

			var allTiles = tileMapManager.GetAllTiles();
			if (allTiles.Count == 0)
			{
				Debug.LogWarning("[MapVisualizer] No tiles to visualize!");
				return null;
			}

			return LogMap(allTiles);
		}

		public static string LogMap(IReadOnlyDictionary<Vector2Int, RoadTileDef> tiles)
		{
			int minX = int.MaxValue;
			int maxX = int.MinValue;
			int minZ = int.MaxValue;
			int maxZ = int.MinValue;

			foreach (var pos in tiles.Keys)
			{
				minX = Mathf.Min(minX, pos.x);
				maxX = Mathf.Max(maxX, pos.x);
				minZ = Mathf.Min(minZ, pos.y);
				maxZ = Mathf.Max(maxZ, pos.y);
			}

			var mapData = new Dictionary<Vector2Int, RoadTileDef>(tiles);
			var mapBuilder = new System.Text.StringBuilder();

			for (int z = maxZ; z >= minZ; z--)
			{
				for (int x = minX; x <= maxX; x++)
				{
					var pos = new Vector2Int(x, z);
					if (mapData.TryGetValue(pos, out var tile))
					{
						mapBuilder.Append(tile);
					}
					else
					{
						mapBuilder.Append("  ");
					}
				}

				mapBuilder.AppendLine();
			}

			Debug.Log(mapBuilder.ToString());
			return mapBuilder.ToString();
		}
	}
}