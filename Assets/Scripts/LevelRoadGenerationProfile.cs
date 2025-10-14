using System.Collections.Generic;
using UnityEngine;

namespace TD
{
	[System.Serializable]
	public class LevelRoadGenerationProfile : GenerationProfile
	{
		public enum NoiseType { Perlin, Random, Voronoi, Combined }

		public int mapSize = 60;
		public int roadWidth = 1;
		public int pathCount = 3;
		public float heightScale = 8f;
		public float noiseScale = 0.08f;

		public NoiseType terrainNoiseType = NoiseType.Perlin;
		public Texture2D heightmapTexture;
		public float textureMixStrength = 0.5f;
		public float voronoiCellSize = 15f;
		public int voronoiPointCount = 15;

		public int terrainLayer = 0;
		public int roadLayer = 0;

		public List<Color> colorPalette = new List<Color>();
		public List<Color> emissionPalette = new List<Color>();

		public Part terrain = new Part { name = "Terrain" };
		public Part road = new Part { name = "Road" };

		private Vector2Int basePosition;
		private HashSet<Vector2Int> roadCells;
		private float noiseOffsetX;
		private float noiseOffsetZ;
		private float heightCurvePower;
		private List<Vector2> voronoiPoints;

		protected override void Randomize()
		{
			Random.InitState(Seed);
			GenerateColorPalette();

			int margin = mapSize / 4;
			basePosition = new Vector2Int(Random.Range(-mapSize / 2 + margin, mapSize / 2 - margin),
				Random.Range(-mapSize / 2 + margin, mapSize / 2 - margin));

			noiseOffsetX = Random.Range(0f, 1000f);
			noiseOffsetZ = Random.Range(0f, 1000f);
			heightCurvePower = Random.Range(0.5f, 3f);

			GenerateVoronoiPoints();

			roadCells = new HashSet<Vector2Int>();
			GenerateRoadPaths();
		}

		public override void Generate(VoxelGenerator generator)
		{
			base.Generate(generator);

			terrain.voxels.Clear();
			road.voxels.Clear();

			GenerateTerrain();
			GenerateRoad();

			GeneratePart(generator, terrain, generator.transform, Vector3.zero);
			GeneratePart(generator, road, generator.transform, Vector3.zero);

			SetPartLayer(generator.transform, "Terrain", terrainLayer);
			SetPartLayer(generator.transform, "Road", roadLayer);
		}

		private void SetPartLayer(Transform parent, string partName, int layer)
		{
			Transform found = parent.Find(partName);
			if (found)
			{
				Transform combined = found.Find("Combined");
				if (combined) combined.gameObject.layer = layer;
			}
		}

		private void GenerateColorPalette()
		{
			colorPalette.Clear();
			emissionPalette.Clear();

			Color baseColor = RandomColor(0.2f, 0.5f);
			Color accentColor = RandomColor(0.3f, 0.6f);
			Color darkColor = RandomColor(0.1f, 0.3f);
			Color roadColor = RandomColor(0.15f, 0.25f);

			colorPalette.Add(baseColor);
			colorPalette.Add(accentColor);
			colorPalette.Add(darkColor);
			colorPalette.Add(Color.Lerp(baseColor, accentColor, 0.5f));
			colorPalette.Add(roadColor);

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

		private void GenerateVoronoiPoints()
		{
			voronoiPoints = new List<Vector2>();
			for (int i = 0; i < voronoiPointCount; i++)
			{
				voronoiPoints.Add(new Vector2(Random.Range(-mapSize / 2f, mapSize / 2f), Random.Range(-mapSize / 2f, mapSize / 2f)));
			}
		}

		private void GenerateRoadPaths()
		{
			int baseSize = 8;
			for (int x = -baseSize / 2; x <= baseSize / 2; x++)
			{
				for (int z = -baseSize / 2; z <= baseSize / 2; z++)
				{
					roadCells.Add(new Vector2Int(basePosition.x + x, basePosition.y + z));
				}
			}

			for (int i = 0; i < pathCount; i++)
			{
				float angle = (360f / pathCount * i + Random.Range(-30f, 30f)) * Mathf.Deg2Rad;
				GeneratePath(basePosition, angle);
			}
		}

		private void GeneratePath(Vector2Int start, float direction)
		{
			Vector2Int current = start;
			Vector2Int previous = start;
			Vector2 dir = new Vector2(Mathf.Cos(direction), Mathf.Sin(direction));

			float waveFrequency = Random.Range(0.05f, 0.15f);
			float waveAmplitude = Random.Range(3f, 8f);
			float distance = 0;

			while (IsInsideMap(current))
			{
				AddRoadCell(current);

				// Заполняем диагональные переходы для A*
				if (current != previous)
				{
					int dx = current.x - previous.x;
					int dz = current.y - previous.y;

					// Если движение по диагонали, добавляем соседние клетки
					if (Mathf.Abs(dx) == 1 && Mathf.Abs(dz) == 1)
					{
						AddRoadCell(new Vector2Int(previous.x + dx, previous.y));
						AddRoadCell(new Vector2Int(previous.x, previous.y + dz));
					}
				}

				previous = current;

				float wave = Mathf.Sin(distance * waveFrequency) * waveAmplitude;
				Vector2 perpendicular = new Vector2(-dir.y, dir.x);

				Vector2 offset = dir + perpendicular * wave * 0.1f;
				offset.Normalize();

				current.x += Mathf.RoundToInt(offset.x);
				current.y += Mathf.RoundToInt(offset.y);
				distance++;

				if (Random.value < 0.1f)
				{
					direction += Random.Range(-0.3f, 0.3f);
					dir = new Vector2(Mathf.Cos(direction), Mathf.Sin(direction));
				}
			}
		}

		private void AddRoadCell(Vector2Int pos)
		{
			for (int dx = -roadWidth / 2; dx <= roadWidth / 2; dx++)
			{
				for (int dz = -roadWidth / 2; dz <= roadWidth / 2; dz++)
				{
					roadCells.Add(new Vector2Int(pos.x + dx, pos.y + dz));
				}
			}
		}

		private bool IsInsideMap(Vector2Int pos)
		{
			int halfSize = mapSize / 2;
			return pos.x >= -halfSize && pos.x < halfSize && pos.y >= -halfSize && pos.y < halfSize;
		}

		private void GenerateTerrain()
		{
			int halfSize = mapSize / 2;

			for (int x = -halfSize; x < halfSize; x++)
			{
				for (int z = -halfSize; z < halfSize; z++)
				{
					Vector2Int pos = new Vector2Int(x, z);

					if (roadCells.Contains(pos)) continue;

					float height = CalculateHeight(x, z);
					int colorIndex = GetTerrainColorIndex(height);
					AddTerrainColumn(terrain, x, z, height, colorIndex);
				}
			}
		}

		private void GenerateRoad()
		{
			foreach (var pos in roadCells)
			{
				road.voxels.Add(new VoxelData
				{
					position = new Vector3(pos.x, 0, pos.y),
					size = new Vector3(1f, 0.2f, 1f),
					colorIndex = 4
				});
			}
		}

		private float CalculateHeight(int x, int z)
		{
			float height = 0f;

			switch (terrainNoiseType)
			{
				case NoiseType.Perlin:
					height = CalculatePerlinHeight(x, z);
					break;

				case NoiseType.Random:
					height = CalculateRandomHeight(x, z);
					break;

				case NoiseType.Voronoi:
					height = CalculateVoronoiHeight(x, z);
					break;

				case NoiseType.Combined:
					height = CalculateCombinedHeight(x, z);
					break;
			}

			if (heightmapTexture != null)
			{
				float textureHeight = SampleHeightmap(x, z);
				height = Mathf.Lerp(height, textureHeight, textureMixStrength);
			}

			return height;
		}

		private float CalculatePerlinHeight(int x, int z)
		{
			float nx = (x + noiseOffsetX) * noiseScale;
			float nz = (z + noiseOffsetZ) * noiseScale;

			float rawNoise = Mathf.PerlinNoise(nx, nz);
			float remappedHeight = Mathf.Pow(rawNoise, heightCurvePower);

			return remappedHeight * heightScale;
		}

		private float CalculateRandomHeight(int x, int z)
		{
			int hash = Seed + x * 73856093 ^ z * 19349663;
			float t = Mathf.Sin(hash) * 43758.5453f;
			float height = t - Mathf.Floor(t);

			return Mathf.Pow(height, heightCurvePower) * heightScale;
		}

		private float CalculateVoronoiHeight(int x, int z)
		{
			Vector2 pos = new Vector2(x, z);
			float minDist = float.MaxValue;
			int closestIndex = 0;

			for (int i = 0; i < voronoiPoints.Count; i++)
			{
				float dist = Vector2.Distance(pos, voronoiPoints[i]);
				if (dist < minDist)
				{
					minDist = dist;
					closestIndex = i;
				}
			}

			// Используем индекс ближайшей точки и расстояние для высоты
			float normalizedDist = Mathf.Clamp01(minDist / voronoiCellSize);
			float heightValue = (closestIndex % 5) / 5f + normalizedDist * 0.3f;

			return Mathf.Pow(heightValue, heightCurvePower) * heightScale;
		}

		private float CalculateCombinedHeight(int x, int z)
		{
			float perlin = CalculatePerlinHeight(x, z) / heightScale;

			float nx2 = (x + noiseOffsetX * 2) * noiseScale * 2f;
			float nz2 = (z + noiseOffsetZ * 2) * noiseScale * 2f;
			float detailNoise = Mathf.PerlinNoise(nx2, nz2) * 0.3f;

			Vector2 pos = new Vector2(x, z);
			float minDist = float.MaxValue;
			foreach (var point in voronoiPoints)
			{
				float dist = Vector2.Distance(pos, point);
				minDist = Mathf.Min(minDist, dist);
			}

			float voronoiInfluence = Mathf.Clamp01(minDist / voronoiCellSize) * 0.2f;

			float combined = perlin + detailNoise + voronoiInfluence;
			return Mathf.Pow(combined, heightCurvePower) * heightScale;
		}

		private float SampleHeightmap(int x, int z)
		{
			int halfSize = mapSize / 2;
			float u = (x + halfSize) / (float)mapSize;
			float v = (z + halfSize) / (float)mapSize;

			int px = Mathf.Clamp(Mathf.FloorToInt(u * heightmapTexture.width), 0, heightmapTexture.width - 1);
			int py = Mathf.Clamp(Mathf.FloorToInt(v * heightmapTexture.height), 0, heightmapTexture.height - 1);

			Color pixel = heightmapTexture.GetPixel(px, py);
			return pixel.grayscale * heightScale;
		}

		private int GetTerrainColorIndex(float height)
		{
			float normalizedHeight = height / heightScale;

			if (normalizedHeight < 0.3f) return 0;
			if (normalizedHeight < 0.6f) return 1;

			return 3;
		}

		private void AddTerrainColumn(Part part, int x, int z, float targetHeight, int colorIndex)
		{
			int voxelHeight = Mathf.Max(1, Mathf.RoundToInt(targetHeight));

			for (int y = 0; y < voxelHeight; y++)
			{
				part.voxels.Add(new VoxelData
				{
					position = new Vector3(x, y * 0.2f, z),
					size = new Vector3(1f, 0.2f, 1f),
					colorIndex = colorIndex
				});
			}
		}
	}
}