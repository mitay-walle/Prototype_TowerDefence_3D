using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD.Levels
{
	public readonly struct TilePlacementPreview
	{
		public bool IsValid { get; }
		public string Reason { get; }
		public Vector2Int GridPosition { get; }
		public string TileName { get; }
		public int Rotation { get; }
		public RoadConnections RotatedConnections { get; }
		public int ConnectedNeighborCount { get; }
		public IReadOnlyList<Vector2Int> OpenSpawnEnds { get; }
		public IReadOnlyList<Vector2Int> AffectedOpenSpawnEnds { get; }
		public int OpenSpawnEndCountBefore { get; }
		public int OpenSpawnEndCountAfter { get; }
		public bool HasRouteConsequence { get; }
		public bool RouteLengthComputed { get; }
		public int RouteLengthBefore { get; }
		public int RouteLengthAfter { get; }
		public int RouteLengthDelta { get; }

		public TilePlacementPreview(
			bool isValid,
			string reason,
			Vector2Int gridPosition,
			string tileName,
			int rotation,
			RoadConnections rotatedConnections,
			int connectedNeighborCount,
			IReadOnlyList<Vector2Int> openSpawnEnds,
			IReadOnlyList<Vector2Int> affectedOpenSpawnEnds,
			int openSpawnEndCountBefore,
			int openSpawnEndCountAfter,
			bool hasRouteConsequence,
			bool routeLengthComputed,
			int routeLengthBefore,
			int routeLengthAfter,
			int routeLengthDelta)
		{
			IsValid = isValid;
			Reason = reason;
			GridPosition = gridPosition;
			TileName = tileName;
			Rotation = rotation;
			RotatedConnections = rotatedConnections;
			ConnectedNeighborCount = connectedNeighborCount;
			OpenSpawnEnds = openSpawnEnds ?? Array.Empty<Vector2Int>();
			AffectedOpenSpawnEnds = affectedOpenSpawnEnds ?? Array.Empty<Vector2Int>();
			OpenSpawnEndCountBefore = openSpawnEndCountBefore;
			OpenSpawnEndCountAfter = openSpawnEndCountAfter;
			HasRouteConsequence = hasRouteConsequence;
			RouteLengthComputed = routeLengthComputed;
			RouteLengthBefore = routeLengthBefore;
			RouteLengthAfter = routeLengthAfter;
			RouteLengthDelta = routeLengthDelta;
		}

		public static TilePlacementPreview Inactive(Vector2Int gridPosition, int rotation)
		{
			return new TilePlacementPreview(
				false,
				"Tile placement is not active",
				gridPosition,
				string.Empty,
				rotation,
				RoadConnections.None,
				0,
				Array.Empty<Vector2Int>(),
				Array.Empty<Vector2Int>(),
				0,
				0,
				false,
				false,
				-1,
				-1,
				0);
		}
	}
}
