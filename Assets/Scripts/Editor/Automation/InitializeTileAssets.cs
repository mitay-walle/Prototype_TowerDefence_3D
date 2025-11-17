using UnityEditor;

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
                CreateTileAssets.CreateTileAssetsFinal();
                EditorApplication.delayCall -= CreateAssetsDelayed;
            }
        }
    }
}
