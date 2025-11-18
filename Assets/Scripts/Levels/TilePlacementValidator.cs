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

                    if (hasConnection != neighborHasConnection)
                        return PlacementResult.Invalid($"Road connection mismatch on {side}");
                }
            }

            return PlacementResult.Valid();
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
