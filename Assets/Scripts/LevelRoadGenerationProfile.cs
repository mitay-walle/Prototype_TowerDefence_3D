using System;
using System.Collections.Generic;
using TD.Plugins.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace TD
{
	[System.Serializable]
	public class LevelRoadGenerationProfile : GenerationProfile
	{
		public enum NoiseType { Perlin, Random, Voronoi, Combined }
		public enum PathType { Straight, Loop, SCurve, Branch }

		public int mapSize = 60;
		public int roadWidth = 1;
		public int pathCount = 3;
		public float heightScale = 8f;
		public float noiseScale = 0.08f;

		[Range(0f, 1f)] public float loopChance = 0.3f;
		[Range(0f, 1f)] public float sCurveChance = 0.3f;
		[Range(0f, 1f)] public float intersectionChance = 0.4f;

		public GameObject roadEdgePrefab;

		public NoiseType terrainNoiseType = NoiseType.Perlin;
		public Texture2D heightmapTexture;
		public float textureMixStrength = 0.5f;
		public float voronoiCellSize = 15f;
		public int voronoiPointCount = 15;

		[Layer] public int terrainLayer = 0;
		[Layer] public int roadLayer = 0;

		public List<Color> colorPalette = new List<Color>();
		public List<Color> emissionPalette = new List<Color>();

		public Part terrain = new Part { name = "Terrain" };
		public Part road = new Part { name = "Road" };

		private Vector2Int basePosition;
		private HashSet<Vector2Int> roadCells;
		private List<Vector2Int> roadEdges;
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
			roadEdges = new List<Vector2Int>();
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

			if (roadEdgePrefab != null)
			{
				PlaceRoadEdgePrefabs(generator.transform);
			}
		}

		private void PlaceRoadEdgePrefabs(Transform parent)
		{
			Transform edgesParent = parent.Find("RoadEdges");
			if (edgesParent != null)
			{
				Object.DestroyImmediate(edgesParent.gameObject);
			}

			edgesParent = new GameObject("RoadEdges").transform;
			edgesParent.SetParent(parent);
			edgesParent.localPosition = Vector3.zero;

			foreach (var edge in roadEdges)
			{
				GameObject instance = Object.Instantiate(roadEdgePrefab, edgesParent);
				instance.transform.localPosition = new Vector3(edge.x, 0, edge.y);
			}
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
		int baseSize = 6;
		for (int x = -baseSize / 2; x <= baseSize / 2; x++)
		{
			for (int z = -baseSize / 2; z <= baseSize / 2; z++)
			{
				roadCells.Add(new Vector2Int(basePosition.x + x, basePosition.y + z));
			}
		}

		var pathQueue = new System.Collections.Generic.Queue<(Vector2Int start, float direction, int depth)>();
		for (int i = 0; i < pathCount; i++)
		{
			float angleBase = 360f / pathCount * i;
			float angle = (angleBase + Random.Range(-30f, 30f)) * Mathf.Deg2Rad;
			pathQueue.Enqueue((basePosition, angle, 0));
		}

		while (pathQueue.Count > 0)
		{
			var (start, direction, depth) = pathQueue.Dequeue();
			if (depth > 2) continue;

			GenerateRandomWalkPath(start, direction, pathQueue, depth);
		}

		EnsureRoadConnectivity();
		SimplifyRoadEdges();
	}

		private void GenerateRandomWalkPath(Vector2Int start,
		                                    float initialDirection,
		                                    System.Collections.Generic.Queue<(Vector2Int, float, int)> pathQueue,
		                                    int depth)
		{
			var current = start;
			var previous = start;
			var direction = initialDirection;
			int distance = 0;

			while (IsInsideMap(current) && distance < mapSize * 2)
			{
				AddRoadCell(current);
				FillDiagonalGaps(current, previous);

				int midpoint = mapSize;
				if (distance == midpoint && depth < 2)
				{
					for (int b = 0; b < 2; b++)
					{
						float branchAngle = direction + Random.Range(-0.7f, 0.7f);
						pathQueue.Enqueue((current, branchAngle, depth + 1));
					}
				}

				if (distance % 15 == 0 && Random.value < 0.4f)
				{
					if (Random.value < 0.5f)
						GenerateRectangularRing(current);
					else
						GenerateDiamondRing(current);
				}

				if (distance % 20 == 0 && Random.value < 0.3f)
				{
					GenerateSmallLabyrinth(current);
				}

				previous = current;

				var neighbors = GetRandomNeighbor(current, direction);
				if (neighbors != current)
				{
					direction += Random.Range(-0.2f, 0.2f);
					current = neighbors;
					distance++;
				}
				else
					break;
			}

			roadEdges.Add(current);
		}

		private Vector2Int GetRandomNeighbor(Vector2Int current, float preferredDirection)
		{
			var neighbors = new Vector2Int[]
			{
				new Vector2Int(current.x + 1, current.y),
				new Vector2Int(current.x - 1, current.y),
				new Vector2Int(current.x, current.y + 1),
				new Vector2Int(current.x, current.y - 1)
			};

			Vector2Int best = current;
			float bestScore = -1f;

			foreach (var neighbor in neighbors)
			{
				if (!IsInsideMap(neighbor)) continue;

				var direction = ((Vector2)neighbor - (Vector2)current).normalized;
				var dirAngle = Mathf.Atan2(direction.y, direction.x);
				var score = Mathf.Cos(dirAngle - preferredDirection) + Random.Range(-0.3f, 0.3f);

				if (score > bestScore)
				{
					bestScore = score;
					best = neighbor;
				}
			}

			return best;
		}

		private void GenerateRectangularRing(Vector2Int center)
		{
			int size = Random.Range(4, 8);
			for (int x = -size; x <= size; x++)
			{
				for (int z = -size; z <= size; z++)
				{
					if ((x == -size || x == size || z == -size || z == size))
					{
						Vector2Int pos = new Vector2Int(center.x + x, center.y + z);
						if (IsInsideMap(pos))
							AddRoadCell(pos);
					}
				}
			}
		}

		private void GenerateDiamondRing(Vector2Int center)
		{
			int radius = Random.Range(3, 6);
			for (int x = -radius; x <= radius; x++)
			{
				for (int z = -radius; z <= radius; z++)
				{
					if (Mathf.Abs(x) + Mathf.Abs(z) == radius)
					{
						Vector2Int pos = new Vector2Int(center.x + x, center.y + z);
						if (IsInsideMap(pos))
							AddRoadCell(pos);
					}
				}
			}
		}

		private void GenerateSmallLabyrinth(Vector2Int center)
		{
			int size = 5;
			var maze = new bool[size, size];

			for (int x = 0; x < size; x++)
			{
				for (int z = 0; z < size; z++)
				{
					maze[x, z] = Random.value < 0.4f;
				}
			}

			for (int x = 0; x < size; x++)
			{
				for (int z = 0; z < size; z++)
				{
					if (maze[x, z])
					{
						Vector2Int pos = new Vector2Int(center.x + x - size / 2, center.y + z - size / 2);
						if (IsInsideMap(pos))
							AddRoadCell(pos);
					}
				}
			}
		}

		private void EnsureAllPathsConnected(List<Vector2Int> endpoints)
		{
			foreach (var endpoint in endpoints)
			{
				if (!IsRoadConnected(basePosition, endpoint))
				{
					ConnectTwoPoints(basePosition, endpoint);
				}
			}
		}

		private void EnsureRoadConnectivity()
		{
			var cellsToRemove = new HashSet<Vector2Int>();

			foreach (var cell in roadCells)
			{
				var xzNeighbors = new Vector2Int[]
				{
					new Vector2Int(cell.x + 1, cell.y),
					new Vector2Int(cell.x - 1, cell.y),
					new Vector2Int(cell.x, cell.y + 1),
					new Vector2Int(cell.x, cell.y - 1)
				};

				bool hasNeighbor = false;
				foreach (var neighbor in xzNeighbors)
				{
					if (roadCells.Contains(neighbor))
					{
						hasNeighbor = true;
						break;
					}
				}

				if (!hasNeighbor)
					cellsToRemove.Add(cell);
			}

			foreach (var cell in cellsToRemove)
				roadCells.Remove(cell);
		}

		private PathType ChoosePathType()
		{
			float roll = Random.value;

			if (roll < loopChance)
				return PathType.Loop;
			else if (roll < loopChance + sCurveChance)
				return PathType.SCurve;
			else if (roll < loopChance + sCurveChance + intersectionChance)
				return PathType.Branch;
			else
				return PathType.Straight;
		}

		private Vector2Int GenerateStraightPath(Vector2Int start, float direction, int maxLength = -1)
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
				FillDiagonalGaps(current, previous);

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

				if (maxLength > 0 && distance >= maxLength)
					break;
			}

			roadEdges.Add(current);
			return current;
		}

		private void GenerateLoopPath(Vector2Int start, float startDirection)
		{
			Vector2Int current = start;
			Vector2Int previous = start;

			float radius = Random.Range(15f, 25f);
			float angle = startDirection;
			float angleStep = Random.Range(0.15f, 0.25f);
			int steps = Mathf.RoundToInt(Mathf.PI * 2f / angleStep);

			Vector2 center = new Vector2(start.x, start.y) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

			for (int i = 0; i < steps; i++)
			{
				angle += angleStep;
				current = new Vector2Int(Mathf.RoundToInt(center.x + Mathf.Cos(angle) * radius),
					Mathf.RoundToInt(center.y + Mathf.Sin(angle) * radius));

				if (!IsInsideMap(current)) break;

				AddRoadCell(current);
				FillDiagonalGaps(current, previous);
				previous = current;
			}
		}

		private void GenerateSCurvePath(Vector2Int start, float direction)
		{
			Vector2Int current = start;
			Vector2Int previous = start;
			Vector2 dir = new Vector2(Mathf.Cos(direction), Mathf.Sin(direction));

			float curveStrength = Random.Range(0.3f, 0.6f);
			float distance = 0;

			while (IsInsideMap(current))
			{
				AddRoadCell(current);
				FillDiagonalGaps(current, previous);

				previous = current;

				// S-образная кривая
				float sCurve = Mathf.Sin(distance * 0.1f) * curveStrength;
				Vector2 perpendicular = new Vector2(-dir.y, dir.x);

				Vector2 offset = dir + perpendicular * sCurve;
				offset.Normalize();

				current.x += Mathf.RoundToInt(offset.x);
				current.y += Mathf.RoundToInt(offset.y);
				distance++;

				if (distance > 40) break;
			}

			roadEdges.Add(current);
		}

		private void FillDiagonalGaps(Vector2Int current, Vector2Int previous)
		{
			if (current == previous) return;

			int dx = current.x - previous.x;
			int dz = current.y - previous.y;

			if (Mathf.Abs(dx) == 1 && Mathf.Abs(dz) == 1)
			{
				AddRoadCell(new Vector2Int(previous.x + dx, previous.y));
				AddRoadCell(new Vector2Int(previous.x, previous.y + dz));
			}
		}

		private void ConnectDisconnectedSegments(List<Vector2Int> connectedPoints)
		{
			if (connectedPoints.Count <= 1) return;

			HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
			foreach (var point in connectedPoints)
			{
				visited.Add(point);
			}

			for (int i = 1; i < connectedPoints.Count; i++)
			{
				if (!IsRoadConnected(connectedPoints[i - 1], connectedPoints[i]))
				{
					ConnectTwoPoints(connectedPoints[i - 1], connectedPoints[i]);
				}
			}
		}

		private bool IsRoadConnected(Vector2Int from, Vector2Int to)
		{
			if (roadCells.Contains(from) && roadCells.Contains(to))
			{
				var visited = new HashSet<Vector2Int>();
				return BfsPathExists(from, to, visited);
			}

			return false;
		}

		private bool BfsPathExists(Vector2Int from, Vector2Int to, HashSet<Vector2Int> visited)
		{
			var queue = new System.Collections.Generic.Queue<Vector2Int>();
			queue.Enqueue(from);
			visited.Add(from);

			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				if (current == to) return true;

				var neighbors = new Vector2Int[]
				{
					new Vector2Int(current.x + 1, current.y),
					new Vector2Int(current.x - 1, current.y),
					new Vector2Int(current.x, current.y + 1),
					new Vector2Int(current.x, current.y - 1)
				};

				foreach (var neighbor in neighbors)
				{
					if (!visited.Contains(neighbor) && roadCells.Contains(neighbor))
					{
						visited.Add(neighbor);
						queue.Enqueue(neighbor);
					}
				}
			}

			return false;
		}

		private void ConnectTwoPoints(Vector2Int from, Vector2Int to)
		{
			Vector2Int current = from;
			int dx = Math.Sign(to.x - from.x);
			int dz = Math.Sign(to.y - from.y);

			while (current != to)
			{
				AddRoadCell(current);

				if (Random.value < 0.5f && current.x != to.x)
					current.x += dx;
				else if (current.y != to.y)
					current.y += dz;
				else if (current.x != to.x)
					current.x += dx;
			}

			AddRoadCell(to);
		}

		private void EnsureRoadContinuity()
		{
			var cellsToAdd = new List<Vector2Int>();

			foreach (var cell in roadCells)
			{
				var neighbors = new Vector2Int[]
				{
					new Vector2Int(cell.x + 1, cell.y),
					new Vector2Int(cell.x - 1, cell.y),
					new Vector2Int(cell.x, cell.y + 1),
					new Vector2Int(cell.x, cell.y - 1),
					new Vector2Int(cell.x + 1, cell.y + 1),
					new Vector2Int(cell.x - 1, cell.y - 1),
					new Vector2Int(cell.x + 1, cell.y - 1),
					new Vector2Int(cell.x - 1, cell.y + 1)
				};

				int adjacentCount = 0;
				foreach (var neighbor in neighbors)
				{
					if (roadCells.Contains(neighbor))
						adjacentCount++;
				}

				if (adjacentCount == 1 && Random.value < 0.3f)
				{
					foreach (var neighbor in neighbors)
					{
						if (!roadCells.Contains(neighbor) && IsInsideMap(neighbor))
						{
							cellsToAdd.Add(neighbor);
							break;
						}
					}
				}
			}

			foreach (var cell in cellsToAdd)
			{
				AddRoadCell(cell);
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

		/// <summary>
		/// Центральная (базовая) позиция генерации.
		/// </summary>
		public Vector3 BasePosition => new Vector3(basePosition.x, 0, basePosition.y);

		/// <summary>
		/// Возвращает ячейки дороги, находящиеся на внешней границе карты.
		/// </summary>
		public IReadOnlyList<Vector2Int> RoadOuterCells
		{
			get
			{
				if (_cachedRoadOuterCells == null)
					_cachedRoadOuterCells = CalculateMapEdgeRoadCells();

				return _cachedRoadOuterCells;
			}
		}

		/// <summary>
		/// Возвращает мировые позиции вокселей на внешней границе карты (дороги).
		/// </summary>
		public IReadOnlyList<Vector3> RoadOuterVoxelsWorldPositions
		{
			get
			{
				if (_cachedRoadOuterWorldPositions == null)
				{
					var list = new List<Vector3>();
					foreach (var c in RoadOuterCells)
						list.Add(new Vector3(c.x, 0f, c.y));

					_cachedRoadOuterWorldPositions = list;
				}

				return _cachedRoadOuterWorldPositions;
			}
		}

private void SimplifyRoadEdges()
	{
		if (roadEdges.Count <= 2) return;

		var simplified = new List<Vector2Int>();
		simplified.Add(roadEdges[0]);

		float minDistanceThreshold = 5f;
		float angleThreshold = 30f * Mathf.Deg2Rad;

		for (int i = 1; i < roadEdges.Count - 1; i++)
		{
			Vector2Int prev = roadEdges[i - 1];
			Vector2Int curr = roadEdges[i];
			Vector2Int next = roadEdges[i + 1];

			Vector2 dir1 = ((Vector2)curr - (Vector2)prev).normalized;
			Vector2 dir2 = ((Vector2)next - (Vector2)curr).normalized;

			float angle = Mathf.Abs(Mathf.Atan2(dir1.x * dir2.y - dir1.y * dir2.x, Vector2.Dot(dir1, dir2)));
			float distToPrev = Vector2.Distance(prev, curr);
			float distToNext = Vector2.Distance(curr, next);

			if (angle < angleThreshold || (distToPrev < minDistanceThreshold && distToNext < minDistanceThreshold))
				continue;

			simplified.Add(curr);
		}

		simplified.Add(roadEdges[roadEdges.Count - 1]);
		roadEdges = simplified;
		_cachedRoadOuterCells = null;
		_cachedRoadOuterWorldPositions = null;
	}

		private List<Vector2Int> _cachedRoadOuterCells;
		private List<Vector3> _cachedRoadOuterWorldPositions;

		/// <summary>
		/// Возвращает ячейки дороги, которые находятся на краю карты (x или y равны ±mapSize/2 - 1).
		/// </summary>
		private List<Vector2Int> CalculateMapEdgeRoadCells()
		{
			var result = new List<Vector2Int>();
			if (roadCells == null || roadCells.Count == 0)
				return result;

			int half = mapSize / 2;
			int edgeMin = -half;
			int edgeMax = half - 1;

			foreach (var cell in roadCells)
			{
				if (cell.x <= edgeMin || cell.x >= edgeMax || cell.y <= edgeMin || cell.y >= edgeMax)
				{
					result.Add(cell);
				}
			}

			return result;
		}
	}
}