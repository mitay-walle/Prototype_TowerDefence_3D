using UnityEngine;
using UnityEngine.InputSystem;

namespace TD.Interactions
{
	public class SelectionSystem : MonoBehaviour
	{
		private const string TOOLTIP_SPHERE_RADIUS = "Radius of sphere for Spherecast selection";

		public LayerMask raycastMask = ~0;
		public float maxRayDistance = 1000f;
		[Tooltip(TOOLTIP_SPHERE_RADIUS)]
		public float spherecastRadius = 0.5f;
		public RenderingLayerMask defaultRenderingLayer = 1;
		public RenderingLayerMask hoveredRenderingLayer = 2;
		public RenderingLayerMask selectedRenderingLayer = 4;

		private ITargetable currentSelected;
		private ITargetable currentHovered;
		private Camera cam;
		private InputAction selectAction;
		private bool isMouseActive = true;

		void Awake()
		{
			selectAction = new InputAction(binding: "<Mouse>/leftButton");
			selectAction.AddBinding("<Gamepad>/buttonSouth");
			selectAction.performed += OnSelect;
			cam = Camera.main;
		}

		void OnDestroy()
		{
			selectAction.performed -= OnSelect;
			selectAction.Dispose();
		}

		void OnEnable() => selectAction.Enable();

		void OnDisable() => selectAction.Disable();

		void Update()
		{
			UpdateInputDevice();
			UpdateHover();
		}

		private void UpdateInputDevice()
		{
			if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f)
			{
				isMouseActive = true;
			}
			else if (Gamepad.current != null && Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.1f)
			{
				isMouseActive = false;
			}
		}

		private Ray GetRay()
		{
			if (isMouseActive && Mouse.current != null)
			{
				return cam.ScreenPointToRay(Mouse.current.position.ReadValue());
			}
			else
			{
				return cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
			}
		}

		private void UpdateHover()
		{
			if (!IsValidTargetable(currentSelected))
			{
				currentSelected = null;
			}

			if (!IsValidTargetable(currentHovered))
			{
				currentHovered = null;
			}

			Ray ray = GetRay();

			if (Physics.SphereCast(ray, spherecastRadius, out RaycastHit hit, maxRayDistance, raycastMask))
			{
				var targetable = hit.collider.GetComponent<ITargetable>();

				if (targetable != null && targetable != currentSelected)
				{
					if (currentHovered != targetable)
					{
						UnhoverCurrent();
						HoverNew(targetable);
					}
				}
				else
				{
					UnhoverCurrent();
				}
			}
			else
			{
				UnhoverCurrent();
			}
		}

		private void OnSelect(InputAction.CallbackContext context)
		{
			if (context.control.device is Mouse)
			{
				isMouseActive = true;
			}
			else if (context.control.device is Gamepad)
			{
				isMouseActive = false;
			}

			Ray ray = GetRay();

			if (Physics.SphereCast(ray, spherecastRadius, out RaycastHit hit, maxRayDistance, raycastMask))
			{
				var selectable = hit.collider.GetComponent<ITargetable>();

				if (selectable != null)
				{
					if (currentSelected != selectable)
					{
						DeselectCurrent();
						SelectNew(selectable);
					}
				}
				else
				{
					DeselectCurrent();
				}
			}
			else
			{
				DeselectCurrent();
			}
		}

		private void HoverNew(ITargetable targetable)
		{
			currentHovered = targetable;
			if (IsValidTargetable(currentHovered))
			{
				SetRenderingLayer(currentHovered.gameObject, hoveredRenderingLayer);
			}
		}

		private void UnhoverCurrent()
		{
			if (currentHovered != null)
			{
				if (IsValidTargetable(currentHovered))
				{
					SetRenderingLayer(currentHovered.gameObject, defaultRenderingLayer);
				}
				currentHovered = null;
			}
		}

		private void SelectNew(ITargetable selectable)
		{
			if (currentHovered == selectable)
			{
				currentHovered = null;
			}

			currentSelected = selectable;
			if (IsValidTargetable(currentSelected))
			{
				currentSelected.OnSelected();
				SetRenderingLayer(currentSelected.gameObject, selectedRenderingLayer);

				var tooltip = currentSelected.gameObject.GetComponent<TD.UI.TooltipWorldBridge>();
				if (tooltip != null)
				{
					tooltip.ShowTooltip();
				}
			}
		}

		private void DeselectCurrent()
		{
			if (currentSelected != null)
			{
				if (IsValidTargetable(currentSelected))
				{
					currentSelected.OnDeselected();

					var tooltip = currentSelected.gameObject.GetComponent<TD.UI.TooltipWorldBridge>();
					if (tooltip != null)
					{
						tooltip.HideTooltip();
					}

					if (currentSelected == currentHovered)
					{
						SetRenderingLayer(currentSelected.gameObject, hoveredRenderingLayer);
					}
					else
					{
						SetRenderingLayer(currentSelected.gameObject, defaultRenderingLayer);
					}
				}

				currentSelected = null;
			}
		}

		private bool IsValidTargetable(ITargetable targetable)
		{
			if (targetable == null) return false;

			var monoBehaviour = targetable as MonoBehaviour;
			if (monoBehaviour == null) return false;

			return monoBehaviour != null && monoBehaviour.gameObject != null;
		}

		private void SetRenderingLayer(GameObject obj, uint layer)
		{
			var renderers = obj.GetComponentsInChildren<Renderer>();
			foreach (var renderer in renderers)
			{
				renderer.renderingLayerMask = layer;
			}
		}
	}
}
