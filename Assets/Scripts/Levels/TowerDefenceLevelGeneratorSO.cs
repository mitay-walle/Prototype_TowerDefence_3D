using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "TowerDefenceLevelGenerator", menuName = "TowerDefence/Level Generator")]
public class TowerDefenceLevelGeneratorSO : ScriptableObject
{
	[Title("Map Settings")]
	[MinValue(5)]
	public int width = 20;
	[MinValue(5)]
	public int height = 20;

	[LabelText("Random Seed (0 = random)")]
	public int seed = 0;

	[ShowInInspector, ReadOnly]
	private Dictionary<Vector2Int, bool> generatedMap;

	[Button(ButtonSizes.Large)]
	public void GenerateMap()
	{
		if (seed != 0)
			Random.InitState(seed);
		else
			Random.InitState(System.Environment.TickCount);

		generatedMap = new Dictionary<Vector2Int, bool>();
		var path = new List<Vector2Int>();

		// --- Инициализация карты ---
		for (int y = 0; y < height; y++)
			for (int x = 0; x < width; x++)
				generatedMap[new Vector2Int(x, y)] = false;

		// --- Старт и финиш (внутри границ, не у краёв) ---
		Vector2Int start = new(1, Random.Range(1, height - 1));
		Vector2Int end = new(width - 2, Random.Range(1, height - 1));

		// --- Основной путь ---
		Vector2Int current = start;
		path.Add(current);

		while (current != end)
		{
			Vector2Int dir = Vector2Int.zero;
			int dx = end.x - current.x;
			int dy = end.y - current.y;

			if (Random.value < 0.65f && dx != 0)
				dir = new Vector2Int((int)Mathf.Sign(dx), 0);
			else if (dy != 0)
				dir = new Vector2Int(0, (int)Mathf.Sign(dy));
			else
				dir = new Vector2Int((int)Mathf.Sign(dx), 0);

			Vector2Int next = current + dir;
			next.x = Mathf.Clamp(next.x, 1, width - 2);
			next.y = Mathf.Clamp(next.y, 1, height - 2);

			if (next == current || path.Contains(next))
			{
				var alt = new List<Vector2Int>()
				{
					new(current.x + 1, current.y),
					new(current.x - 1, current.y),
					new(current.x, current.y + 1),
					new(current.x, current.y - 1)
				};

				alt.RemoveAll(p => p.x <= 0 || p.x >= width - 1 || p.y <= 0 || p.y >= height - 1);
				next = alt[Random.Range(0, alt.Count)];
			}

			path.Add(next);
			current = next;
		}

		// --- Помечаем основную дорогу ---
		foreach (var pos in path)
			generatedMap[pos] = true;

		// --- Генерация ответвлений ---
		int branchCount = Random.Range(2, 6);
		for (int i = 0; i < branchCount; i++)
		{
			// выбираем случайную точку дороги
			var root = path[Random.Range(3, path.Count - 3)];
			Vector2Int dir = Vector2Int.zero;

			// выбираем направление к краю, где нет основной дороги
			if (root.y > height / 2) dir = Vector2Int.down;
			else dir = Vector2Int.up;

			// 25% шанс сделать горизонтальное ответвление
			if (Random.value < 0.25f)
				dir = (Random.value < 0.5f) ? Vector2Int.left : Vector2Int.right;

			Vector2Int p = root + dir;
			List<Vector2Int> branch = new();

			while (p.x > 0 && p.x < width - 1 && p.y > 0 && p.y < height - 1)
			{
				// если наткнулись на дорогу — стоп, не петляем
				if (generatedMap[p])
					break;

				branch.Add(p);
				p += dir;

				// небольшой шанс свернуть (змейкой)
				if (Random.value < 0.15f)
				{
					if (dir.x != 0)
						dir = (Random.value < 0.5f) ? Vector2Int.up : Vector2Int.down;
					else
						dir = (Random.value < 0.5f) ? Vector2Int.left : Vector2Int.right;
				}
			}

			// применяем ветвь, если она не пуста
			foreach (var b in branch)
				generatedMap[b] = true;
		}

		PrintToConsole();
	}

	public void PrintToConsole()
	{
		if (generatedMap == null || generatedMap.Count == 0)
		{
			Debug.LogWarning("⚠️ No map generated yet. Press [Generate Map] first.");
			return;
		}

		Vector2Int start = new(0, -1);
		Vector2Int end = new(width - 1, -1);

		foreach (var kvp in generatedMap)
		{
			if (kvp.Key.x == 1 && kvp.Value) start = kvp.Key;
			if (kvp.Key.x == width - 2 && kvp.Value) end = kvp.Key;
		}

		System.Text.StringBuilder sb = new System.Text.StringBuilder();

		for (int y = height - 1; y >= 0; y--)
		{
			for (int x = 0; x < width; x++)
			{
				var pos = new Vector2Int(x, y);
				if (pos == start) sb.Append("S");
				else if (pos == end) sb.Append("◎");
				else sb.Append(generatedMap[pos] ? "□" : "■");
			}

			sb.AppendLine();
		}

		Debug.Log(sb.ToString());
	}

	[Button("Clear Map")]
	public void Clear()
	{
		generatedMap?.Clear();
		Debug.Log("🧹 Map cleared.");
	}
}