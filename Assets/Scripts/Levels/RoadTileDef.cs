using UnityEngine;

namespace TD.Levels
{
	[System.Serializable]
	public struct RoadTileDef
	{
		public Vector2Int position;
		public int rotation;
		public RoadConnections connections;
		public string name;

		public int ConnectionCount => connections.GetConnectionCount();

		public bool IsValid => connections.IsValidRoadTile();

		public RoadConnections GetRotatedConnections(int rotationCount)
		{
			var result = connections;
			for (int i = 0; i < rotationCount % 4; i++)
			{
				var north = result.HasConnection(RoadSide.North);
				var south = result.HasConnection(RoadSide.South);
				var east = result.HasConnection(RoadSide.East);
				var west = result.HasConnection(RoadSide.West);

				result = (north ? RoadConnections.East : 0) |
						(south ? RoadConnections.West : 0) |
						(east ? RoadConnections.South : 0) |
						(west ? RoadConnections.North : 0);
			}
			return result;
		}

		public override string ToString()
		{
			var rotatedConnections = GetRotatedConnections(rotation);
			var north = rotatedConnections.HasConnection(RoadSide.North);
			var south = rotatedConnections.HasConnection(RoadSide.South);
			var east = rotatedConnections.HasConnection(RoadSide.East);
			var west = rotatedConnections.HasConnection(RoadSide.West);

			string symbol = (north, south, east, west) switch
			{
				(true, true, true, true) => "┼",
				(true, true, true, false) => "┤",
				(true, true, false, true) => "├",
				(true, false, true, true) => "┴",
				(false, true, true, true) => "┬",
				(true, true, false, false) => "│",
				(false, false, true, true) => "─",
				(true, false, true, false) => "┘",
				(true, false, false, true) => "└",
				(false, true, true, false) => "┐",
				(false, true, false, true) => "┌",
				_ => "•"
			};

			return symbol;
		}
	}
}
