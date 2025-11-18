using UnityEngine;
using Sirenix.OdinInspector;

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

            if (Logs) Debug.Log("[LevelGenerator] === TILE-BASED LEVEL GENERATION COMPLETE ===");
        }

private void GenerateInitialTiles()
	{
		if (Logs) Debug.Log($"[LevelGenerator] Generating {tilesToGenerate} tiles attached to base");

		var allTilePrefabs = LoadAllTilePrefabs();
		if (allTilePrefabs.Count == 0)
		{
			if (Logs) Debug.LogWarning("[LevelGenerator] Could not load tile prefabs");
			return;
		}

		int placedCount = 0;
		var openEdges = new System.Collections.Generic.Queue<Vector2Int>();
		openEdges.Enqueue(new Vector2Int(0, -1));
		openEdges.Enqueue(new Vector2Int(0, 1));
		openEdges.Enqueue(new Vector2Int(1, 0));
		openEdges.Enqueue(new Vector2Int(-1, 0));

		var processedEdges = new System.Collections.Generic.HashSet<Vector2Int>();

		while (openEdges.Count > 0 && placedCount < tilesToGenerate)
		{
			var nextPos = openEdges.Dequeue();

			if (processedEdges.Contains(nextPos))
				continue;

			processedEdges.Add(nextPos);

			if (tileMapManager.GetTile(nextPos) != null)
				continue;

			var randomTilePrefab = allTilePrefabs[Random.Range(0, allTilePrefabs.Count)];
			var tileConnections = randomTilePrefab.GetConnections();

			var tileDef = new RoadTileDef
			{
				name = randomTilePrefab.name,
				connections = tileConnections
			};

			for (int rotation = 0; rotation < 4; rotation++)
			{
				if (!tileMapManager.CanPlaceTile(nextPos, tileDef, rotation))
					continue;

				tileMapManager.PlaceTile(nextPos, tileDef, rotation, randomTilePrefab.gameObject);
				placedCount++;

				if (Logs) Debug.Log($"[LevelGenerator] Placed {tileDef.name} at {nextPos} with rotation {rotation}");

				var newConnections = tileDef.GetRotatedConnections(rotation);
				if (newConnections.HasConnection(RoadSide.North))
					openEdges.Enqueue(nextPos + Vector2Int.up);
				if (newConnections.HasConnection(RoadSide.South))
					openEdges.Enqueue(nextPos + Vector2Int.down);
				if (newConnections.HasConnection(RoadSide.East))
					openEdges.Enqueue(nextPos + Vector2Int.right);
				if (newConnections.HasConnection(RoadSide.West))
					openEdges.Enqueue(nextPos + Vector2Int.left);

				break;
			}
		}

		if (Logs) Debug.Log($"[LevelGenerator] Road network created: {placedCount} tiles placed");
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
    

private System.Collections.Generic.List<RoadTileComponent> LoadAllTilePrefabs()
	{
		var tileComponents = new System.Collections.Generic.List<RoadTileComponent>();

		if (TileDatabase.Instance != null)
		{
			var allTiles = TileDatabase.Instance.GetAllTilePrefabs();
			foreach (var tilePrefab in allTiles)
			{
				var component = tilePrefab.GetComponent<RoadTileComponent>();
				if (component != null)
					tileComponents.Add(component);
			}
		}

		return tileComponents;
	}
}
}
