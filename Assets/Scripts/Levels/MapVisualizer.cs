using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TD.Levels
{
	public static class MapVisualizer
	{
		public static string LogCurrentMap()
		{
			TileMapManager tileMapManager = Object.FindAnyObjectByType<TileMapManager>();

			if (tileMapManager == null)
			{
				Debug.LogError("[MapVisualizer] TileMapManager not found!");
				return null;
			}

			IReadOnlyDictionary<Vector2Int, RoadTileDef> allTiles = tileMapManager.GetAllTiles();
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

			foreach (Vector2Int pos in tiles.Keys)
			{
				minX = Mathf.Min(minX, pos.x);
				maxX = Mathf.Max(maxX, pos.x);
				minZ = Mathf.Min(minZ, pos.y);
				maxZ = Mathf.Max(maxZ, pos.y);
			}

			Dictionary<Vector2Int, RoadTileDef> mapData = new Dictionary<Vector2Int, RoadTileDef>(tiles);
			StringBuilder mapBuilder = new System.Text.StringBuilder();
			var stack = Application.GetStackTraceLogType(LogType.Log);
			Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
			Debug.Log("map generated:");

			for (int z = maxZ; z >= minZ; z--)
			{
				mapBuilder.Clear();
				for (int x = minX; x <= maxX; x++)
				{
					Vector2Int pos = new Vector2Int(x, z);
					if (mapData.TryGetValue(pos, out RoadTileDef tile))
					{
						mapBuilder.Append(tile);
					}
					else
					{
						mapBuilder.Append("  ");
					}
				}


				Debug.Log(mapBuilder.ToString());
			}
			Application.SetStackTraceLogType(LogType.Log, stack);

			return mapBuilder.ToString();
		}
	}
}