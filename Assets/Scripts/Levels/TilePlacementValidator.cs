using System.Collections.Generic;
using UnityEngine;

namespace TD.Levels
{
    public class TilePlacementValidator
    {
        public struct PlacementResult
        {
            public bool isValid;
            public string reason;

            public PlacementResult(bool valid, string reason = "")
            {
                isValid = valid;
                this.reason = reason;
            }

            public static PlacementResult Valid() => new PlacementResult(true, "");
            public static PlacementResult Invalid(string reason) => new PlacementResult(false, reason);
        }

        private Dictionary<Vector2Int, RoadTileDef> placedTiles;
        private Dictionary<Vector2Int, int> tileRotations;

        public TilePlacementValidator()
        {
            placedTiles = new Dictionary<Vector2Int, RoadTileDef>();
            tileRotations = new Dictionary<Vector2Int, int>();
        }

        public void AddBaseTile(Vector2Int position, RoadTileDef baseTile)
        {
            placedTiles[position] = baseTile;
            tileRotations[position] = 0;
        }

        public PlacementResult CanPlace(Vector2Int position, RoadTileDef tileDef, int rotation)
        {
            if (tileDef.name == null)
                return PlacementResult.Invalid("Tile definition is null");

            if (!tileDef.IsValid)
                return PlacementResult.Invalid("Tile has invalid connections (dead end)");

            if (placedTiles.ContainsKey(position))
                return PlacementResult.Invalid("Tile already placed at this position");

            var rotatedConnections = tileDef.GetRotatedConnections(rotation);

            var directions = new (RoadSide side, Vector2Int offset)[]
            {
                (RoadSide.North, Vector2Int.up),
                (RoadSide.South, Vector2Int.down),
                (RoadSide.East, Vector2Int.right),
                (RoadSide.West, Vector2Int.left)
            };


		bool hasAtLeastOneConnection = false;
            foreach (var (side, offset) in directions)
            {
                var neighborPos = position + offset;
                var hasConnection = (rotatedConnections & (RoadConnections)(1 << (int)side)) != 0;

                if (placedTiles.TryGetValue(neighborPos, out var neighbor))
                {
                    int neighborRotation = tileRotations[neighborPos];
                    var neighborConnections = neighbor.GetRotatedConnections(neighborRotation);
                    var oppositeSide = RoadConnectionsExtensions.GetOppositeSide(side);
                    bool neighborHasConnection = (neighborConnections & (RoadConnections)(1 << (int)oppositeSide)) != 0;

                    if (hasConnection && !neighborHasConnection)
                        return PlacementResult.Invalid($"Road on {side} but neighbor has no road back");

                    if (!hasConnection && neighborHasConnection)
                        return PlacementResult.Invalid($"No road on {side} but neighbor has road towards us");

				if (hasConnection && neighborHasConnection)
					hasAtLeastOneConnection = true;
                }
            }

            foreach (var (side, offset) in directions)
            {
                var neighborPos = position + offset;
                var hasConnection = (rotatedConnections & (RoadConnections)(1 << (int)side)) != 0;

                if (!placedTiles.ContainsKey(neighborPos))
                {
                    var tilesAroundEmpty = new (RoadSide, Vector2Int)[]
                    {
                        (RoadSide.North, neighborPos + Vector2Int.up),
                        (RoadSide.South, neighborPos + Vector2Int.down),
                        (RoadSide.East, neighborPos + Vector2Int.right),
                        (RoadSide.West, neighborPos + Vector2Int.left)
                    };

                    foreach (var (checkSide, checkPos) in tilesAroundEmpty)
                    {
                        if (checkPos == position) continue;

                        if (placedTiles.TryGetValue(checkPos, out var checkTile))
                        {
                            int checkRotation = tileRotations[checkPos];
                            var checkConnections = checkTile.GetRotatedConnections(checkRotation);
                            var sideFromCheckToEmpty = GetSideFromTo(checkPos, neighborPos);

                            if (sideFromCheckToEmpty.HasValue && checkConnections.HasConnection(sideFromCheckToEmpty.Value))
                            {
                                if (!hasConnection)
                                {
                                    return PlacementResult.Invalid($"Cannot place tile without road on {side}: tile at {checkPos} has road pointing to empty {neighborPos}");
                                }
                            }
                        }
                    }
                }
            }


		if (placedTiles.Count > 0 && !hasAtLeastOneConnection)
			return PlacementResult.Invalid("Tile must connect to at least one existing road");

            return PlacementResult.Valid();
        }

        private RoadSide[] GetAdjacentSides(RoadSide side)
        {
            return side switch
            {
                RoadSide.North => new[] { RoadSide.East, RoadSide.West },
                RoadSide.South => new[] { RoadSide.East, RoadSide.West },
                RoadSide.East => new[] { RoadSide.North, RoadSide.South },
                RoadSide.West => new[] { RoadSide.North, RoadSide.South },
                _ => new RoadSide[0]
            };
        }

        private Vector2Int GetOffset(RoadSide side)
        {
            return side switch
            {
                RoadSide.North => Vector2Int.up,
                RoadSide.South => Vector2Int.down,
                RoadSide.East => Vector2Int.right,
                RoadSide.West => Vector2Int.left,
                _ => Vector2Int.zero
            };
        }

        private RoadSide? GetSideFromTo(Vector2Int from, Vector2Int to)
        {
            var delta = to - from;
            if (delta == Vector2Int.up) return RoadSide.North;
            if (delta == Vector2Int.down) return RoadSide.South;
            if (delta == Vector2Int.right) return RoadSide.East;
            if (delta == Vector2Int.left) return RoadSide.West;
            return null;
        }

        public void PlaceTile(Vector2Int position, RoadTileDef tileDef, int rotation)
        {
            placedTiles[position] = tileDef;
            tileRotations[position] = rotation % 4;
        }

        public void RemoveTile(Vector2Int position)
        {
            placedTiles.Remove(position);
            tileRotations.Remove(position);
        }

        public RoadTileDef? GetTile(Vector2Int position)
        {
            if (placedTiles.TryGetValue(position, out var tile))
                return tile;
            return null;
        }

        public int GetTileRotation(Vector2Int position)
        {
            tileRotations.TryGetValue(position, out var rotation);
            return rotation;
        }

        public IReadOnlyDictionary<Vector2Int, RoadTileDef> GetAllTiles() => placedTiles;
    }
}
