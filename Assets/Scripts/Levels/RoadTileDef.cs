using UnityEngine;

namespace TD.Levels
{
    [CreateAssetMenu(menuName = "TD/Road Tile Definition")]
    public class RoadTileDef : ScriptableObject
    {
        [field: SerializeField] public RoadConnections connections { get; private set; }
        [field: SerializeField] public Texture2D preview { get; private set; }
        [field: SerializeField] public string description { get; private set; }

        public int ConnectionCount => connections.GetConnectionCount();

        public bool IsValid => connections.IsValidRoadTile();

        public bool CanConnectTo(RoadTileDef other, RoadSide direction)
        {
            if (other == null) return false;
            return connections.CanConnect(other.connections, direction);
        }

        public void Rotate()
        {
            var north = connections.HasConnection(RoadSide.North);
            var south = connections.HasConnection(RoadSide.South);
            var east = connections.HasConnection(RoadSide.East);
            var west = connections.HasConnection(RoadSide.West);

            connections = (north ? RoadConnections.East : 0) |
                         (south ? RoadConnections.West : 0) |
                         (east ? RoadConnections.South : 0) |
                         (west ? RoadConnections.North : 0);
        }

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

        #if UNITY_EDITOR
        public void InitializeConnections(RoadConnections newConnections)
        {
            var fields = typeof(RoadTileDef).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var f in fields)
            {
                if (f.Name.Contains("connections"))
                {
                    if (f.FieldType == typeof(RoadConnections))
                    {
                        f.SetValue(this, newConnections);
                        break;
                    }
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }
        #endif
    }
}
