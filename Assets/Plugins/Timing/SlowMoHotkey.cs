using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

namespace TD.Plugins.Timing
{
    public class SlowMoHotkey : MonoBehaviour
    {
        [SerializeField, Required] private InputActionReference hotkey;
        [ShowInInspector] private TimeControl TimeControl => TimeControl.Instance;

        void Start()
        {
            if (!hotkey) return;
            hotkey.action.Enable();
            hotkey.action.started += Enable;
            hotkey.action.canceled += Disable;
        }

        void Enable(CallbackContext context) => TimeControl.Instance.SlowMo.Add(this);
        void Disable(CallbackContext context) => TimeControl.Instance.SlowMo.Remove(this);
    }
}