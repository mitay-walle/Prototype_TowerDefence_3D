using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace GameJam.Plugins.Audio.Winds
{
    public class Wind : MonoBehaviour
    {
        [SerializeField] private Vector2 _perlinSpeedRange = new Vector2(.9f, 1f);
        [SerializeField] private float _perlinSpeedFrequency = .1f;
        [SerializeField, PropertyRange(0, 1)] public float External = .5f;
        [SerializeField, PropertyRange(0, 1), ReadOnly] public float ResultSpeed = .5f;
        [SerializeField] private Vector2 _perlinDirectionRange = new Vector2(0, 359);
        [SerializeField] private float _perlinDirectionFrequency = .002f;
        [SerializeField, OnValueChanged("DirectionChanged")] public Vector3 Direction = Vector3.forward;
        [SerializeField] private string _shaderGlobalVector2 = "_WindDirection2D";
        [SerializeField] private bool _simulateDirection = true;
        [SerializeField] private RangedAudioSource[] _sources;

        [SerializeField] private float _zoneMultiplier = 5;

        [SerializeField] private WindZone _zone;
        [ShowInInspector, ReadOnly] private float _rotationZ;

        public event Action<float> DirectionChangedDegree;
        public event Action<Vector3> DirectionChangedVector3;

        private Material _lineMaterial;

        private void Update()
        {
            Simulate();
            foreach (RangedAudioSource rangedAudioSource in _sources)
            {
                rangedAudioSource.OnUpdate(ResultSpeed);
            }
        }

        private void Simulate()
        {
            ResultSpeed = External * Mathf.Lerp(_perlinSpeedRange.x, _perlinSpeedRange.y,
                Mathf.PerlinNoise(Time.time * _perlinSpeedFrequency, 0));
            if (_simulateDirection)
            {
                _rotationZ = Mathf.Lerp(_perlinDirectionRange.x, _perlinDirectionRange.y,
                    Mathf.PerlinNoise(Time.time * _perlinDirectionFrequency, 0)) - 180;
                Direction = Quaternion.AngleAxis(_rotationZ, Vector3.up) * Vector3.forward;
                DirectionChanged();
            }

            if (_zone)
            {
                _zone.transform.forward = Direction;
                _zone.windMain = _zoneMultiplier * ResultSpeed;
            }
        }

        private void DirectionChanged()
        {
            _rotationZ = Mathf.Atan2(Direction.z, Direction.x) * Mathf.Rad2Deg;
            DirectionChangedDegree?.Invoke(_rotationZ);
            DirectionChangedVector3?.Invoke(Direction);
            if (!string.IsNullOrEmpty(_shaderGlobalVector2))
            {
                Shader.SetGlobalVector(_shaderGlobalVector2, Direction);
            }
        }

        #region Editor

#if UNITY_EDITOR
        [OnInspectorInit]
        private void OnInspectorInit()
        {
            _lineMaterial ??= new Material(Shader.Find("Hidden/Internal-Colored"));
        }

        [OnInspectorGUI]
        private void OnInspectorGUI()
        {
            // Begin to draw a horizontal layout, using the helpBox EditorStyle
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            Rect layoutRectangle = GUILayoutUtility.GetRect(10, 10000, 200, 200);
            GUILayout.Label("");
            Rect rect = GUILayoutUtility.GetLastRect();
            rect.y = layoutRectangle.center.y - 20;
            rect.x -= 15;
            rect.height += 2;
            rect.width += 13;
            GUI.Box(rect, "X");

            GUILayout.Box("");
            rect = GUILayoutUtility.GetLastRect();
            rect.x = layoutRectangle.center.x - 20;
            rect.height += 2;
            rect.width += 13;
            GUI.Box(rect, "Y");

            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag &&
                layoutRectangle.Contains(Event.current.mousePosition))
            {
                Undo.RecordObject(this, "rotate");
                Vector2 direction = (Event.current.mousePosition - layoutRectangle.center) /
                                    Mathf.Min(layoutRectangle.width, layoutRectangle.height) / 2;
                direction.y *= -1;
                Direction = new Vector3(direction.normalized.x, 0, direction.normalized.y);
                DirectionChanged();
            }

            if (Event.current.type == EventType.Repaint)
            {
                Vector2 direction = new Vector2(Direction.x, -Direction.z).normalized;
                Vector2 center = layoutRectangle.center;
                Vector2 endPoint = center + direction * Mathf.Min(layoutRectangle.width, layoutRectangle.height) / 2;
                Vector2 midCenterPoint = Vector2.Lerp(endPoint, center, .2f);
                Vector2 leftPoint = midCenterPoint + Vector2.Perpendicular(direction) * 10;
                Vector2 rightPoint = midCenterPoint - Vector2.Perpendicular(direction) * 10;
                Handles.DrawAAPolyLine(2, center, endPoint, leftPoint, rightPoint, endPoint);
                Handles.color /= 2;
                Rect rect2 = layoutRectangle;
                rect2.y = rect2.center.y;
                rect2.height = 0;
                Handles.DrawAAPolyLine(2, rect2.min, rect2.max);
                rect2 = layoutRectangle;
                rect2.x = rect2.center.x;
                rect2.width = 0;
                Handles.DrawAAPolyLine(2, rect2.min, rect2.max);
            }

            GUILayout.EndHorizontal();
        }
#endif

        #endregion
    }
}