using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TD.Levels;
using TD.UI;

namespace TD.GameLoop
{
    public class GameplayBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelGenerator levelGenerator;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameHUD gameHUD;
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private TilePlacementSystem tilePlacementSystem;
        [SerializeField] private NavMeshSurfaceWrapper navMeshSurfaceWrapper;

        private void Start()
        {
            StartCoroutine(BootstrapSequence());
        }

        private IEnumerator BootstrapSequence()
        {
            if (Logs) Debug.Log("[GameplayBootstrap] === BOOTSTRAP STARTED ===");

            yield return StartCoroutine(GenerateLevel());
            yield return StartCoroutine(BakeNavMesh());
            yield return StartCoroutine(PlaceGameplayObjects());
            yield return StartCoroutine(InitializeSystems());

            if (Logs) Debug.Log("[GameplayBootstrap] === BOOTSTRAP COMPLETE ===");
        }

        private IEnumerator GenerateLevel()
        {
            if (Logs) Debug.Log("[GameplayBootstrap] Generating level...");

            if (levelGenerator != null)
                levelGenerator.GenerateLevel();

            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator BakeNavMesh()
        {
            if (Logs) Debug.Log("[GameplayBootstrap] Baking NavMesh...");

            if (navMeshSurfaceWrapper != null)
            {
                navMeshSurfaceWrapper.BuildNavMesh();
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                if (Logs) Debug.LogWarning("[GameplayBootstrap] NavMeshSurface wrapper not found!");
                yield return null;
            }
        }

        private IEnumerator PlaceGameplayObjects()
        {
            if (Logs) Debug.Log("[GameplayBootstrap] Placing gameplay objects...");

            if (levelGenerator == null)
            {
                if (Logs) Debug.LogError("[GameplayBootstrap] LevelGenerator not found!");
                yield break;
            }

            var tileMapManager = levelGenerator.GetTileMapManager();
            if (tileMapManager == null)
            {
                if (Logs) Debug.LogError("[GameplayBootstrap] TileMapManager not found!");
                yield break;
            }

            Vector3 basePosition = tileMapManager.BasePosition;
            List<Vector3> spawnPositions = tileMapManager.SpawnPositions;

            if (Logs) Debug.Log($"[GameplayBootstrap] Base at {basePosition}, spawners: {spawnPositions.Count}");

            var playerBaseGo = GameObject.FindObjectOfType<GameManager>()?.gameObject;
            if (playerBaseGo != null)
            {
                var base3dGo = playerBaseGo.transform.Find("Base3D");
                if (base3dGo != null)
                    base3dGo.position = basePosition;
            }

            if (waveManager != null)
            {
                var spawnTransforms = new Transform[spawnPositions.Count];
                for (int i = 0; i < spawnPositions.Count; i++)
                {
                    var spawnGo = new GameObject($"Spawner_{i}");
                    spawnGo.transform.position = spawnPositions[i];
                    spawnTransforms[i] = spawnGo.transform;
                }

                waveManager.Initialize(null, spawnTransforms);
                if (Logs) Debug.Log("[GameplayBootstrap] WaveManager initialized with spawn points");
            }
            else
            {
                if (Logs) Debug.LogWarning("[GameplayBootstrap] WaveManager not found!");
            }

            yield return null;
        }

        private IEnumerator InitializeSystems()
        {
            if (Logs) Debug.Log("[GameplayBootstrap] Initializing systems...");

            if (gameHUD != null)
            {
                gameHUD.Initialize();
            }
            else
            {
                if (Logs) Debug.LogWarning("[GameplayBootstrap] GameHUD not found!");
            }

            if (gameManager != null)
            {
                gameManager.Initialize();
            }
            else
            {
                if (Logs) Debug.LogError("[GameplayBootstrap] GameManager not found!");
            }

            yield return null;
        }

        private bool Logs = true;
    }
}
