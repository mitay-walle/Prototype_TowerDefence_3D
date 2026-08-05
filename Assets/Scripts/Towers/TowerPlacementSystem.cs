using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TD.GameLoop;
using TD.Interactions;
using TD.UI.Information;
using TD.Voxels;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

namespace TD.Towers
{
	public class TowerPlacementSystem : MonoBehaviour
	{
		[SerializeField] private InputActionReference _submit;
		[SerializeField] private InputActionReference _cancel;
		[SerializeField] private TD.Levels.TileMapManager _tileMapManager;
		[SerializeField] private LayerMask intersectMask = -1;
		public Material ghostMaterial;
		private GameObject ghostInstance;
		private GameObject currentPrefab;
		IRTSCInputProvider inputProvider;
		InputAction clickAction;
		[SerializeField] private float towerGridSize = 1f;
		private bool _placementRequested;

		public bool IsPlacing => ghostInstance != null;
		public UnityEvent<int, int> onPlacementPreviewChanged = new UnityEvent<int, int>();
		public UnityEvent<string> onTowerPlaced = new UnityEvent<string>();

		[SerializeField] private Camera cam;
		private int _previewCoveredEntrances = -1;
		private int _previewTotalEntrances = -1;
		private int _previewExistingCoveredEntrances = -1;
		private int _previewCandidateCoveredEntrances = -1;
		private readonly List<Vector3> _routeSamples = new List<Vector3>();
		private readonly List<Tower> _existingTowers = new List<Tower>();
		private int _previewCoveredRouteSamples = -1;
		private int _previewTotalRouteSamples = -1;
		private int _previewExistingCoveredRouteSamples = -1;
		private int _previewCandidateCoveredRouteSamples = -1;

		public int PreviewCoveredRouteSamples => _previewCoveredRouteSamples;
		public int PreviewTotalRouteSamples => _previewTotalRouteSamples;
		public int PreviewExistingCoveredEntrances => _previewExistingCoveredEntrances;
		public int PreviewCandidateCoveredEntrances => _previewCandidateCoveredEntrances;
		public int PreviewExistingCoveredRouteSamples => _previewExistingCoveredRouteSamples;
		public int PreviewCandidateCoveredRouteSamples => _previewCandidateCoveredRouteSamples;

		void Start()
		{
			_submit.action.Enable();
			_submit.action.started -= PlaceTower;
			_submit.action.started += PlaceTower;
			clickAction = _submit.action.actionMap.asset.FindAction("UI/Click", true);
			clickAction.performed -= PlaceTower;
			clickAction.performed += PlaceTower;
			clickAction.Enable();

			_cancel.action.Enable();
			_cancel.action.started -= CancelPlacement;
			_cancel.action.started += CancelPlacement;

			inputProvider = FindFirstObjectByType<InputProvider_NewInputSystem>();
		}

		void OnDestroy()
		{
			_submit.action.Disable();
			_cancel.action.Disable();
			_submit.action.started -= PlaceTower;
			_cancel.action.started -= CancelPlacement;
			if (clickAction != null)
			{
				clickAction.performed -= PlaceTower;
				clickAction.Disable();
			}
		}

		void Update()
		{
			if (!currentPrefab)
			{
				_placementRequested = false;
				return;
			}

			if (Mouse.current.rightButton.wasPressedThisFrame) // ПКМ — отмена
			{
				CancelPlacement();
				return;
			}

			Vector2 mousePosition = inputProvider?.MousePosition() ?? Vector2.zero;

			if (mousePosition == Vector2.zero) return;

			if (!TryGetTowerPlacementPoint(mousePosition, out var hitPoint))
				return;

			ghostInstance.transform.position = hitPoint;
			Physics.SyncTransforms();
			UpdatePlacementCoveragePreview(hitPoint);

			if (!_placementRequested)
				return;

			_placementRequested = false;
			if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
				return;

			TryPlaceTowerAtScreenPosition(mousePosition);
		}

		public void BeginPlacement(GameObject prefab)
		{
			CancelPlacement();

			var turret = prefab.GetComponent<Tower>();
			if (turret != null && turret.Stats != null)
			{
				int currentTowerCost = turret.Stats.statsSO.Cost;

				if (!ResourceManager.Instance.CanAfford(currentTowerCost))
				{
					Debug.LogWarning(
						$"TowerPlacement: Cannot afford tower (cost: {currentTowerCost}, current: {ResourceManager.Instance.CurrentCurrency})");

					return;
				}
			}

			currentPrefab = prefab;
			_existingTowers.Clear();
			var discoveredTowers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
			var excludedTowerCount = 0;
			for (var towerIndex = 0; towerIndex < discoveredTowers.Length; towerIndex++)
			{
				var tower = discoveredTowers[towerIndex];
				if (tower == null || !tower.isActiveAndEnabled)
				{
					excludedTowerCount++;
					continue;
				}

				_existingTowers.Add(tower);
			}
			_routeSamples.Clear();
			if (_tileMapManager != null)
			{
				_routeSamples.AddRange(BuildRouteSamples(
					_tileMapManager.SpawnPositions,
					_tileMapManager.BasePosition,
					Mathf.Max(0.5f, _tileMapManager.TileSize * 0.25f)));
			}
			Debug.Log($"[TowerPlacementCoverage] existingTowers={_existingTowers.Count};excludedTowers={excludedTowerCount};routeSamples={_routeSamples.Count};entrances={(_tileMapManager != null ? _tileMapManager.SpawnPositions.Count : 0)}");
			ghostInstance = Instantiate(prefab);
			ghostInstance.transform.eulerAngles = new(0, -90);
			FindAnyObjectByType<AutoPositionalTooltip>()?.Hide();
			ghostInstance.GetComponent<VoxelGenerator>().Generate();

			ghostInstance.name = prefab.name + "_Ghost";
			MakeDummyGraphicOnlyPrefab(ghostInstance);
			ghostInstance.transform.DOPunchScale(Vector3.one, .5f);
			ghostInstance.transform.DORotate(default, .5f);
		}

		void MakeDummyGraphicOnlyPrefab(GameObject go)
		{
			var behs = ghostInstance.GetComponents<MonoBehaviour>();
			foreach (MonoBehaviour beh in behs)
			{
				if (beh is Tower or VoxelGenerator)
				{
					beh.enabled = false;
					continue;
				}

				if (Application.isPlaying)
					Destroy(beh);
				else
					DestroyImmediate(beh);
			}

			MeshRenderer[] rends = go.GetComponentsInChildren<MeshRenderer>();
			for (var i = 0; i < rends.Length; i++)
			{
				MeshRenderer r = rends[i];
				r.sharedMaterials = Enumerable.Repeat(ghostMaterial, r.sharedMaterials.Length).ToArray();
			}

			if (ghostInstance.TryGetComponent<Collider>(out var col))
			{
				col.isTrigger = true;
			}

			if (!ghostInstance.GetComponent<TriggerIntersectColor>())
			{
				ghostInstance.AddComponent<TriggerIntersectColor>().layerMask = intersectMask;
			}

			if (!ghostInstance.GetComponent<Rigidbody>())
			{
				var rb = ghostInstance.AddComponent<Rigidbody>();
				rb.useGravity = false;
				rb.isKinematic = true;
			}

			ghostInstance.GetComponent<Tower>().TowerStatsVisual.Show(ghostInstance.GetComponent<Tower>());
		}

		void PlaceTower(CallbackContext obj) => PlaceTower();

		void PlaceTower()
		{
			if (ghostInstance && _tileMapManager != null && cam != null && inputProvider != null)
				_placementRequested = true;
		}

		public bool TryPlaceTowerAtScreenPosition(Vector2 mousePosition)
		{
			if (!ghostInstance || currentPrefab == null || _tileMapManager == null || cam == null)
				return false;

			if (!TryGetTowerPlacementPoint(mousePosition, out var hitPoint))
			{
				Debug.Log($"[TowerPlacement] Rejected reason=surface-point-unavailable;screen={mousePosition}");
				return false;
			}

			ghostInstance.transform.position = hitPoint;
			Physics.SyncTransforms();
			UpdatePlacementCoveragePreview(hitPoint);

			if (HasBlockingIntersection())
			{
				Debug.Log($"[TowerPlacement] Rejected reason=blocking-intersection;position={hitPoint}");
				return false;
			}

			var sourceTower = currentPrefab.GetComponent<Tower>();
			var currentTowerCost = sourceTower != null && sourceTower.Stats != null && sourceTower.Stats.statsSO != null
				? sourceTower.Stats.statsSO.Cost
				: 0;
			Debug.Log($"[TowerPlacement] Commit tower={currentPrefab.name} cost={currentTowerCost} currency={(ResourceManager.Instance != null ? ResourceManager.Instance.CurrentCurrency : -1)} coverage={_previewCoveredEntrances}/{_previewTotalEntrances} routeCoverage={FormatCoverage(_previewCoveredRouteSamples, _previewTotalRouteSamples)} position={hitPoint}");
			if (ResourceManager.Instance != null && currentTowerCost > 0 && !ResourceManager.Instance.TrySpend(currentTowerCost))
			{
				Debug.LogWarning("TowerPlacement: Cannot afford tower anymore!");
				CancelPlacement();
				return false;
			}

			var placedPosition = ghostInstance.transform.position;
			Instantiate(currentPrefab, placedPosition, ghostInstance.transform.rotation);
			var currencyAfter = ResourceManager.Instance != null ? ResourceManager.Instance.CurrentCurrency : -1;
			var placementDetails = $"tower={currentPrefab.name};cost={currentTowerCost};coverage={_previewCoveredEntrances}/{_previewTotalEntrances};routeCoverage={FormatCoverage(_previewCoveredRouteSamples, _previewTotalRouteSamples)};position={placedPosition};currencyAfter={currencyAfter}";
			CancelPlacement();
			Debug.Log($"[TowerPlacement] Committed {placementDetails}");
			onTowerPlaced?.Invoke(placementDetails);
			return true;
		}

		public static int CountCoveredEntrances(Vector3 towerPosition, float range, IReadOnlyList<Vector3> entrances)
		{
			if (range < 0f || entrances == null)
				return 0;

			var coveredEntrances = 0;
			for (var entranceIndex = 0; entranceIndex < entrances.Count; entranceIndex++)
			{
				if (Vector3.Distance(towerPosition, entrances[entranceIndex]) <= range)
					coveredEntrances++;
			}

			return coveredEntrances;
		}

		public static int CountCoveredEntrances(IReadOnlyList<Tower> towers, IReadOnlyList<Vector3> entrances)
		{
			if (towers == null || entrances == null)
				return 0;

			var coveredEntrances = 0;
			for (var entranceIndex = 0; entranceIndex < entrances.Count; entranceIndex++)
			{
				if (IsCoveredByTowers(towers, entrances[entranceIndex]))
					coveredEntrances++;
			}

			return coveredEntrances;
		}

		public static int CountCoveredEntrances(
			IReadOnlyList<Tower> towers,
			Vector3 candidatePosition,
			float candidateRange,
			IReadOnlyList<Vector3> entrances)
		{
			if (entrances == null || candidateRange < 0f)
				return 0;

			var coveredEntrances = 0;
			for (var entranceIndex = 0; entranceIndex < entrances.Count; entranceIndex++)
			{
				var entrance = entrances[entranceIndex];
				if (IsCoveredByTowers(towers, entrance) || Vector3.Distance(candidatePosition, entrance) <= candidateRange)
					coveredEntrances++;
			}

			return coveredEntrances;
		}

		public static int CountCoveredRouteSamples(Vector3 towerPosition, float range, IReadOnlyList<Vector3> routeSamples)
		{
			if (range < 0f || routeSamples == null)
				return 0;

			var coveredSamples = 0;
			for (var sampleIndex = 0; sampleIndex < routeSamples.Count; sampleIndex++)
			{
				if (Vector3.Distance(towerPosition, routeSamples[sampleIndex]) <= range)
					coveredSamples++;
			}

			return coveredSamples;
		}

		public static int CountCoveredRouteSamples(IReadOnlyList<Tower> towers, IReadOnlyList<Vector3> routeSamples)
		{
			if (towers == null || routeSamples == null)
				return 0;

			var coveredSamples = 0;
			for (var sampleIndex = 0; sampleIndex < routeSamples.Count; sampleIndex++)
			{
				if (IsCoveredByTowers(towers, routeSamples[sampleIndex]))
					coveredSamples++;
			}

			return coveredSamples;
		}

		public static int CountCoveredRouteSamples(
			IReadOnlyList<Tower> towers,
			Vector3 candidatePosition,
			float candidateRange,
			IReadOnlyList<Vector3> routeSamples)
		{
			if (routeSamples == null || candidateRange < 0f)
				return 0;

			var coveredSamples = 0;
			for (var sampleIndex = 0; sampleIndex < routeSamples.Count; sampleIndex++)
			{
				var sample = routeSamples[sampleIndex];
				if (IsCoveredByTowers(towers, sample) || Vector3.Distance(candidatePosition, sample) <= candidateRange)
					coveredSamples++;
			}

			return coveredSamples;
		}

		public static List<Vector3> BuildRouteSamples(
			IReadOnlyList<Vector3> entrances,
			Vector3 basePosition,
			float sampleSpacing)
		{
			var samples = new List<Vector3>();
			if (entrances == null || entrances.Count == 0 || sampleSpacing <= 0f)
				return samples;

			if (!NavMesh.SamplePosition(basePosition, out var baseHit, 2f, NavMesh.AllAreas))
				return samples;

			var path = new NavMeshPath();
			for (var entranceIndex = 0; entranceIndex < entrances.Count; entranceIndex++)
			{
				if (!NavMesh.SamplePosition(entrances[entranceIndex], out var entranceHit, 2f, NavMesh.AllAreas) ||
					!NavMesh.CalculatePath(entranceHit.position, baseHit.position, NavMesh.AllAreas, path) ||
					path.status != NavMeshPathStatus.PathComplete)
					continue;

				var corners = path.corners;
				if (corners == null || corners.Length < 2)
					continue;

				for (var cornerIndex = 0; cornerIndex < corners.Length - 1; cornerIndex++)
				{
					var start = corners[cornerIndex];
					var end = corners[cornerIndex + 1];
					var distance = Vector3.Distance(start, end);
					var steps = Mathf.Max(1, Mathf.CeilToInt(distance / sampleSpacing));

					for (var step = 0; step < steps; step++)
						AddRouteSample(samples, Vector3.Lerp(start, end, (float)step / steps));
				}

				AddRouteSample(samples, corners[corners.Length - 1]);
			}

			return samples;
		}

		private static void AddRouteSample(List<Vector3> samples, Vector3 sample)
		{
			if (samples.Count == 0 || Vector3.Distance(samples[samples.Count - 1], sample) > 0.01f)
				samples.Add(sample);
		}

		private static bool IsCoveredByTowers(IReadOnlyList<Tower> towers, Vector3 point)
		{
			if (towers == null)
				return false;

			for (var towerIndex = 0; towerIndex < towers.Count; towerIndex++)
			{
				var tower = towers[towerIndex];
				if (tower != null && tower.enabled && Vector3.Distance(tower.transform.position, point) <= tower.EffectiveRange)
					return true;
			}

			return false;
		}

		private static string FormatCoverage(int covered, int total)
		{
			return total > 0 ? $"{covered}/{total}" : "unavailable";
		}

		private void UpdatePlacementCoveragePreview(Vector3 placementPosition)
		{
			var tower = ghostInstance != null ? ghostInstance.GetComponent<Tower>() : null;
			var entrances = _tileMapManager != null ? _tileMapManager.SpawnPositions : null;
			var towers = _existingTowers;
			var totalEntrances = entrances != null ? entrances.Count : 0;
			var existingCoveredEntrances = CountCoveredEntrances(towers, entrances);
			var candidateCoveredEntrances = tower != null
				? CountCoveredEntrances(placementPosition, tower.EffectiveRange, entrances)
				: 0;
			var coveredEntrances = tower != null
				? CountCoveredEntrances(towers, placementPosition, tower.EffectiveRange, entrances)
				: existingCoveredEntrances;
			var totalRouteSamples = _routeSamples.Count;
			var existingCoveredRouteSamples = CountCoveredRouteSamples(towers, _routeSamples);
			var candidateCoveredRouteSamples = tower != null
				? CountCoveredRouteSamples(placementPosition, tower.EffectiveRange, _routeSamples)
				: 0;
			var coveredRouteSamples = tower != null
				? CountCoveredRouteSamples(towers, placementPosition, tower.EffectiveRange, _routeSamples)
				: existingCoveredRouteSamples;
			if (coveredEntrances == _previewCoveredEntrances && totalEntrances == _previewTotalEntrances &&
				existingCoveredEntrances == _previewExistingCoveredEntrances && candidateCoveredEntrances == _previewCandidateCoveredEntrances &&
				coveredRouteSamples == _previewCoveredRouteSamples && totalRouteSamples == _previewTotalRouteSamples &&
				existingCoveredRouteSamples == _previewExistingCoveredRouteSamples && candidateCoveredRouteSamples == _previewCandidateCoveredRouteSamples)
				return;

			_previewCoveredEntrances = coveredEntrances;
			_previewTotalEntrances = totalEntrances;
			_previewExistingCoveredEntrances = existingCoveredEntrances;
			_previewCandidateCoveredEntrances = candidateCoveredEntrances;
			_previewCoveredRouteSamples = coveredRouteSamples;
			_previewTotalRouteSamples = totalRouteSamples;
			_previewExistingCoveredRouteSamples = existingCoveredRouteSamples;
			_previewCandidateCoveredRouteSamples = candidateCoveredRouteSamples;
			tower?.TowerStatsVisual.SetCoverageFeedback(
				totalRouteSamples > 0 ? coveredRouteSamples : coveredEntrances,
				totalRouteSamples > 0 ? totalRouteSamples : totalEntrances);
			onPlacementPreviewChanged?.Invoke(coveredEntrances, totalEntrances);
		}

		private bool HasBlockingIntersection()
		{
			var intersectColor = ghostInstance.GetComponent<TriggerIntersectColor>();
			if (intersectColor != null && intersectColor.IsIntersected)
				return true;

			var colliders = ghostInstance.GetComponentsInChildren<Collider>();
			for (var i = 0; i < colliders.Length; i++)
			{
				var collider = colliders[i];
				if (collider == null)
					continue;

				var overlaps = Physics.OverlapBox(collider.bounds.center, collider.bounds.extents, Quaternion.identity, intersectMask, QueryTriggerInteraction.Collide);
				for (var overlapIndex = 0; overlapIndex < overlaps.Length; overlapIndex++)
				{
					var overlap = overlaps[overlapIndex];
					if (overlap != null && overlap.transform != ghostInstance.transform && !overlap.transform.IsChildOf(ghostInstance.transform))
						return true;
				}
			}

			return false;
		}

		bool TryGetTowerPlacementPoint(Vector2 mousePosition, out Vector3 placementPoint)
		{
			placementPoint = default;
			if (_tileMapManager == null || cam == null || towerGridSize <= 0f)
				return false;

			Ray ray = cam.ScreenPointToRay(mousePosition);
			if (!_tileMapManager.TryGetTileSurfacePoint(ray, out var hitPoint, out _))
				return false;

			var snappedPoint = new Vector3(
				Mathf.Round(hitPoint.x / towerGridSize) * towerGridSize,
				hitPoint.y + 50f,
				Mathf.Round(hitPoint.z / towerGridSize) * towerGridSize);
			return _tileMapManager.TryGetTileSurfacePoint(
				new Ray(snappedPoint, Vector3.down), out placementPoint, out _);
		}

		void CancelPlacement(CallbackContext obj) => CancelPlacement();

		public void CancelPlacement()
		{
			_placementRequested = false;
			if (ghostInstance)
			{
				ghostInstance.transform.DOKill();
				if (Application.isPlaying)
					Destroy(ghostInstance);
				else
					DestroyImmediate(ghostInstance);
			}

			ghostInstance = null;
			currentPrefab = null;
			_existingTowers.Clear();
			_previewCoveredEntrances = -1;
			_previewTotalEntrances = -1;
			_previewExistingCoveredEntrances = -1;
			_previewCandidateCoveredEntrances = -1;
			_previewCoveredRouteSamples = -1;
			_previewTotalRouteSamples = -1;
			_previewExistingCoveredRouteSamples = -1;
			_previewCandidateCoveredRouteSamples = -1;
			_routeSamples.Clear();
		}
	}
}
