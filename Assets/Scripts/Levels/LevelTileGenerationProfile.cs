using Sirenix.OdinInspector;
using TD.Voxels;
using UnityEngine;

namespace TD.Levels
{
    public class LevelTileGenerationProfile : GenerationProfile
    {
        [SerializeField] private Color tileBaseColor = Color.gray;
        [SerializeField] private Color tileRoadColor = Color.yellow;
        [SerializeField] private Color roadConnectionColor = Color.white;

        [SerializeField] private float roadHeight = 0.2f;
        [SerializeField] private float roadThickness = 1f;

        private const int TileSize = 5;
        private const int RoadCellIndex = 2;

        [SerializeField] private RoadConnections connections = RoadConnections.None;

        [Button("Toggle North")]
        private void ToggleNorth() => connections ^= RoadConnections.North;

        [Button("Toggle South")]
        private void ToggleSouth() => connections ^= RoadConnections.South;

        [Button("Toggle East")]
        private void ToggleEast() => connections ^= RoadConnections.East;

        [Button("Toggle West")]
        private void ToggleWest() => connections ^= RoadConnections.West;

        protected override void Randomize()
        {
        }

        protected override Color GetColor(int index)
        {
            return index switch
            {
                0 => tileBaseColor,
                1 => tileRoadColor,
                _ => Color.white
            };
        }

        protected override Color GetEmissionColor(int index)
        {
            return Color.black;
        }

        public override void Generate(VoxelGenerator generator)
        {
            base.Generate(generator);
            ClearMeshes(generator.transform.parent ?? generator.transform);

            var parts = new Part[2];

            parts[0] = GenerateBaseTerrain();
            parts[1] = GenerateRoads();

            foreach (var part in parts)
            {
                GeneratePart(generator, part, generator.transform.parent ?? generator.transform, Vector3.zero);
            }
        }

        private Part GenerateBaseTerrain()
        {
            var part = new Part { name = "Base" };

            for (int x = 0; x < TileSize; x++)
            {
                for (int z = 0; z < TileSize; z++)
                {
                    bool isRoad = IsRoadCell(x, z);

                    if (!isRoad)
                    {
                        var voxel = new VoxelData
                        {
                            position = new Vector3(x, 0, z),
                            size = Vector3.one,
                            colorIndex = 0,
                            emissionColorIndex = -1,
                            emissionIntensity = 0
                        };
                        part.voxels.Add(voxel);
                    }
                }
            }

            return part;
        }

        private Part GenerateRoads()
        {
            var part = new Part { name = "Road" };

            for (int x = 0; x < TileSize; x++)
            {
                for (int z = 0; z < TileSize; z++)
                {
                    if (IsRoadCell(x, z))
                    {
                        var voxel = new VoxelData
                        {
                            position = new Vector3(x, 0.5f, z),
                            size = new Vector3(1f, roadHeight, 1f),
                            colorIndex = 1,
                            emissionColorIndex = -1,
                            emissionIntensity = 0
                        };
                        part.voxels.Add(voxel);
                    }
                }
            }

            return part;
        }

        private bool IsRoadCell(int x, int z)
        {
            bool isCenterLine = (x == RoadCellIndex) || (z == RoadCellIndex);
            if (!isCenterLine) return false;

            if (x == RoadCellIndex && z == RoadCellIndex)
                return true;

            if (x == RoadCellIndex && z < RoadCellIndex && connections.HasConnection(RoadSide.North))
                return true;

            if (x == RoadCellIndex && z > RoadCellIndex && connections.HasConnection(RoadSide.South))
                return true;

            if (x < RoadCellIndex && z == RoadCellIndex && connections.HasConnection(RoadSide.West))
                return true;

            if (x > RoadCellIndex && z == RoadCellIndex && connections.HasConnection(RoadSide.East))
                return true;

            return false;
        }
    }
}
