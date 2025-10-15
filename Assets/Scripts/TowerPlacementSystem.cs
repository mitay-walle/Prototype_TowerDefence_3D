using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

namespace TD
{
	public class TowerPlacementSystem : MonoBehaviour
	{
		[SerializeField] private InputActionReference _submit;
		[SerializeField] private InputActionReference _cancel;
		public LayerMask groundMask;
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

			Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
			if (Physics.Raycast(ray, out var hit, 500f, groundMask))
			{
				ghostInstance.transform.position = hit.point;
			}

			if (Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
			{
				PlaceTower();
			}
		}

		public void BeginPlacement(GameObject prefab)
		{
			CancelPlacement();

			currentPrefab = prefab;
			ghostInstance = Instantiate(prefab);
			ghostInstance.GetComponent<VoxelGenerator>().Generate();
			Destroy(ghostInstance.GetComponent<ITargetable>() as Component);
			ghostInstance.name = prefab.name + "_Ghost";
			ApplyGhostMaterials(ghostInstance);
		}

		void ApplyGhostMaterials(GameObject go)
		{
			MeshRenderer[] rends = go.GetComponentsInChildren<MeshRenderer>();
			for (var i = 0; i < rends.Length; i++)
			{
				MeshRenderer r = rends[i];
				r.sharedMaterials = Enumerable.Repeat(ghostMaterial, r.sharedMaterials.Length).ToArray();
			}
		}

		void PlaceTower(CallbackContext obj) => PlaceTower();

		void PlaceTower()
		{
			if (!ghostInstance) return;

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