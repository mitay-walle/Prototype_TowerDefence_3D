using System.Linq;
using DG.Tweening;
using TD.GameLoop;
using TD.Interactions;
using TD.UI.Information;
using TD.Voxels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

namespace TD.Towers
{
	public class TowerPlacementSystem : MonoBehaviour
	{
		[SerializeField] private InputActionReference _submit;
		[SerializeField] private InputActionReference _cancel;
		[SerializeField] private LayerMask groundMask = -1;
		[SerializeField] private LayerMask intersectMask = -1;
		public Material ghostMaterial;
		private GameObject ghostInstance;
		private GameObject currentPrefab;

		public bool IsPlacing => ghostInstance != null;

		Camera cam;

		void Start()
		{
			_submit.action.Enable();
			_submit.action.started -= PlaceTower;
			_submit.action.started += PlaceTower;

			_cancel.action.Enable();
			_cancel.action.started -= CancelPlacement;
			_cancel.action.started += CancelPlacement;

			cam = Camera.main;
		}

		void OnDestroy()
		{
			_submit.action.Disable();
			_cancel.action.Disable();
			_submit.action.started -= PlaceTower;
			_cancel.action.started -= CancelPlacement;
		}

		void Update()
		{
			if (!currentPrefab) return;

			if (Mouse.current.rightButton.wasPressedThisFrame) // ПКМ — отмена
			{
				CancelPlacement();
				return;
			}

			Vector2 mousePosition = Mouse.current.position.ReadValue();

			if (mousePosition == Vector2.zero) return;

			Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
			if (Physics.Raycast(ray, out var hit, 500f, groundMask))
			{
				Vector3 p = hit.point;
				p = new Vector3(Mathf.Round(p.x), p.y + 1, Mathf.Round(p.z));
				Physics.Raycast(new Ray(p, Vector3.down), out var hit2, 100, groundMask);
				p = hit2.point;
				ghostInstance.GetComponent<Rigidbody>().position = p;
			}

			if (Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
			{
				PlaceTower();
			}
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
				if (beh is Tower)
				{
					beh.enabled = false;
					continue;
				}

				Destroy(beh);
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

				//rb.isKinematic = true;
			}

			ghostInstance.GetComponent<Tower>().TowerStatsVisual.Show(currentPrefab.GetComponent<Tower>().Stats);
		}

		void PlaceTower(CallbackContext obj) => PlaceTower();

		void PlaceTower()
		{
			if (!ghostInstance) return;
			if (ghostInstance.GetComponent<TriggerIntersectColor>().IsIntersected) return;

			int currentTowerCost = ghostInstance.GetComponent<Tower>().Stats.statsSO.Cost;
			if (ResourceManager.Instance != null && currentTowerCost > 0)
			{
				if (!ResourceManager.Instance.TrySpend(currentTowerCost))
				{
					Debug.LogWarning("TowerPlacement: Cannot afford tower anymore!");
					CancelPlacement();
					return;
				}
			}

			Instantiate(currentPrefab, ghostInstance.transform.position, ghostInstance.transform.rotation);
			CancelPlacement();
		}

		void CancelPlacement(CallbackContext obj) => CancelPlacement();

		void CancelPlacement()
		{
			if (ghostInstance) Destroy(ghostInstance);
			currentPrefab = null;
		}
	}
}