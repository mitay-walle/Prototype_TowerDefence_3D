using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TD.Levels
{
	public class LevelGenerator : MonoBehaviour
	{
		[SerializeField] private TileMapManager tileMapManager;
		[SerializeField] private int tilesToGenerate = 4;
		[Range(2, 4)]
		[SerializeField] private int difficulty = 4;
		[SerializeField] private int seed;
		[ShowInInspector, ReadOnly] MapGenerator mapGenerator;
		[ShowInInspector, ReadOnly] private int generatedSeed;

		public int GeneratedSeed => generatedSeed;
		public bool IsValid { get; private set; }
		[ShowInInspector, ReadOnly] Dictionary<Vector2Int, RoadTileDef> generatedMap;

		[SerializeField] private bool Logs;

		private void OnEnable()
		{
			if (tileMapManager == null)
				tileMapManager = GetComponent<TileMapManager>();
		}

		public bool GenerateLevel()
		{
			IsValid = false;
			if (tileMapManager == null)
			{
				Debug.LogError("[LevelGenerator] TileMapManager not found!");
				return false;
			}

			if (Logs) Debug.Log("[LevelGenerator] === TILE-BASED LEVEL GENERATION STARTED ===");

			ClearLevel();
			GenerateInitialTiles();
			IsValid = ValidateLevel();
			//VisualizeMaps();

			if (IsValid)
			{
				if (Logs) Debug.Log("[LevelGenerator] === TILE-BASED LEVEL GENERATION COMPLETE ===");
			}
			else
			{
				Debug.LogError("[LevelGenerator] Level generation failed validation; bootstrap must remain in MapBuild.");
			}

			return IsValid;
		}

		private void GenerateInitialTiles()
		{
			if (Logs) Debug.Log($"[LevelGenerator] Generating {tilesToGenerate} tiles attached to base");

			List<RoadTileComponent> allTilePrefabs = TileDatabase.Instance.GetAllTilePrefabs();
			if (allTilePrefabs.Count == 0)
			{
				if (Logs) Debug.LogWarning("[LevelGenerator] Could not load tile prefabs");
				return;
			}

			generatedSeed = seed != 0 ? seed : CreateRandomSeed();
			if (Logs) Debug.Log($"[LevelGenerator] Using generation seed: {generatedSeed}");

			mapGenerator = new MapGenerator(tilesToGenerate, Logs, generatedSeed, difficulty);
			generatedMap = mapGenerator.GenerateMap(TileDatabase.Instance.GetAllTileKinds());

			foreach (KeyValuePair<Vector2Int, RoadTileDef> kvp in generatedMap)
			{
				Vector2Int gridPosition = kvp.Key;
				RoadTileDef tileDef = kvp.Value;
				RoadTileComponent tileComponent = TileDatabase.Instance.GetPrefabByConnections(tileDef.connections);
				if (tileComponent == null)
				{
					if (Logs) Debug.LogWarning($"[LevelGenerator] No prefab found for connections: {tileDef.connections}");
					continue;
				}

				tileMapManager.PlaceTile(gridPosition, tileDef, tileDef.rotation, tileComponent.gameObject);
			}

			MapVisualizer.LogMap(generatedMap);

			if (Logs) Debug.Log($"[LevelGenerator] Road network created with prefabs: {generatedMap.Count - 1} tiles placed");
		}

		private int CreateRandomSeed()
		{
			int randomSeed;
			do
			{
				randomSeed = MapGenerator.CreateRandomSeed();
			}
			while (randomSeed == generatedSeed);

			return randomSeed;
		}

		private bool ValidateLevel()
		{
			if (Logs) Debug.Log("[LevelGenerator] Validating level...");

			IReadOnlyDictionary<Vector2Int, RoadTileDef> allTiles = tileMapManager.GetAllTiles();
			List<Vector3> spawnPositions = tileMapManager.SpawnPositions;
			var isValid = generatedMap != null && generatedMap.Count > 1;
			isValid &= allTiles != null && allTiles.ContainsKey(Vector2Int.zero);
			isValid &= allTiles != null && generatedMap != null && allTiles.Count == generatedMap.Count;
			isValid &= spawnPositions != null && spawnPositions.Count > 0;

			if (!tileMapManager.ValidateTopology(out var topologyReason))
			{
				isValid = false;
				if (Logs) Debug.LogWarning($"[LevelGenerator] Invalid tile topology: {topologyReason}");
			}

			if (spawnPositions != null)
			{
				foreach (var spawnPosition in spawnPositions)
				{
					if (spawnPosition == Vector3.zero)
						isValid = false;
				}
			}

			if (allTiles != null)
			{
				foreach (var tile in allTiles)
				{
					var connections = tile.Value.GetRotatedConnections(tile.Value.rotation);
					foreach (RoadSide side in System.Enum.GetValues(typeof(RoadSide)))
					{
						var neighborPosition = tile.Key + GetGridDirection(side);
						if (!allTiles.TryGetValue(neighborPosition, out var neighbor))
							continue;

						var neighborConnections = neighbor.GetRotatedConnections(neighbor.rotation);
						if (connections.HasConnection(side) !=
							neighborConnections.HasConnection(RoadConnectionsExtensions.GetOppositeSide(side)))
							isValid = false;
					}
				}
			}

			if (Logs) Debug.Log($"[LevelGenerator] Level validation complete:");
			if (Logs) Debug.Log($"  - Tiles placed: {allTiles.Count}");
			if (Logs) Debug.Log($"  - Base position: {tileMapManager.BasePosition}");
			if (Logs) Debug.Log($"  - Spawn positions: {spawnPositions.Count}");

			foreach (Vector3 spawn in spawnPositions)
			{
				if (Logs) Debug.Log($"    • {spawn}");
			}

			return isValid;
		}

		private static Vector2Int GetGridDirection(RoadSide side)
		{
			return side switch
			{
				RoadSide.North => Vector2Int.up,
				RoadSide.South => Vector2Int.down,
				RoadSide.East => Vector2Int.right,
				RoadSide.West => Vector2Int.left,
				_ => Vector2Int.zero
			};
		}

		private void VisualizeMaps()
		{
			MapVisualizer.LogCurrentMap();
		}

		[Button("Generate Level")]
		public void GenerateLevelButton()
		{
			GenerateLevel();
		}

		[Button("Clear Level")]
		public void ClearLevel()
		{
			if (tileMapManager == null)
			{
				Debug.LogError("[LevelGenerator] TileMapManager not found!");
				return;
			}

			IReadOnlyDictionary<Vector2Int, RoadTileDef> allTiles = tileMapManager.GetAllTiles();
			List<Vector2Int> tilePositions = new System.Collections.Generic.List<Vector2Int>(allTiles.Keys);

			foreach (Vector2Int pos in tilePositions)
			{
				if (pos != Vector2Int.zero)
				{
					tileMapManager.RemoveTile(pos);
				}
			}

			if (Logs) Debug.Log("[LevelGenerator] Level cleared - base tile remains");
		}

		[Button("Reload Level")]
		public void ReloadLevel()
		{
			GenerateLevel();
		}

		public TileMapManager GetTileMapManager() => tileMapManager;

		private GameObject LoadTilePrefab(string name)
		{
			GameObject prefab = Resources.Load<GameObject>($"Prefabs/Tiles/{name}");
			if (prefab != null)
				return prefab;

            #if UNITY_EDITOR
			prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Tiles/{name}.prefab");
			if (prefab != null)
				return prefab;
            #endif

			return null;
		}
	}
}
