using System.Collections.Generic;
using UnityEngine;

namespace TD.Levels
{
	public class MapVisualizer
	{
		private TileMapManager tileMapManager;
		private bool Logs = true;

		public MapVisualizer(TileMapManager tileMapManager)
		{
			this.tileMapManager = tileMapManager;
		}

		public void VisualizeCurrentMap()
		{
			if (tileMapManager == null)
			{
				Debug.LogError("[MapVisualizer] TileMapManager not found!");
				return;
			}

			var allTiles = tileMapManager.GetAllTiles();
			if (allTiles.Count == 0)
			{
				Debug.LogWarning("[MapVisualizer] No tiles to visualize!");
				return;
			}

			DrawMap(allTiles);
		}

		private void DrawMap(IReadOnlyDictionary<Vector2Int, RoadTileDef> tiles)
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
			var lines = new List<string>();

			lines.Add("[MapVisualizer] === LEVEL MAP ===");
			lines.Add("");

			for (int z = maxZ; z >= minZ; z--)
			{
				string line = "";
				for (int x = minX; x <= maxX; x++)
				{
					var pos = new Vector2Int(x, z);
					if (mapData.TryGetValue(pos, out var tile))
					{
						line += tile.ToString() + " ";
					}
					else
					{
						line += "  ";
					}
				}
				lines.Add(line);
			}

			lines.Add("");
			lines.Add($"[MapVisualizer] Total tiles: {mapData.Count}");
			lines.Add($"[MapVisualizer] Map bounds: X({minX}..{maxX}) Z({minZ}..{maxZ})");

			foreach (var lineText in lines)
			{
				Debug.Log(lineText);
			}
		}
	}
}
