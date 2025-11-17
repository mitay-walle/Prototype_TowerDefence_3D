namespace TD
{
    public enum RoadSide
    {
        North = 0,
        South = 1,
        East = 2,
        West = 3
    }

    [System.Flags]
    public enum RoadConnections
    {
        None = 0,
        North = 1 << 0,
        South = 1 << 1,
        East = 1 << 2,
        West = 1 << 3
    }

    public static class RoadConnectionsExtensions
    {
        public static bool HasConnection(this RoadConnections connections, RoadSide side)
        {
            return (connections & (RoadConnections)(1 << (int)side)) != 0;
        }

        public static bool CanConnect(this RoadConnections a, RoadConnections b, RoadSide directionFromAToB)
        {
            var oppositeSide = GetOppositeSide(directionFromAToB);
            return a.HasConnection(directionFromAToB) && b.HasConnection(oppositeSide);
        }

        public static RoadSide GetOppositeSide(RoadSide side) => side switch
        {
            RoadSide.North => RoadSide.South,
            RoadSide.South => RoadSide.North,
            RoadSide.East => RoadSide.West,
            RoadSide.West => RoadSide.East,
            _ => RoadSide.North
        };

        public static int GetConnectionCount(this RoadConnections connections)
        {
            int count = 0;
            if (connections.HasConnection(RoadSide.North)) count++;
            if (connections.HasConnection(RoadSide.South)) count++;
            if (connections.HasConnection(RoadSide.East)) count++;
            if (connections.HasConnection(RoadSide.West)) count++;
            return count;
        }

        public static bool IsValidRoadTile(this RoadConnections connections)
        {
            int count = GetConnectionCount(connections);
            return count >= 2;
        }
    }
}
