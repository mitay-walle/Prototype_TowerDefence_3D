using UnityEngine;
using UnityEditor;
using TD;
using TD.Voxels;
using System.IO;

namespace TD.Editor
{
    public static class CreateTileAssets
    {
        private const string TilePrefabPath = "Assets/Prefabs/Tiles";
        private const string TileDefPath = "Assets/Resources/TileDefs";

        [MenuItem("Assets/TD/Create Tile Assets")]
        public static void CreateTileAssetsMenu()
        {
            CreateTileAssetsFinal();
        }

        public static void CreateTileAssetsFinal()
        {
            Debug.Log("[CreateTileAssets] Starting tile asset creation...");
            
            CreateDirectories();
            CreateTileDefinitions();
            CreateTilePrefabs();
            AssetDatabase.Refresh();
            
            Debug.Log("[CreateTileAssets] Tile assets created successfully!");
        }

        private static void CreateDirectories()
        {
            CreateDirectoryIfNotExists("Assets");
            CreateDirectoryIfNotExists("Assets/Prefabs");
            CreateDirectoryIfNotExists(TilePrefabPath);
            
            CreateDirectoryIfNotExists("Assets/Resources");
            CreateDirectoryIfNotExists(TileDefPath);
            
            Debug.Log("[CreateTileAssets] Directories created");
        }

        private static void CreateDirectoryIfNotExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parentPath = Path.GetDirectoryName(path).Replace("\\", "/");
                string folderName = Path.GetFileName(path);
                
                if (string.IsNullOrEmpty(parentPath) || parentPath == ".")
                    parentPath = "Assets";
                
                if (!string.IsNullOrEmpty(parentPath) && parentPath != path)
                {
                    CreateDirectoryIfNotExists(parentPath);
                    
                    if (AssetDatabase.IsValidFolder(parentPath))
                    {
                        AssetDatabase.CreateFolder(parentPath, folderName);
                        Debug.Log($"[CreateTileAssets] Created folder: {path}");
                    }
                }
            }
        }

        private static void CreateTileDefinitions()
        {
            var tileDefs = new (string name, RoadConnections connections)[]
            {
                ("Straight_H", RoadConnections.North | RoadConnections.South),
                ("Straight_V", RoadConnections.East | RoadConnections.West),
                ("Turn_NE", RoadConnections.North | RoadConnections.East),
                ("Turn_NW", RoadConnections.North | RoadConnections.West),
                ("Turn_SE", RoadConnections.South | RoadConnections.East),
                ("Turn_SW", RoadConnections.South | RoadConnections.West),
                ("Cross_3_N", RoadConnections.East | RoadConnections.South | RoadConnections.West),
                ("Cross_3_S", RoadConnections.North | RoadConnections.East | RoadConnections.West),
                ("Cross_3_E", RoadConnections.North | RoadConnections.South | RoadConnections.West),
                ("Cross_3_W", RoadConnections.North | RoadConnections.South | RoadConnections.East),
                ("Cross_4", RoadConnections.North | RoadConnections.South | RoadConnections.East | RoadConnections.West),
            };

            foreach (var (name, connections) in tileDefs)
            {
                string assetPath = $"{TileDefPath}/{name}.asset";
                
                var existing = AssetDatabase.LoadAssetAtPath<RoadTileDef>(assetPath);
                if (existing != null)
                {
                    Debug.Log($"[CreateTileAssets] TileDef already exists: {name}");
                    continue;
                }
                
                RoadTileDef def = ScriptableObject.CreateInstance<RoadTileDef>();
                
                var property = typeof(RoadTileDef).GetProperty("connections", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(def, connections);
                }
                
                AssetDatabase.CreateAsset(def, assetPath);
                EditorUtility.SetDirty(def);
                
                Debug.Log($"[CreateTileAssets] Created TileDef: {name}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[CreateTileAssets] TileDefs saved");
        }

        private static void CreateTilePrefabs()
        {
            var tileConfigs = new (string name, RoadConnections connections)[]
            {
                ("Straight_H", RoadConnections.North | RoadConnections.South),
                ("Straight_V", RoadConnections.East | RoadConnections.West),
                ("Turn_NE", RoadConnections.North | RoadConnections.East),
                ("Turn_NW", RoadConnections.North | RoadConnections.West),
                ("Turn_SE", RoadConnections.South | RoadConnections.East),
                ("Turn_SW", RoadConnections.South | RoadConnections.West),
                ("Cross_3_N", RoadConnections.East | RoadConnections.South | RoadConnections.West),
                ("Cross_3_S", RoadConnections.North | RoadConnections.East | RoadConnections.West),
                ("Cross_3_E", RoadConnections.North | RoadConnections.South | RoadConnections.West),
                ("Cross_3_W", RoadConnections.North | RoadConnections.South | RoadConnections.East),
                ("Cross_4", RoadConnections.North | RoadConnections.South | RoadConnections.East | RoadConnections.West),
            };

            foreach (var (name, connections) in tileConfigs)
            {
                string prefabPath = $"{TilePrefabPath}/{name}.prefab";
                
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (existing != null)
                {
                    Debug.Log($"[CreateTileAssets] Prefab already exists: {name}");
                    continue;
                }
                
                GameObject tileGo = new GameObject(name);
                
                var voxelGenerator = tileGo.AddComponent<VoxelGenerator>();
                voxelGenerator.profile = new LevelTileGenerationProfile();
                
                var tileComponent = tileGo.AddComponent<RoadTileComponent>();
                tileComponent.Initialize(connections);
                
                PrefabUtility.SaveAsPrefabAsset(tileGo, prefabPath);
                Object.DestroyImmediate(tileGo);
                
                Debug.Log($"[CreateTileAssets] Created Tile Prefab: {name}");
            }
            
            Debug.Log("[CreateTileAssets] Prefabs created");
        }
    }

    public class RoadTileComponent : MonoBehaviour
    {
        [SerializeField] private RoadConnections connections;

        public void Initialize(RoadConnections roadConnections)
        {
            connections = roadConnections;
        }

        public RoadConnections GetConnections() => connections;
    }
}
