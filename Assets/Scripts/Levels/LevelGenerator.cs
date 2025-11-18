using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using NUnit.Framework;

namespace TD.Levels
{
	public class LevelGenerator : MonoBehaviour
	{
		[SerializeField] private TileMapManager tileMapManager;
		[SerializeField] private int tilesToGenerate = 10;

		private bool Logs = true;

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
			VisualizeMaps();

			if (Logs) Debug.Log("[LevelGenerator] === TILE-BASED LEVEL GENERATION COMPLETE ===");
		}

		private void GenerateInitialTiles()
		{
			if (Logs) Debug.Log($"[LevelGenerator] Generating {tilesToGenerate} tiles attached to base");

			var allTilePrefabs = TileDatabase.Instance.GetAllTilePrefabs();
			if (allTilePrefabs.Count == 0)
			{
				if (Logs) Debug.LogWarning("[LevelGenerator] Could not load tile prefabs");
				return;
			}

			var mapGenerator = new MapGenerator(tilesToGenerate, Logs);
			var generatedMap = mapGenerator.GenerateMap(TileDatabase.Instance.GetAllTileKinds());

			foreach (var kvp in generatedMap)
			{
				var gridPosition = kvp.Key;
				var tileDef = kvp.Value;

				if (gridPosition == Vector2Int.zero)
					continue;

				var prefab = LoadTilePrefab(tileDef.name);
				if (prefab == null)
					continue;

				tileMapManager.PlaceTile(gridPosition, tileDef, tileDef.rotation, prefab);
			}

			if (Logs) Debug.Log($"[LevelGenerator] Road network created with prefabs: {generatedMap.Count - 1} tiles placed");
		}

		private void ValidateLevel()
		{
			if (Logs) Debug.Log("[LevelGenerator] Validating level...");

			var allTiles = tileMapManager.GetAllTiles();
			var spawnPositions = tileMapManager.SpawnPositions;

			if (Logs) Debug.Log($"[LevelGenerator] Level validation complete:");
			if (Logs) Debug.Log($"  - Tiles placed: {allTiles.Count}");
			if (Logs) Debug.Log($"  - Base position: {tileMapManager.BasePosition}");
			if (Logs) Debug.Log($"  - Spawn positions: {spawnPositions.Count}");

			foreach (var spawn in spawnPositions)
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

			var allTiles = tileMapManager.GetAllTiles();
			var tilePositions = new System.Collections.Generic.List<Vector2Int>(allTiles.Keys);

			foreach (var pos in tilePositions)
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
			var prefab = Resources.Load<GameObject>($"Prefabs/Tiles/{name}");
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