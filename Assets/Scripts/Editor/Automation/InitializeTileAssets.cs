using UnityEditor;
using UnityEngine;

namespace TD.Editor
{
    [InitializeOnLoad]
    public class InitializeTileAssets
    {
        static InitializeTileAssets()
        {
            EditorApplication.delayCall += CreateAssetsDelayed;
        }

        private static void CreateAssetsDelayed()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                CheckAndCreateAssets();
                EditorApplication.delayCall -= CreateAssetsDelayed;
            }
        }

        private static void CheckAndCreateAssets()
        {
            var straightH = Resources.Load<TD.Levels.RoadTileDef>("TileDefs/Straight_H");

            if (straightH == null)
            {
                Debug.Log("[InitializeTileAssets] Tile assets not found, creating...");
                CreateTileAssets.CreateTileAssetsFinal();
                return;
            }

            if ((int)straightH.connections == 0)
            {
                Debug.Log("[InitializeTileAssets] Tile assets found but empty (connections==0), recreating...");
                CreateTileAssets.RecreateTileAssetsMenu();
                return;
            }

            Debug.Log("[InitializeTileAssets] Tile assets valid, skipping recreation");
        }
    }
}
