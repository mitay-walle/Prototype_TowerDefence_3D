using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TD
{
	public class TowerPlacementSystem : MonoBehaviour
	{
		public LayerMask groundMask;
		public Material ghostMaterial;
		private GameObject ghostInstance;
		private GameObject currentPrefab;

		Camera cam;

		void Start() => cam = Camera.main;

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

		void PlaceTower()
		{
			Instantiate(currentPrefab, ghostInstance.transform.position, ghostInstance.transform.rotation);
			CancelPlacement();
		}

		void CancelPlacement()
		{
			if (ghostInstance) Destroy(ghostInstance);
			currentPrefab = null;
		}
	}
}