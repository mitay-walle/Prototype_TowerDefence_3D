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
                EditorApplication.delayCall -= CreateAssetsDelayed;
            }
        }
    }
}
