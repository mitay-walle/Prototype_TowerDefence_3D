namespace TD.Levels
{
    [System.Serializable]
    public class RoadTileDef
    {
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
    }
}
