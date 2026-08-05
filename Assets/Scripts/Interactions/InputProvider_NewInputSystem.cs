using UnityEngine;
using UnityEngine.InputSystem;

namespace TD.Interactions
{
public class InputProvider_NewInputSystem : MonoBehaviour, IRTSCInputProvider
{
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction rotateAction;
    private InputAction dragAction;
    private InputAction playerZoomAction;
    private InputAction scrollWheelAction;
    private InputAction pointAction;
    private InputAction heightUpAction;
    private InputAction heightDownAction;
    private InputAction rotateRightAction;
    private InputAction rotateLeftAction;

    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("InputProvider_NewInputSystem requires a PlayerInput component on the same GameObject.");
            return;
        }

        var actions = playerInput.actions;
        moveAction = actions.FindAction("Player/Move", true);
        lookAction = actions.FindAction("Player/Look", true);
        rotateAction = actions.FindAction("UI/MiddleClick", true);
        dragAction = actions.FindAction("UI/RightClick", true);
        playerZoomAction = actions.FindAction("Player/Zoom", true);
        scrollWheelAction = actions.FindAction("UI/ScrollWheel", true);
        pointAction = actions.FindAction("UI/Point", true);
        heightUpAction = actions.FindAction("Player/Tooltip Click", true);
        heightDownAction = actions.FindAction("Player/Tooltip Click1", true);
        rotateRightAction = actions.FindAction("Player/Next", true);
        rotateLeftAction = actions.FindAction("Player/Previous", true);

        actions.FindActionMap("Player", true).Enable();
        actions.FindActionMap("UI", true).Enable();
    }

    private void OnDestroy()
    {
        playerInput?.actions?.Disable();
    }

    public bool DragButtonInput() => dragAction != null && dragAction.IsPressed();

    public Vector2 MouseInput() => lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

    public Vector2 MousePosition()
    {
        var syntheticMouse = FindFirstObjectByType<SyntheticMouse>();
        if (syntheticMouse != null && syntheticMouse.isActiveAndEnabled && syntheticMouse.IsReady)
            return syntheticMouse.Position;

        foreach (var device in InputSystem.devices)
        {
            if (device is Mouse mouse && mouse.added && mouse.name != "TD Synthetic Mouse" && mouse.name != "VirtualMouse")
                return mouse.position.ReadValue();
        }

        return Vector2.zero;
    }

    public Vector2 MovementInput() => moveAction?.ReadValue<Vector2>() ?? Vector2.zero;

    public bool RotationButtonInput() => rotateAction != null && rotateAction.IsPressed();

    public float ZoomInput()
    {
        float scrollWheelInput = scrollWheelAction?.ReadValue<Vector2>().y ?? 0f;
        return scrollWheelInput != 0f ? scrollWheelInput : playerZoomAction?.ReadValue<float>() ?? 0f;
    }

    public bool HeightUpButtonInput() => heightUpAction != null && heightUpAction.IsPressed();

    public bool HeightDownButtonInput() => heightDownAction != null && heightDownAction.IsPressed();

    public bool RotateRightButtonInput() => rotateRightAction != null && rotateRightAction.IsPressed();

    public bool RotateLeftButtonInput() => rotateLeftAction != null && rotateLeftAction.IsPressed();
}
}
