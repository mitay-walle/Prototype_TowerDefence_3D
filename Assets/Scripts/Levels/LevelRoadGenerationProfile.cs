using System;
using System.Collections.Generic;
using System.Linq;
using TD.Plugins.Runtime;
using TD.Voxels;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace TD.Levels
{
	[System.Serializable]
	public class RoadLevelGeneratorProfile : GenerationProfile
	{
		public enum NoiseType { Perlin, Random, Voronoi, Combined }

		public int mapSize = 60;
		public int roadWidth = 2;
		public int minPathsFromBase = 3;
		public int maxPathsFromBase = 5;
		public float heightScale = 8f;
		public float noiseScale = 0.15f;

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
		private HashSet<Vector2Int> roadCells = new HashSet<Vector2Int>();
		private List<Vector2Int> spawnerPositions = new List<Vector2Int>();
		private List<Vector2Int> roadEdges = new List<Vector2Int>();
		private float noiseOffsetX;
		private float noiseOffsetZ;
		private float heightCurvePower;
		private List<Vector2> voronoiPoints = new List<Vector2>();

		protected override void Randomize()
		{
			Random.InitState(Seed);
			Debug.Log($"=== ГЕНЕРАЦИЯ КАРТЫ | SEED: {Seed} ===");

			GenerateColorPalette();

			// Этап 1: Размещение базы
			PlaceBase();
			Debug.Log($"Этап 1: База размещена в ({basePosition.x}, {basePosition.y})");
			PrintMap("ЭТАП 1: БАЗА");

			noiseOffsetX = Random.Range(0f, 1000f);
			noiseOffsetZ = Random.Range(0f, 1000f);
			heightCurvePower = Random.Range(0.5f, 3f);

			GenerateVoronoiPoints();

			roadCells = new HashSet<Vector2Int>();
			spawnerPositions = new List<Vector2Int>();
			roadEdges = new List<Vector2Int>();

			// Этап 2: Генерация путей от базы
			GeneratePathsFromBase();
			Debug.Log($"Этап 2: Сгенерировано {roadEdges.Count} путей от базы");
			PrintMap("ЭТАП 2: ПУТИ ОТ БАЗЫ");

			// Этап 3: Размещение спавнеров
			PlaceSpawners();
			Debug.Log($"Этап 3: Размещено {spawnerPositions.Count} спавнеров");
			PrintMap("ЭТАП 3: СПАВНЕРЫ");
			foreach (Vector2Int p in spawnerPositions)
			{
				Debug.DrawLine(Vector3.up, new(p.x, 0, p.y),Color.black,10);
			}

			// Этап 4: Добавление ветвлений
			AddBranches();
			Debug.Log("Этап 4: Добавлены ветвления");
			PrintMap("ЭТАП 4: ВЕТВЛЕНИЯ");

			// Этап 5: Очистка изолированных клеток
			CleanIsolatedCells();
			Debug.Log("Этап 5: Очищены изолированные клетки");
			PrintMap("ЭТАП 5: ОЧИСТКА");

			// Этап 6: Удаление тупиковых концов
			RemoveDeadEnds();
			Debug.Log("Этап 6: Удалены тупики");
			PrintMap("ЭТАП 6: УДАЛЕНИЕ ТУПИКОВ");

			// Этап 7: Утончение путей
			ThinPaths();
			Debug.Log("Этап 7: Пути утончены");
			PrintMap("ЭТАП 7: УТОНЧЕНИЕ");

			// Этап 8: Финальная проверка связности
			VerifyPathfinding();
			Debug.Log("Этап 8: Проверка связности завершена");
			PrintMap("ФИНАЛ: ГОТОВАЯ КАРТА");

			Debug.Log($"=== ГЕНЕРАЦИЯ ЗАВЕРШЕНА | Дорога: {roadCells.Count} клеток | Спавнеры: {spawnerPositions.Count} ===\n");
		}

		private void PlaceBase()
		{
			int margin = mapSize / 4;
			basePosition = new Vector2Int(Random.Range(-mapSize / 2 + margin, mapSize / 2 - margin),
				Random.Range(-mapSize / 2 + margin, mapSize / 2 - margin));

			// База - небольшая область 3x3
			for (int x = -1; x <= 1; x++)
			{
				for (int z = -1; z <= 1; z++)
				{
					roadCells.Add(new Vector2Int(basePosition.x + x, basePosition.y + z));
				}
			}
		}

		private void GeneratePathsFromBase()
		{
			int pathCount = Random.Range(minPathsFromBase, maxPathsFromBase + 1);

			for (int i = 0; i < pathCount; i++)
			{
				float angle = (360f / pathCount * i + Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
				Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

				GenerateWanderingPath(basePosition, direction);
			}
		}

		private void GenerateWanderingPath(Vector2Int start, Vector2 initialDirection)
		{
			Vector2Int current = start;
			Vector2Int previous = start;
			Vector2 direction = initialDirection;

			int stepCount = 0;
			int maxSteps = mapSize * 2;
			int directionChangeInterval = Random.Range(4, 8);

			while (stepCount < maxSteps && IsInsideMap(current))
			{
				AddRoadCell(current);
				FillDiagonalGaps(current, previous);

				// Меняем направление периодически
				if (stepCount % directionChangeInterval == 0)
				{
					direction = RotateVector(direction, Random.Range(-0.6f, 0.6f));
					directionChangeInterval = Random.Range(4, 8);
				}

				previous = current;
				current = GetNextPositionInDirection(current, direction);

				stepCount++;

				// Если дошли до края карты
				if (IsNearMapEdge(current))
				{
					roadEdges.Add(current);
					break;
				}
			}
		}

		private Vector2Int GetNextPositionInDirection(Vector2Int current, Vector2 direction)
		{
			// Определяем следующую позицию с небольшой рандомизацией
			float randomAngle = Random.Range(-0.3f, 0.3f);
			Vector2 newDir = RotateVector(direction, randomAngle);

			Vector2Int next = new Vector2Int(current.x + Mathf.RoundToInt(newDir.x), current.y + Mathf.RoundToInt(newDir.y));

			// Избегаем существующих дорог (кроме базы)
			if (roadCells.Contains(next) && Vector2Int.Distance(next, basePosition) > 3)
			{
				// Пробуем альтернативные направления
				for (int i = 0; i < 4; i++)
				{
					float testAngle = i * Mathf.PI / 2;
					Vector2 testDir = RotateVector(direction, testAngle);
					Vector2Int testPos = new Vector2Int(current.x + Mathf.RoundToInt(testDir.x), current.y + Mathf.RoundToInt(testDir.y));

					if (!roadCells.Contains(testPos) && IsInsideMap(testPos))
					{
						return testPos;
					}
				}
			}

			return next;
		}

		private Vector2 RotateVector(Vector2 v, float angle)
		{
			float cos = Mathf.Cos(angle);
			float sin = Mathf.Sin(angle);
			return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
		}

		private void PlaceSpawners()
		{
			// Группируем края по сторонам карты
			var edgesBySide = new Dictionary<string, List<Vector2Int>>();
			edgesBySide["north"] = new List<Vector2Int>();
			edgesBySide["south"] = new List<Vector2Int>();
			edgesBySide["east"] = new List<Vector2Int>();
			edgesBySide["west"] = new List<Vector2Int>();

			int halfSize = mapSize / 2;
			int edgeThreshold = halfSize - 5;

			foreach (var edge in roadEdges)
			{
				if (edge.y >= edgeThreshold) edgesBySide["north"].Add(edge);
				else if (edge.y <= -edgeThreshold) edgesBySide["south"].Add(edge);
				else if (edge.x >= edgeThreshold) edgesBySide["east"].Add(edge);
				else if (edge.x <= -edgeThreshold) edgesBySide["west"].Add(edge);
			}

			// Определяем, на какой стороне находится база
			string baseSide = "";
			if (basePosition.y >= edgeThreshold / 2) baseSide = "north";
			else if (basePosition.y <= -edgeThreshold / 2) baseSide = "south";
			else if (basePosition.x >= edgeThreshold / 2) baseSide = "east";
			else if (basePosition.x <= -edgeThreshold / 2) baseSide = "west";

			// Размещаем спавнеры на противоположных сторонах
			foreach (var side in edgesBySide.Keys)
			{
				if (side == baseSide) continue;

				var edges = edgesBySide[side];
				if (edges.Count > 0)
				{
					// Выбираем самую дальнюю точку от базы
					Vector2Int furthest = edges[0];
					float maxDist = Vector2Int.Distance(furthest, basePosition);

					foreach (var edge in edges)
					{
						float dist = Vector2Int.Distance(edge, basePosition);
						if (dist > maxDist)
						{
							maxDist = dist;
							furthest = edge;
						}
					}

					spawnerPositions.Add(furthest);
				}
			}
		}

		private void AddBranches()
		{
			var cellsList = roadCells.ToList();
			int branchCount = Random.Range(2, 4);

			for (int i = 0; i < branchCount; i++)
			{
				// Выбираем случайную точку не слишком близко к базе
				Vector2Int branchStart = cellsList[Random.Range(cellsList.Count / 3, cellsList.Count)];

				// Генерируем короткое ответвление
				Vector2 randomDir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
				int branchLength = Random.Range(5, 12);

				Vector2Int current = branchStart;
				for (int j = 0; j < branchLength; j++)
				{
					current = GetNextPositionInDirection(current, randomDir);
					if (!IsInsideMap(current)) break;

					AddRoadCell(current);

					if (Random.value < 0.3f)
					{
						randomDir = RotateVector(randomDir, Random.Range(-0.5f, 0.5f));
					}
				}
			}
		}

		private void CleanIsolatedCells()
		{
			var toRemove = new HashSet<Vector2Int>();

			foreach (var cell in roadCells)
			{
				// Пропускаем базу и спавнеры
				if (Vector2Int.Distance(cell, basePosition) < 2) continue;
				if (spawnerPositions.Any(s => Vector2Int.Distance(s, cell) < 2)) continue;

				int neighborCount = 0;
				foreach (var neighbor in GetOrthogonalNeighbors(cell))
				{
					if (roadCells.Contains(neighbor))
						neighborCount++;
				}

				if (neighborCount == 0)
					toRemove.Add(cell);
			}

			foreach (var cell in toRemove)
				roadCells.Remove(cell);
		}

		private void RemoveDeadEnds()
		{
			bool changed = true;
			int iterations = 0;
			int maxIterations = 20;

			while (changed && iterations < maxIterations)
			{
				changed = false;
				iterations++;
				var toRemove = new HashSet<Vector2Int>();

				foreach (var cell in roadCells)
				{
					// Защищаем базу и спавнеры
					if (Vector2Int.Distance(cell, basePosition) < 2) continue;
					if (spawnerPositions.Any(s => Vector2Int.Distance(s, cell) < 2)) continue;

					int neighborCount = 0;
					foreach (var neighbor in GetOrthogonalNeighbors(cell))
					{
						if (roadCells.Contains(neighbor))
							neighborCount++;
					}

					// Если только один сосед - это тупик
					if (neighborCount == 1)
					{
						toRemove.Add(cell);
						changed = true;
					}
				}

				foreach (var cell in toRemove)
					roadCells.Remove(cell);
			}
		}

		private void ThinPaths()
		{
			var cellsList = roadCells.ToList();
			var toRemove = new HashSet<Vector2Int>();

			foreach (var cell in cellsList)
			{
				// Защищаем ключевые точки
				if (Vector2Int.Distance(cell, basePosition) < 2) continue;
				if (spawnerPositions.Any(s => Vector2Int.Distance(s, cell) < 2)) continue;

				// Проверяем, можем ли удалить клетку без разрыва пути
				if (CanRemoveWithoutBreaking(cell))
				{
					// Удаляем с вероятностью 30%
					if (Random.value < 0.3f)
					{
						toRemove.Add(cell);
					}
				}
			}

			// Применяем удаление и проверяем связность
			var backup = new HashSet<Vector2Int>(roadCells);
			foreach (var cell in toRemove)
			{
				roadCells.Remove(cell);

				// Если нарушилась связность, возвращаем клетку
				if (!AreAllSpawnersReachable())
				{
					roadCells.Add(cell);
				}
			}
		}

		private bool CanRemoveWithoutBreaking(Vector2Int cell)
		{
			var neighbors = GetOrthogonalNeighbors(cell).Where(n => roadCells.Contains(n)).ToList();

			// Если меньше 2 соседей - это край или тупик, не трогаем
			if (neighbors.Count < 2) return false;

			// Если 2 соседа - проверяем, не на одной ли они линии
			if (neighbors.Count == 2)
			{
				var n1 = neighbors[0];
				var n2 = neighbors[1];

				// Если соседи на одной линии (по X или Y), клетка избыточна
				if (n1.x == n2.x || n1.y == n2.y)
					return true;
			}

			// Если больше 2 соседей - перекресток, не удаляем
			return false;
		}

		private bool AreAllSpawnersReachable()
		{
			if (spawnerPositions.Count == 0) return true;

			foreach (var spawner in spawnerPositions)
			{
				if (!IsPathExists(basePosition, spawner))
					return false;
			}

			return true;
		}

		private void VerifyPathfinding()
		{
			foreach (var spawner in spawnerPositions)
			{
				if (!IsPathExists(basePosition, spawner))
				{
					Debug.LogWarning($"Спавнер {spawner} недостижим от базы! Соединяю...");
					ConnectTwoPoints(basePosition, spawner);
				}
			}
		}

		private bool IsPathExists(Vector2Int from, Vector2Int to)
		{
			var visited = new HashSet<Vector2Int>();
			var queue = new Queue<Vector2Int>();
			queue.Enqueue(from);
			visited.Add(from);

			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				if (current == to) return true;

				foreach (var neighbor in GetOrthogonalNeighbors(current))
				{
					if (roadCells.Contains(neighbor) && !visited.Contains(neighbor))
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

			while (current != to)
			{
				AddRoadCell(current);

				int dx = Math.Sign(to.x - current.x);
				int dy = Math.Sign(to.y - current.y);

				if (Random.value < 0.5f && dx != 0)
					current.x += dx;
				else if (dy != 0)
					current.y += dy;
				else if (dx != 0)
					current.x += dx;
			}

			AddRoadCell(to);
		}

		private IEnumerable<Vector2Int> GetOrthogonalNeighbors(Vector2Int pos)
		{
			yield return new Vector2Int(pos.x + 1, pos.y);
			yield return new Vector2Int(pos.x - 1, pos.y);
			yield return new Vector2Int(pos.x, pos.y + 1);
			yield return new Vector2Int(pos.x, pos.y - 1);
		}

		private void FillDiagonalGaps(Vector2Int current, Vector2Int previous)
		{
			if (current == previous) return;

			int dx = current.x - previous.x;
			int dy = current.y - previous.y;

			if (Mathf.Abs(dx) == 1 && Mathf.Abs(dy) == 1)
			{
				AddRoadCell(new Vector2Int(previous.x + dx, previous.y));
				AddRoadCell(new Vector2Int(previous.x, previous.y + dy));
			}
		}

		private void AddRoadCell(Vector2Int pos)
		{
			roadCells.Add(pos);
		}

		private bool IsInsideMap(Vector2Int pos)
		{
			int halfSize = mapSize / 2;
			return pos.x >= -halfSize && pos.x < halfSize && pos.y >= -halfSize && pos.y < halfSize;
		}

		private bool IsNearMapEdge(Vector2Int pos)
		{
			int halfSize = mapSize / 2;
			int edgeThreshold = halfSize - 3;
			return Mathf.Abs(pos.x) >= edgeThreshold || Mathf.Abs(pos.y) >= edgeThreshold;
		}

		private void PrintMap(string title)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.AppendLine($"\n{title}");
			sb.AppendLine(new string('=', mapSize + 2));

			int halfSize = mapSize / 2;

			for (int z = halfSize - 1; z >= -halfSize; z--)
			{
				for (int x = -halfSize; x < halfSize; x++)
				{
					Vector2Int pos = new Vector2Int(x, z);

					if (Vector2Int.Distance(pos, basePosition) < 1.5f)
						sb.Append("■"); // База
					else if (spawnerPositions.Contains(pos))
						sb.Append("▲"); // Спавнер
					else if (roadCells.Contains(pos))
						sb.Append("▓"); // Дорога
					else
						sb.Append("░"); // Terrain
				}

				sb.AppendLine();
			}

			sb.AppendLine(new string('=', mapSize + 2));
			Debug.Log(sb.ToString());
		}

		// Остальные методы без изменений
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

			foreach (var spawner in spawnerPositions)
			{
				GameObject instance = Object.Instantiate(roadEdgePrefab, edgesParent);
				instance.transform.localPosition = new Vector3(spawner.x, 0, spawner.y);
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

		public Vector3 BasePosition => new Vector3(basePosition.x, 0, basePosition.y);

		public IReadOnlyList<Vector2Int> RoadOuterCells
		{
			get
			{
				if (_cachedRoadOuterCells == null)
					_cachedRoadOuterCells = new List<Vector2Int>(spawnerPositions);

				return _cachedRoadOuterCells;
			}
		}

		public IReadOnlyList<Vector3> RoadOuterVoxelsWorldPositions
		{
			get
			{
				if (_cachedRoadOuterWorldPositions == null)
				{
					var list = new List<Vector3>();
					foreach (var s in spawnerPositions)
						list.Add(new Vector3(s.x, 0f, s.y));

					_cachedRoadOuterWorldPositions = list;
				}

				return _cachedRoadOuterWorldPositions;
			}
		}

		private List<Vector2Int> _cachedRoadOuterCells;
		private List<Vector3> _cachedRoadOuterWorldPositions;
	}
}