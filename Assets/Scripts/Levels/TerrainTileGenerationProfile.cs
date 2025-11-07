using System.Collections.Generic;
using TD.Voxels;
using UnityEngine;

namespace TD
{
	[System.Serializable]
	public class TerrainTileGenerationProfile : GenerationProfile
	{
		public int cellsPerTile = 6;
		public int voxelsPerCell = 8;
		public float heightScale = 10f;
		public float noiseScale = 0.1f;
		public float curvePower = 1.5f;

		public bool roadNorth;
		public bool roadEast;
		public bool roadSouth;
		public bool roadWest;

		public List<Color> colorPalette = new List<Color>();
		public List<Color> emissionPalette = new List<Color>();

		public Part terrain = new Part { name = "Terrain" };

		private float heightCurvePower;
		private float noiseOffsetX;
		private float noiseOffsetZ;
		private int roadConnectionCount;
		private Vector2 intersectionOffset;
		private int roadVariation;

		// 0=straight horizontal, 1=straight vertical, 2=curve smooth, 3=curve sharp, 4=curve wide
		private static readonly int[][] roadShapes = new int[][]
		{
			new int[] { 0, 0, 0, 1, 1, 1, 0, 0, 0 },
			new int[] { 0, 1, 0, 0, 1, 0, 0, 1, 0 },
			new int[] { 0, 0, 1, 0, 1, 1, 1, 1, 0 },
			new int[] { 0, 1, 1, 0, 0, 1, 0, 0, 1 },
			new int[] { 1, 1, 0, 1, 1, 1, 0, 1, 1 }
		};

		protected override void Randomize()
		{
			Random.InitState(Seed);
			GenerateColorPalette();

			heightCurvePower = Random.Range(0.5f, 3f);
			noiseOffsetX = Random.Range(0f, 1000f);
			noiseOffsetZ = Random.Range(0f, 1000f);

			roadConnectionCount = (roadNorth ? 1 : 0) + (roadEast ? 1 : 0) + (roadSouth ? 1 : 0) + (roadWest ? 1 : 0);

			if (roadConnectionCount >= 3)
			{
				float maxOffset = voxelsPerCell * 0.5f;
				intersectionOffset = new Vector2(Random.Range(-maxOffset, maxOffset), Random.Range(-maxOffset, maxOffset));
			}
			else
			{
				intersectionOffset = Vector2.zero;
			}

			roadVariation = Random.Range(0, roadShapes.Length);
		}

		public override void Generate(VoxelGenerator generator)
		{
			base.Generate(generator);

			terrain.voxels.Clear();
			GenerateTerrain();
			GeneratePart(generator, terrain, generator.transform, Vector3.zero);
		}

		private void GenerateColorPalette()
		{
			colorPalette.Clear();
			emissionPalette.Clear();

			Color baseColor = RandomColor(0.2f, 0.5f);
			Color accentColor = RandomColor(0.3f, 0.6f);
			Color darkColor = RandomColor(0.1f, 0.3f);

			colorPalette.Add(baseColor);
			colorPalette.Add(accentColor);
			colorPalette.Add(darkColor);
			colorPalette.Add(Color.Lerp(baseColor, accentColor, 0.5f));

			emissionPalette.Add(new Color(0.5f, 0.5f, 1f));
		}

		private Color RandomColor(float min, float max)
		{
			return new Color(Random.Range(min, max), Random.Range(min, max), Random.Range(min, max));
		}

		protected override Color GetColor(int index)
		{
			return index >= 0 && index < colorPalette.Count ? colorPalette[index] : Color.white;
		}

		protected override Color GetEmissionColor(int index)
		{
			return index >= 0 && index < emissionPalette.Count ? emissionPalette[index] : Color.white;
		}

		private void GenerateTerrain()
		{
			int totalSize = cellsPerTile * voxelsPerCell;
			int halfSize = totalSize / 2;

			for (int x = -halfSize; x < halfSize; x++)
			{
				for (int z = -halfSize; z < halfSize; z++)
				{
					if (IsRoadCell(x, z, halfSize))
					{
						AddRoadVoxel(x, z);
					}
					else
					{
						float height = CalculateHeight(x, z);
						int colorIndex = GetTerrainColorIndex(height);
						AddTerrainColumn(x, z, height, colorIndex);
					}
				}
			}
		}

		private bool IsRoadCell(int x, int z, int halfSize)
		{
			int roadWidth = voxelsPerCell * 2;
			int edgeMargin = voxelsPerCell;

			if (roadConnectionCount >= 3)
			{
				return IsRoadCellCrossroad(x, z, halfSize, edgeMargin, roadWidth);
			}
			else if (roadConnectionCount == 2)
			{
				return IsRoadCellTurn(x, z, halfSize, roadWidth);
			}
			else if (roadConnectionCount == 1)
			{
				return IsRoadCellStraight(x, z, halfSize, edgeMargin, roadWidth);
			}

			return false;
		}

		private bool IsRoadCellCrossroad(int x, int z, int halfSize, int edgeMargin, int roadWidth)
		{
			float offsetX = x - intersectionOffset.x;
			float offsetZ = z - intersectionOffset.y;

			if (roadNorth && offsetZ >= halfSize - edgeMargin && Mathf.Abs(offsetX) <= roadWidth)
				return true;

			if (roadSouth && offsetZ <= -(halfSize - edgeMargin) && Mathf.Abs(offsetX) <= roadWidth)
				return true;

			if (roadEast && offsetX >= halfSize - edgeMargin && Mathf.Abs(offsetZ) <= roadWidth)
				return true;

			if (roadWest && offsetX <= -(halfSize - edgeMargin) && Mathf.Abs(offsetZ) <= roadWidth)
				return true;

			if (Mathf.Abs(offsetX) <= roadWidth && Mathf.Abs(offsetZ) <= roadWidth)
				return true;

			return false;
		}

		private bool IsRoadCellTurn(int x, int z, int halfSize, int roadWidth)
		{
			bool isVerticalRoad = (roadNorth || roadSouth);
			bool isHorizontalRoad = (roadEast || roadWest);

			if (!isVerticalRoad || !isHorizontalRoad)
			{
				return IsRoadCellStraight(x, z, halfSize, voxelsPerCell, roadWidth);
			}

			int cellX = Mathf.FloorToInt((x + halfSize) / (float)voxelsPerCell);
			int cellZ = Mathf.FloorToInt((z + halfSize) / (float)voxelsPerCell);

			cellX = Mathf.Clamp(cellX, 0, cellsPerTile - 1);
			cellZ = Mathf.Clamp(cellZ, 0, cellsPerTile - 1);

			int gridX = cellX * 3 / cellsPerTile;
			int gridZ = cellZ * 3 / cellsPerTile;

			gridX = Mathf.Clamp(gridX, 0, 2);
			gridZ = Mathf.Clamp(gridZ, 0, 2);

			int shapeIndex = gridZ * 3 + gridX;
			if (shapeIndex >= 0 && shapeIndex < 9)
			{
				return roadShapes[roadVariation][shapeIndex] == 1;
			}

			return false;
		}

		private bool IsRoadCellStraight(int x, int z, int halfSize, int edgeMargin, int roadWidth)
		{
			if (roadNorth && z >= halfSize - edgeMargin && Mathf.Abs(x) <= roadWidth)
				return true;

			if (roadSouth && z <= -(halfSize - edgeMargin) && Mathf.Abs(x) <= roadWidth)
				return true;

			if (roadEast && x >= halfSize - edgeMargin && Mathf.Abs(z) <= roadWidth)
				return true;

			if (roadWest && x <= -(halfSize - edgeMargin) && Mathf.Abs(z) <= roadWidth)
				return true;

			return false;
		}

		private void AddRoadVoxel(int x, int z)
		{
			terrain.voxels.Add(new VoxelData
			{
				position = new Vector3(x, 0, z),
				size = new Vector3(1f, 0.2f, 1f),
				colorIndex = 2
			});
		}

		private float CalculateHeight(int x, int z)
		{
			float nx = (x + noiseOffsetX) * noiseScale;
			float nz = (z + noiseOffsetZ) * noiseScale;

			float rawNoise = Mathf.PerlinNoise(nx, nz);
			float remappedHeight = Mathf.Pow(rawNoise, heightCurvePower);

			return remappedHeight * heightScale;
		}

		private int GetTerrainColorIndex(float height)
		{
			float normalizedHeight = height / heightScale;

			if (normalizedHeight < 0.3f)
				return 0;

			if (normalizedHeight < 0.6f)
				return 1;

			return 3;
		}

		private void AddTerrainColumn(int x, int z, float targetHeight, int colorIndex)
		{
			int voxelHeight = Mathf.Max(1, Mathf.RoundToInt(targetHeight));

			for (int y = 0; y < voxelHeight; y++)
			{
				terrain.voxels.Add(new VoxelData
				{
					position = new Vector3(x, y * 0.2f, z),
					size = new Vector3(1f, 0.2f, 1f),
					colorIndex = colorIndex
				});
			}
		}
	}
}