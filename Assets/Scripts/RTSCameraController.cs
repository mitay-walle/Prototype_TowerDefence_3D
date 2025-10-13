// 12.10.2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class RTSCameraController : MonoBehaviour
{
    [Header("Cinemachine")]
    public CinemachineCamera virtualCamera;

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float maxEdgeScrollSpeed = 15f;
    public float edgeScrollThreshold = 10f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minZoom = 5f;
    public float maxZoom = 20f;

    private Vector2 moveInput;
    private Vector3 dragStartPoint;
    private bool isDragging = false;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        HandleEdgeScrolling();
        HandleDragging();
        HandleZoom();
        HandleKeyboardMovement();
    }

    private bool IsCursorInsideGameView()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        return mousePosition.x >= 0 && mousePosition.x <= Screen.width &&
               mousePosition.y >= 0 && mousePosition.y <= Screen.height;
    }

    private void HandleEdgeScrolling()
    {
        if (Mouse.current.rightButton.isPressed) return;
        if (!IsCursorInsideGameView()) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 direction = Vector3.zero;

        // Calculate horizontal edge scrolling
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

        // Calculate vertical edge scrolling
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

        transform.position += direction.normalized * maxEdgeScrollSpeed * Time.deltaTime;
    }

    private void HandleDragging()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            if (!isDragging)
            {
                isDragging = true;

                // Record the initial drag start point in the world space
                Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float enter))
                {
                    dragStartPoint = ray.GetPoint(enter);
                }
            }
            else
            {
                // Calculate the new drag position in the world space
                Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float enter))
                {
                    Vector3 dragCurrentPoint = ray.GetPoint(enter);

                    // Calculate the difference and move the camera
                    Vector3 dragOffset = dragStartPoint - dragCurrentPoint;
                    transform.position += new Vector3(dragOffset.x, 0, dragOffset.z);
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
                framingTransposer.CameraDistance = Mathf.Clamp(
                    framingTransposer.CameraDistance - scrollValue * zoomSpeed * Time.deltaTime,
                    minZoom,
                    maxZoom
                );
            }
        }
    }

    private void HandleKeyboardMovement()
    {
        Vector3 direction = new Vector3(moveInput.x, 0, moveInput.y);
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // This is used for mouse delta but is not necessary unless implementing free look
    }
}