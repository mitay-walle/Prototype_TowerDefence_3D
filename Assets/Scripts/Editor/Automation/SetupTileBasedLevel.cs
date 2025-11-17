using UnityEngine;
using UnityEditor;
using TD.GameLoop;

namespace TD.Editor
{
    public static class SetupTileBasedLevel
    {
        private const string BootstrapName = "GameplayBootstrap";
        private const string LevelGenName = "LevelGenerator";
        private const string TileMapName = "TileMapManager";
        private const string TilePlacementName = "TilePlacementSystem";

        [MenuItem("TD/Setup/Create Tile-Based Level Objects")]
        public static void SetupTileLevel()
        {
            Debug.Log("[SetupTileBasedLevel] Creating tile-based level hierarchy...");

            var bootstrap = FindOrCreateGameObject(BootstrapName, null);
            var levelGenGo = FindOrCreateGameObject(LevelGenName, bootstrap.transform);
            var tileMapGo = FindOrCreateGameObject(TileMapName, levelGenGo.transform);
            var tilePlacementGo = FindOrCreateGameObject(TilePlacementName, bootstrap.transform);

            var bootstrapComponent = bootstrap.GetComponent<GameplayBootstrap>();
            if (bootstrapComponent == null)
            {
                bootstrapComponent = bootstrap.AddComponent<GameplayBootstrap>();
                Debug.Log("[SetupTileBasedLevel] Added GameplayBootstrap component");
            }

            var levelGen = levelGenGo.GetComponent<LevelGenerator>();
            if (levelGen == null)
            {
                levelGen = levelGenGo.AddComponent<LevelGenerator>();
                Debug.Log("[SetupTileBasedLevel] Added LevelGenerator component");
            }

            var tileMapManager = tileMapGo.GetComponent<TileMapManager>();
            if (tileMapManager == null)
            {
                tileMapManager = tileMapGo.AddComponent<TileMapManager>();
                Debug.Log("[SetupTileBasedLevel] Added TileMapManager component");
            }

            var tilePlacement = tilePlacementGo.GetComponent<TilePlacementSystem>();
            if (tilePlacement == null)
            {
                tilePlacement = tilePlacementGo.AddComponent<TilePlacementSystem>();
                Debug.Log("[SetupTileBasedLevel] Added TilePlacementSystem component");
            }

            AssignReferencesToBootstrap(bootstrap, bootstrapComponent, levelGen, tilePlacement);
            AssignReferencesToLevelGen(levelGen, tileMapManager);
            AssignReferencesToTilePlacement(tilePlacement, tileMapManager);

            EditorGUIUtility.PingObject(bootstrap);
            EditorUtility.SetDirty(bootstrap);

            Debug.Log("[SetupTileBasedLevel] ✓ Tile-based level setup complete!");
            LogSetupInstructions();
        }

        [MenuItem("TD/Setup/Assign GameManager References")]
        public static void AssignGameManagerReferences()
        {
            var bootstrap = GameObject.Find(BootstrapName);
            if (bootstrap == null)
            {
                Debug.LogError("[SetupTileBasedLevel] GameplayBootstrap not found! Run 'Create Tile-Based Level Objects' first.");
                return;
            }

            var bootstrapComponent = bootstrap.GetComponent<GameplayBootstrap>();
            if (bootstrapComponent == null)
            {
                Debug.LogError("[SetupTileBasedLevel] GameplayBootstrap component not found!");
                return;
            }

            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("[SetupTileBasedLevel] GameManager not found in scene!");
                return;
            }

            var gameHUD = FindObjectOfType<UI.GameHUD>();
            if (gameHUD == null)
            {
                Debug.LogWarning("[SetupTileBasedLevel] GameHUD not found in scene - optional");
            }

            var waveManager = FindObjectOfType<WaveManager>();
            if (waveManager == null)
            {
                Debug.LogError("[SetupTileBasedLevel] WaveManager not found in scene!");
                return;
            }

            var field = typeof(GameplayBootstrap).GetField("gameManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(bootstrapComponent, gameManager);

            field = typeof(GameplayBootstrap).GetField("gameHUD", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(bootstrapComponent, gameHUD);

            field = typeof(GameplayBootstrap).GetField("waveManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(bootstrapComponent, waveManager);

            EditorUtility.SetDirty(bootstrap);
            Debug.Log("[SetupTileBasedLevel] ✓ GameManager references assigned!");
        }

        [MenuItem("TD/Setup/Show Tile Setup Instructions")]
        public static void ShowSetupInstructions()
        {
            LogSetupInstructions();
        }

        [MenuItem("TD/Setup/Reset Tile Level")]
        public static void ResetTileLevel()
        {
            var objects = new[] 
            { 
                GameObject.Find(BootstrapName),
                GameObject.Find(LevelGenName),
                GameObject.Find(TileMapName),
                GameObject.Find(TilePlacementName)
            };

            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                    Debug.Log($"[SetupTileBasedLevel] Destroyed {obj.name}");
                }
            }

            Debug.Log("[SetupTileBasedLevel] ✓ Reset complete - run 'Create Tile-Based Level Objects' to rebuild");
        }

        private static void LogSetupInstructions()
        {
            Debug.Log(@"
╔════════════════════════════════════════════════════════════╗
║       TILE-BASED LEVEL SYSTEM - SETUP COMPLETE              ║
╚════════════════════════════════════════════════════════════╝

✓ CREATED OBJECTS:
  • GameplayBootstrap (root coordinator)
  • LevelGenerator (initialization)
  • TileMapManager (tile management)
  • TilePlacementSystem (user interaction)

📋 NEXT STEPS:

1. ASSIGN GAME SYSTEMS:
   → Run: TD/Setup/Assign GameManager References
   This will auto-find and assign:
     • GameManager
     • GameHUD
     • WaveManager

2. CONFIGURE TILE PLACEMENT:
   Select TilePlacementSystem GameObject and set:
   
   ⚙️ Inspector Fields:
   • Main Camera → Main Camera from scene
   • Ground Mask → Layer for raycasting (e.g., 'Default')
   • Ghost Prefab → Simple cube or preview prefab
   • Available Tiles → Load from Assets/Resources/TileDefs/
   • Tile Map Manager → TileMapManager component

3. CREATE GHOST PREVIEW (optional):
   → Assets/Prefabs/Tiles/Straight_H.prefab works as template

🎮 IN-GAME CONTROLS:
   Mouse:
     • Move → Preview tile position
     • LMB → Place tile
     • RMB → Rotate tile 90°
     • ESC → Cancel placement
   
   Gamepad:
     • Move → Preview tile position
     • A → Place tile
     • B → Rotate tile
     • Y → Cancel placement

🧩 TILE SYSTEM:
   • 5x5 unit tiles (auto grid snap)
   • Road connections validated
   • Dead-ends (1 connection) prevented
   • 11 tile types: straights, turns, crosses
   • Spawner positions auto-update

📁 KEY FILES:
   Scripts/Levels/TileMapManager.cs
   Scripts/Levels/TilePlacementSystem.cs
   Scripts/Levels/TilePlacementValidator.cs
   Resources/TileDefs/ (tile configurations)
   Prefabs/Tiles/ (3D models)

⚡ READY TO PLAY!
");
        }

        private static void AssignReferencesToBootstrap(GameObject bootstrap, GameplayBootstrap bootstrapComponent, LevelGenerator levelGen, TilePlacementSystem tilePlacement)
        {
            var field = typeof(GameplayBootstrap).GetField("levelGenerator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(bootstrapComponent, levelGen);

            field = typeof(GameplayBootstrap).GetField("tilePlacementSystem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(bootstrapComponent, tilePlacement);

            Debug.Log("[SetupTileBasedLevel] Assigned references to GameplayBootstrap");
        }

        private static void AssignReferencesToLevelGen(LevelGenerator levelGen, TileMapManager tileMapManager)
        {
            var field = typeof(LevelGenerator).GetField("tileMapManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(levelGen, tileMapManager);

            Debug.Log("[SetupTileBasedLevel] Assigned TileMapManager to LevelGenerator");
        }

        private static void AssignReferencesToTilePlacement(TilePlacementSystem tilePlacement, TileMapManager tileMapManager)
        {
            var field = typeof(TilePlacementSystem).GetField("tileMapManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(tilePlacement, tileMapManager);

            var mainCam = Camera.main;
            if (mainCam != null)
            {
                field = typeof(TilePlacementSystem).GetField("mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(tilePlacement, mainCam);
                Debug.Log("[SetupTileBasedLevel] Assigned Main Camera to TilePlacementSystem");
            }

            Debug.Log("[SetupTileBasedLevel] Assigned TileMapManager to TilePlacementSystem");
        }

        private static GameObject FindOrCreateGameObject(string name, Transform parent)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
            {
                if (parent != null && existing.transform.parent != parent)
                    existing.transform.SetParent(parent);
                return existing;
            }

            var go = new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent);

            return go;
        }

        private static T FindObjectOfType<T>() where T : Component
        {
            return Object.FindObjectOfType<T>();
        }
    }
}
