using UnityEngine;
using UnityEditor;
using TD;
using TD.Voxels;
using System.IO;
using TD.Levels;

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

        [MenuItem("Assets/TD/Recreate Optimized Tile Assets")]
        public static void RecreateTileAssetsMenu()
        {
            DeleteExistingTileAssets();
            CreateTileAssetsFinal();
        }

        private static void DeleteExistingTileAssets()
        {
            var tileDefFolder = TileDefPath;
            var guids = AssetDatabase.FindAssets("", new[] { tileDefFolder });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".asset"))
                {
                    AssetDatabase.DeleteAsset(path);
                    Debug.Log($"[CreateTileAssets] Deleted: {path}");
                }
            }

            var prefabFolder = TilePrefabPath;
            guids = AssetDatabase.FindAssets("", new[] { prefabFolder });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".prefab"))
                {
                    AssetDatabase.DeleteAsset(path);
                    Debug.Log($"[CreateTileAssets] Deleted: {path}");
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[CreateTileAssets] Old assets deleted");
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
                ("Straight", RoadConnections.North | RoadConnections.South),
                ("Turn", RoadConnections.North | RoadConnections.East),
                ("Cross_3", RoadConnections.North | RoadConnections.East | RoadConnections.West),
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
                def.InitializeConnections(connections);

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
                ("Straight", RoadConnections.North | RoadConnections.South),
                ("Turn", RoadConnections.North | RoadConnections.East),
                ("Cross_3", RoadConnections.North | RoadConnections.East | RoadConnections.West),
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
                voxelGenerator.profile = ScriptableObject.CreateInstance<LevelTileGenerationProfile>();

                var tileComponent = tileGo.AddComponent<RoadTileComponent>();
                tileComponent.Initialize(connections);

                voxelGenerator.GenerateMesh();

                Object.DestroyImmediate(voxelGenerator);

                PrefabUtility.SaveAsPrefabAsset(tileGo, prefabPath);
                Object.DestroyImmediate(tileGo);

                Debug.Log($"[CreateTileAssets] Created Tile Prefab: {name}");
            }

            Debug.Log("[CreateTileAssets] Prefabs created");
        }
    }
}
