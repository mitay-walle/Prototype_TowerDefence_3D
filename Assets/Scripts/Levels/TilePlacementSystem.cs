using Sirenix.OdinInspector;
using TD.Voxels;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TD.Levels
{
    public class TilePlacementSystem : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private GameObject ghostPrefab;
        [SerializeField] private TileMapManager tileMapManager;
        [SerializeField] private RoadTileDef[] availableTiles;

        private GameObject ghostTile;
        private RoadTileDef currentTileDef;
        private GameObject currentTilePrefab;
        private int currentRotation;
        private Vector2Int currentGridPosition;
        private VoxelGenerator ghostGenerator;
        private bool isPlacingTile;
        private bool Logs;

        private void OnEnable()
        {
            if (tileMapManager == null)
                tileMapManager = GetComponent<TileMapManager>();
        }

        public void StartTilePlacement(RoadTileDef tileDef, GameObject tilePrefab)
        {
            if (tileDef == null || tilePrefab == null) return;

            currentTileDef = tileDef;
            currentTilePrefab = tilePrefab;
            currentRotation = 0;
            isPlacingTile = true;

            CreateGhost();

            if (Logs) Debug.Log($"[TilePlacement] Started placing tile: {tileDef.name}");
        }

        public void CancelPlacement()
        {
            if (ghostTile != null)
                Destroy(ghostTile);

            isPlacingTile = false;
            if (Logs) Debug.Log("[TilePlacement] Placement cancelled");
        }

        public void RotateTile()
        {
            if (!isPlacingTile) return;

            currentRotation = (currentRotation + 1) % 4;
            UpdateGhostAppearance();

            if (Logs) Debug.Log($"[TilePlacement] Tile rotated to {currentRotation * 90}°");
        }

        public void PlaceTile()
        {
            if (!isPlacingTile || currentTileDef == null) return;

            if (!tileMapManager.CanPlaceTile(currentGridPosition, currentTileDef, currentRotation))
            {
                if (Logs) Debug.LogWarning($"[TilePlacement] Cannot place tile at {currentGridPosition}");
                return;
            }

            tileMapManager.PlaceTile(currentGridPosition, currentTileDef, currentRotation, currentTilePrefab);
            CancelPlacement();

            if (Logs) Debug.Log($"[TilePlacement] Tile placed at {currentGridPosition}");
        }

        private void Update()
        {
            if (!isPlacingTile) return;

            HandleInput();
            UpdateGhostPosition();
        }

        private void HandleInput()
        {
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                    PlaceTile();

                if (Mouse.current.rightButton.wasPressedThisFrame)
                    RotateTile();

                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                    CancelPlacement();
            }

            if (Gamepad.current != null)
            {
                if (Gamepad.current.aButton.wasPressedThisFrame)
                    PlaceTile();

                if (Gamepad.current.bButton.wasPressedThisFrame)
                    RotateTile();

                if (Gamepad.current.yButton.wasPressedThisFrame)
                    CancelPlacement();
            }
        }

        private void UpdateGhostPosition()
        {
            if (mainCamera == null || ghostTile == null) return;

            Vector2 mousePos = Mouse.current?.position.ReadValue() ?? Vector2.zero;
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
            {
                Vector3 hitPoint = hit.point;
                currentGridPosition = new Vector2Int(Mathf.RoundToInt(hitPoint.x / 5f), Mathf.RoundToInt(hitPoint.z / 5f));

                ghostTile.transform.position = new Vector3(currentGridPosition.x * 5f, 0, currentGridPosition.y * 5f);
                ghostTile.transform.rotation = Quaternion.Euler(0, currentRotation * 90, 0);

                UpdateGhostAppearance();
            }
        }

        private void UpdateGhostAppearance()
        {
            if (ghostTile == null || currentTileDef == null) return;

            bool canPlace = tileMapManager.CanPlaceTile(currentGridPosition, currentTileDef, currentRotation);

            var renderers = ghostTile.GetComponentsInChildren<Renderer>();

            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    mat.color = canPlace ? Color.green : Color.red;
                }
            }
        }

        private void CreateGhost()
        {
            if (ghostTile != null)
                Destroy(ghostTile);

            ghostTile = Instantiate(ghostPrefab ?? currentTilePrefab, Vector3.zero, Quaternion.identity);
            ghostGenerator = ghostTile.GetComponent<VoxelGenerator>();

            if (ghostGenerator != null && currentTileDef != null)
            {
                var profile = new LevelTileGenerationProfile();
                ghostGenerator.profile = profile;
                ghostGenerator.Generate();
            }

            var renderers = ghostTile.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.gameObject.layer = LayerMask.NameToLayer("Default");
            }

            ghostTile.name = "Ghost_Tile";
        }

        [Button("Test Placement")]
        private void TestPlacement()
        {
            if (availableTiles.Length == 0) return;
            
            var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tiles/Straight_H.prefab");
            StartTilePlacement(availableTiles[0], tilePrefab);
        }

        private static class AssetDatabase
        {
            public static T LoadAssetAtPath<T>(string path) where T : Object
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            }
        }
    }
}
