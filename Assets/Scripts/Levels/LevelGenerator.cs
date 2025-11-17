using UnityEngine;

namespace TD
{
    public class LevelGenerator : MonoBehaviour
    {
        [SerializeField] private TileMapManager tileMapManager;

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

            Debug.Log("[LevelGenerator] Tile-based level generated - ready for player tile placement");
        }

        public TileMapManager GetTileMapManager() => tileMapManager;
    }
}
