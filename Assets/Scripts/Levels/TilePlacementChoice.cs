using System.Collections.Generic;
using UnityEngine;

namespace TD.Levels
{
	public readonly struct TilePlacementChoice
	{
		public bool IsValid { get; }
		public string Reason { get; }
		public RoadTileDef TileDefinition { get; }
		public RoadTileComponent Prefab { get; }
		public Vector2Int GridPosition { get; }
		public string TileName { get; }
		public int Rotation { get; }
		public RoadConnections RotatedConnections { get; }
		public int ConnectedNeighborCount { get; }
		public IReadOnlyList<Vector2Int> OpenRoadEndsBefore { get; }
		public IReadOnlyList<Vector2Int> OpenRoadEndsAfter { get; }
		public IReadOnlyList<Vector2Int> AffectedOpenRoadEnds { get; }
		public int OpenRoadEndCountBefore => OpenRoadEndsBefore.Count;
		public int OpenRoadEndCountAfter => OpenRoadEndsAfter.Count;
		public bool HasTopologyConsequence => AffectedOpenRoadEnds.Count > 0;
		public bool RouteLengthComputed => false;
		public int RouteLengthBefore => -1;
		public int RouteLengthAfter => -1;
		public int RouteLengthDelta => 0;

		public TilePlacementChoice(
			bool isValid,
			string reason,
			RoadTileDef tileDefinition,
			RoadTileComponent prefab,
			Vector2Int gridPosition,
			int rotation,
			RoadConnections rotatedConnections,
			int connectedNeighborCount,
			IReadOnlyList<Vector2Int> openRoadEndsBefore,
			IReadOnlyList<Vector2Int> openRoadEndsAfter,
			IReadOnlyList<Vector2Int> affectedOpenRoadEnds)
		{
			IsValid = isValid;
			Reason = reason;
			TileDefinition = tileDefinition;
			Prefab = prefab;
			GridPosition = gridPosition;
			TileName = tileDefinition.name;
			Rotation = rotation;
			RotatedConnections = rotatedConnections;
			ConnectedNeighborCount = connectedNeighborCount;
			OpenRoadEndsBefore = openRoadEndsBefore;
			OpenRoadEndsAfter = openRoadEndsAfter;
			AffectedOpenRoadEnds = affectedOpenRoadEnds;
		}
	}
}
