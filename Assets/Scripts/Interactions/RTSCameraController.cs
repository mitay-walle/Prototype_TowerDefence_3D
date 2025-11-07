// 12.10.2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using TD.Plugins.Runtime;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TD.Interactions
{
	[RequireComponent(typeof(CinemachineCamera))]
	public class RTSCameraController : MonoBehaviour
	{
		public DisablerList DisablerList = new();
		[SerializeField] private float moveSpeed = 10f;
		[SerializeField] private float maxEdgeScrollSpeed = 15f;
		[SerializeField] private float edgeScrollThreshold = 10f;

		[SerializeField] private float zoomSpeed = 5f;
		[SerializeField] private float minZoom = 5f;
		[SerializeField] private float maxZoom = 20f;

		[SerializeField] private float gamepadMoveSpeed = 15f;
		[SerializeField] private float gamepadZoomSpeed = 8f;

		private Transform target;
		private CinemachineCamera virtualCamera;
		private Vector3 dragStartPoint;
		private bool isDragging = false;
		private bool isUsingGamepad = false;

		private Camera mainCamera;

		private void Awake()
		{
			virtualCamera = GetComponent<CinemachineCamera>();
			target = (virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer).FollowTarget;
			mainCamera = Camera.main;
			DisablerList.Init(hasEntries => enabled = !hasEntries);
		}

		private void Update()
		{
			DetectInputMethod();

			if (isUsingGamepad)
			{
				CenterMouseCursor();
				HandleGamepadMovement();
				HandleGamepadZoom();
			}
			else
			{
				HandleEdgeScrolling();
				HandleDragging();
				HandleZoom();
				HandleKeyboardMovement();
			}
		}

		private void DetectInputMethod()
		{
			var gamepad = Gamepad.current;

			if (gamepad != null)
			{
				Vector2 leftStick = gamepad.leftStick.ReadValue();
				float rightStickY = gamepad.rightStick.ReadValue().y;

				if (leftStick.sqrMagnitude > 0.01f || Mathf.Abs(rightStickY) > 0.01f)
				{
					if (!isUsingGamepad)
					{
						isUsingGamepad = true;
						Cursor.visible = false;
						Cursor.lockState = CursorLockMode.Locked;
					}
				}
			}

			if (Mouse.current != null)
			{
				if (Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f || Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed)
				{
					if (isUsingGamepad)
					{
						isUsingGamepad = false;
						Cursor.visible = true;
						Cursor.lockState = CursorLockMode.None;
					}
				}
			}

			var keyboard = Keyboard.current;
			if (keyboard != null)
			{
				if (keyboard.wKey.isPressed || keyboard.sKey.isPressed || keyboard.aKey.isPressed || keyboard.dKey.isPressed)
				{
					if (isUsingGamepad)
					{
						isUsingGamepad = false;
						Cursor.visible = true;
						Cursor.lockState = CursorLockMode.None;
					}
				}
			}
		}

		private void CenterMouseCursor()
		{
			if (Mouse.current != null)
			{
				Mouse.current.WarpCursorPosition(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
			}
		}

		private void HandleGamepadMovement()
		{
			var gamepad = Gamepad.current;
			if (gamepad == null) return;

			Vector2 leftStick = gamepad.leftStick.ReadValue();
			Vector3 direction = new Vector3(leftStick.x, 0, leftStick.y);
			(virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer).FollowTarget.position +=
				direction * gamepadMoveSpeed * Time.deltaTime;
		}

		private void HandleGamepadZoom()
		{
			var gamepad = Gamepad.current;
			if (gamepad == null) return;

			float rightStickY = gamepad.rightStick.ReadValue().y;

			if (Mathf.Abs(rightStickY) > 0.01f)
			{
				CinemachineComponentBase component = virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Body);
				if (component is CinemachinePositionComposer framingTransposer)
				{
					framingTransposer.CameraDistance = Mathf.Clamp(framingTransposer.CameraDistance - rightStickY * gamepadZoomSpeed * Time.deltaTime,
						minZoom, maxZoom);
				}
			}
		}

		private bool IsCursorInsideGameView()
		{
			Vector2 mousePosition = Mouse.current.position.ReadValue();
			return mousePosition.x >= 0 && mousePosition.x <= Screen.width && mousePosition.y >= 0 && mousePosition.y <= Screen.height;
		}

		private void HandleEdgeScrolling()
		{
			if (Mouse.current.rightButton.isPressed) return;
			if (!IsCursorInsideGameView()) return;

			Vector2 mousePosition = Mouse.current.position.ReadValue();
			Vector3 direction = Vector3.zero;

			if (mousePosition.x >= Screen.width - edgeScrollThreshold)
			{
				float t = (mousePosition.x - (Screen.width - edgeScrollThreshold)) / edgeScrollThreshold;
				direction.x += Mathf.Lerp(0, 1, t);
			}
			else if (mousePosition.x <= edgeScrollThreshold)
			{
				float t = (edgeScrollThreshold - mousePosition.x) / edgeScrollThreshold;
				direction.x -= Mathf.Lerp(0, 1, t);
			}

			if (mousePosition.y >= Screen.height - edgeScrollThreshold)
			{
				float t = (mousePosition.y - (Screen.height - edgeScrollThreshold)) / edgeScrollThreshold;
				direction.z += Mathf.Lerp(0, 1, t);
			}
			else if (mousePosition.y <= edgeScrollThreshold)
			{
				float t = (edgeScrollThreshold - mousePosition.y) / edgeScrollThreshold;
				direction.z -= Mathf.Lerp(0, 1, t);
			}

			target.position += direction.normalized * maxEdgeScrollSpeed * Time.deltaTime;
		}

		private void HandleDragging()
		{
			if (Mouse.current.rightButton.isPressed)
			{
				if (!isDragging)
				{
					isDragging = true;

					Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
					if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float enter))
					{
						dragStartPoint = ray.GetPoint(enter);
					}
				}
				else
				{
					Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
					if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float enter))
					{
						Vector3 dragCurrentPoint = ray.GetPoint(enter);
						Vector3 dragOffset = dragStartPoint - dragCurrentPoint;
						target.position += new Vector3(dragOffset.x, 0, dragOffset.z);
					}
				}
			}
			else
			{
				isDragging = false;
			}
		}

		private void HandleZoom()
		{
			float scrollValue = Mouse.current.scroll.ReadValue().y;
			if (scrollValue != 0)
			{
				CinemachineComponentBase component = virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Body);
				if (component is CinemachinePositionComposer framingTransposer)
				{
					framingTransposer.CameraDistance = Mathf.Clamp(framingTransposer.CameraDistance - scrollValue * zoomSpeed * Time.deltaTime,
						minZoom, maxZoom);
				}
			}
		}

		private void HandleKeyboardMovement()
		{
			var keyboard = Keyboard.current;
			if (keyboard == null) return;

			Vector3 direction = Vector3.zero;

			if (keyboard.wKey.isPressed) direction.z += 1;
			if (keyboard.sKey.isPressed) direction.z -= 1;
			if (keyboard.aKey.isPressed) direction.x -= 1;
			if (keyboard.dKey.isPressed) direction.x += 1;

			target.position += direction.normalized * moveSpeed * Time.deltaTime;
		}
	}
}