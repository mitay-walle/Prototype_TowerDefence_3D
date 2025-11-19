using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TD.Levels
{
	public class LevelGenerator : MonoBehaviour
	{
		[SerializeField] private TileMapManager tileMapManager;
		[SerializeField] private int tilesToGenerate = 10;
		[ShowInInspector, ReadOnly] MapGenerator mapGenerator;
		[ShowInInspector, ReadOnly] Dictionary<Vector2Int, RoadTileDef> generatedMap;

		[SerializeField] private bool Logs;

		private void OnEnable()
		{
			if (tileMapManager == null)
				tileMapManager = GetComponent<TileMapManager>();
		}

		public void GenerateLevel()
		{
			if (tileMapManager == null)
			{
				Debug.LogError("[LevelGenerator] TileMapManager not found!");
				return;
			}

			if (Logs) Debug.Log("[LevelGenerator] === TILE-BASED LEVEL GENERATION STARTED ===");

			GenerateInitialTiles();
			ValidateLevel();
			//VisualizeMaps();

			if (Logs) Debug.Log("[LevelGenerator] === TILE-BASED LEVEL GENERATION COMPLETE ===");
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

			mapGenerator = new MapGenerator(tilesToGenerate, Logs);
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

		private void ValidateLevel()
		{
			if (Logs) Debug.Log("[LevelGenerator] Validating level...");

			IReadOnlyDictionary<Vector2Int, RoadTileDef> allTiles = tileMapManager.GetAllTiles();
			List<Vector3> spawnPositions = tileMapManager.SpawnPositions;

			if (Logs) Debug.Log($"[LevelGenerator] Level validation complete:");
			if (Logs) Debug.Log($"  - Tiles placed: {allTiles.Count}");
			if (Logs) Debug.Log($"  - Base position: {tileMapManager.BasePosition}");
			if (Logs) Debug.Log($"  - Spawn positions: {spawnPositions.Count}");

			foreach (Vector3 spawn in spawnPositions)
			{
				if (Logs) Debug.Log($"    • {spawn}");
			}
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
			ClearLevel();
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