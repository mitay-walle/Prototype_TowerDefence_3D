using Sirenix.OdinInspector;
using TD.GameLoop;
using TD.Interactions;
using TD.Voxels;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TD.Levels
{
    public class TilePlacementSystem : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private GameObject ghostPrefab;
        [SerializeField] private TileMapManager tileMapManager;
        [SerializeField] private RoadTileDef[] availableTiles;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private NavMeshSurfaceWrapper navMeshSurfaceWrapper;

        private GameObject ghostTile;
        private RoadTileDef currentTileDef;
        private GameObject currentTilePrefab;
        private int currentRotation;
        private Vector2Int currentGridPosition;
        private VoxelGenerator ghostGenerator;
        private bool isPlacingTile;

        public bool IsPlacing => isPlacingTile;
        private bool Logs;
        private IRTSCInputProvider inputProvider;
        private InputAction clickAction;
        private InputAction rightClickAction;
        private InputAction submitAction;
        private InputAction cancelAction;
        private InputAction rotateAction;
        private InputAction restartAction;

        private void OnEnable()
        {
            if (tileMapManager == null)
                tileMapManager = GetComponent<TileMapManager>();

            if (navMeshSurfaceWrapper == null)
                navMeshSurfaceWrapper = FindAnyObjectByType<NavMeshSurfaceWrapper>();

            if (mainCamera != null)
                inputProvider = mainCamera.GetComponentInParent<IRTSCInputProvider>();

            if (inputActions == null) return;

            clickAction = inputActions.FindAction("UI/Click", true);
            rightClickAction = inputActions.FindAction("UI/RightClick", true);
            submitAction = inputActions.FindAction("UI/Submit", true);
            cancelAction = inputActions.FindAction("UI/Cancel", true);
            rotateAction = inputActions.FindAction("Player/Build", true);
            restartAction = inputActions.FindAction("Player/Restart", true);

            clickAction.Enable();
            rightClickAction.Enable();
            submitAction.Enable();
            cancelAction.Enable();
            rotateAction.Enable();
            restartAction.Enable();
        }

        public void StartTilePlacement(RoadTileDef tileDef, GameObject tilePrefab)
        {
            if (tileDef.name == null || tilePrefab == null) return;

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
            if (!isPlacingTile || currentTileDef.name == null) return;

            if (!tileMapManager.CanPlaceTile(currentGridPosition, currentTileDef, currentRotation))
            {
                if (Logs) Debug.LogWarning($"[TilePlacement] Cannot place tile at {currentGridPosition}");
                return;
            }

            tileMapManager.PlaceTile(currentGridPosition, currentTileDef, currentRotation, currentTilePrefab);
            if (navMeshSurfaceWrapper != null && !navMeshSurfaceWrapper.BuildNavMesh())
            {
                Debug.LogError("[TilePlacement] NavMesh rebuild did not produce NavMesh data!");
            }

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
            if (clickAction != null && clickAction.WasPressedThisFrame() &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                PlaceTile();
            }

            if (rightClickAction != null && rightClickAction.WasPressedThisFrame() &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                RotateTile();
            }

            if (submitAction != null && submitAction.WasPressedThisFrame())
            {
                PlaceTile();
            }

            if (rotateAction != null && rotateAction.WasPressedThisFrame())
            {
                RotateTile();
            }

            if (cancelAction != null && cancelAction.WasPressedThisFrame() &&
                cancelAction.activeControl?.device is Keyboard)
            {
                CancelPlacement();
            }

            if (restartAction != null && restartAction.WasPressedThisFrame())
            {
                CancelPlacement();
            }
        }

        private void UpdateGhostPosition()
        {
            if (mainCamera == null || ghostTile == null) return;

            if (inputProvider == null) return;

            Vector2 mousePos = inputProvider.MousePosition();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            if (tileMapManager == null || !tileMapManager.TryGetGridPoint(ray, out var hitPoint))
                return;

            currentGridPosition = tileMapManager.WorldToGrid(hitPoint);
			ghostTile.transform.position = tileMapManager.GridToWorld(currentGridPosition);
			ghostTile.transform.rotation = Quaternion.Euler(0, currentRotation * 90, 0);

			UpdateGhostAppearance();
        }

        private void UpdateGhostAppearance()
        {
            if (ghostTile == null || currentTileDef.name == null) return;

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

            if (ghostGenerator != null && currentTileDef.name != null)
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
            if (availableTiles.Length == 0 || TileDatabase.Instance == null) return;

            var tilePrefab = TileDatabase.Instance.GetPrefabByConnections(availableTiles[0].connections);
            if (tilePrefab != null)
                StartTilePlacement(availableTiles[0], tilePrefab.gameObject);
        }
    }
}
