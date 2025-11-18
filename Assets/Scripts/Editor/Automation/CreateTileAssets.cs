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
            Debug.Log("[CreateTileAssets] TileDef creation skipped (created at runtime)");
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

                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                GameObject tileGo;
                bool isExisting = prefabAsset != null;

                if (isExisting)
                {
                    tileGo = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
                }
                else
                {
                    tileGo = new GameObject(name);
                    var voxelGenerator = tileGo.AddComponent<VoxelGenerator>();
                    voxelGenerator.profile = new LevelTileGenerationProfile();

                    var tileComponent = tileGo.AddComponent<RoadTileComponent>();
                    tileComponent.Initialize(connections);

                    voxelGenerator.Generate();
                    Object.DestroyImmediate(voxelGenerator);
                }

                var meshFilters = tileGo.GetComponentsInChildren<MeshFilter>();

                foreach (MeshFilter meshFilter in meshFilters)
                {
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        var meshCollider = meshFilter.GetComponent<MeshCollider>();
                        if (meshCollider == null)
                        {
                            meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
                            meshCollider.convex = false;
                            meshCollider.sharedMesh = meshFilter.sharedMesh;
                        }
                    }    
                }
                

                tileGo.layer = LayerMask.NameToLayer("Tileable");
                if (tileGo.layer == -1)
                    tileGo.layer = 0;

                if (isExisting)
                {
                    PrefabUtility.ApplyPrefabInstance(tileGo, InteractionMode.AutomatedAction);
                    Object.DestroyImmediate(tileGo);
                    Debug.Log($"[CreateTileAssets] Updated Tile Prefab: {name}");
                }
                else
                {
                    PrefabUtility.SaveAsPrefabAsset(tileGo, prefabPath);
                    Object.DestroyImmediate(tileGo);
                    Debug.Log($"[CreateTileAssets] Created Tile Prefab: {name}");
                }
            }

            Debug.Log("[CreateTileAssets] Prefabs updated");
        }
    }
}
