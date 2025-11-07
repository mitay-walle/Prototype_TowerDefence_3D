using Sirenix.OdinInspector;
using UnityEngine;

namespace TD.Plugins.Timing
{
    public class PauseTime : MonoBehaviour
    {
        [ShowInInspector, HideInEditorMode] private TimeControl TimeControl => TimeControl.Instance;
        private void OnEnable() => TimeControl.Instance.Pause.Add(this);
        private void OnDisable() => TimeControl.Instance.Pause.Remove(this);
    }
}