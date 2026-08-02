using UnityEngine;
using UnityEngine.U2D.Animation;

namespace TD.Towers
{
    [ExecuteAlways]
    public sealed class DirectionalSpriteBillboard : MonoBehaviour
    {
        private const string DirectionCategory = "Direction";

        private static readonly string[] DirectionLabels =
        {
            "East",
            "North-East",
            "North",
            "North-West",
            "West",
            "South-West",
            "South",
            "South-East"
        };

        private static readonly Vector3[] DirectionVectors =
        {
            Vector3.right,
            new Vector3(1f, 0f, 1f).normalized,
            Vector3.forward,
            new Vector3(-1f, 0f, 1f).normalized,
            Vector3.left,
            new Vector3(-1f, 0f, -1f).normalized,
            Vector3.back,
            new Vector3(1f, 0f, -1f).normalized
        };

        [SerializeField] private SpriteResolver _colorResolver;
        [SerializeField] private SpriteResolver _shadowResolver;

        private Light _sun;
        private int _colorDirection = -1;
        private int _shadowDirection = -1;

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying)
            {
                LateUpdate();
            }
        }
#endif
        private void LateUpdate()
        {
            Camera targetCamera = Camera.current ?? Camera.main;
            if (targetCamera != null)
            {
                UpdateBillboard(_colorResolver, targetCamera.transform.position - transform.position, ref _colorDirection);
            }

            _sun = _sun != null ? _sun : RenderSettings.sun;
            if (_sun != null)
            {
                UpdateBillboard(_shadowResolver, -_sun.transform.forward, ref _shadowDirection);
            }
        }

        private void UpdateBillboard(SpriteResolver resolver, Vector3 direction, ref int previousDirection)
        {
            if (resolver == null)
            {
                return;
            }

            Vector3 objectUp = transform.up;
            Vector3 flatWorldDirection = Vector3.ProjectOnPlane(direction, objectUp);
            if (flatWorldDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            flatWorldDirection.Normalize();
            Vector3 localDirection = new Vector3(
                Vector3.Dot(flatWorldDirection, transform.right),
                0f,
                Vector3.Dot(flatWorldDirection, transform.forward));
            Vector3 worldFacingDirection =
                (transform.right * localDirection.x + transform.forward * localDirection.z).normalized;

            resolver.transform.rotation = Quaternion.LookRotation(worldFacingDirection, objectUp);

            int directionIndex = GetDirectionIndex(localDirection);
            if (directionIndex == previousDirection)
            {
                return;
            }

            resolver.SetCategoryAndLabel(DirectionCategory, DirectionLabels[directionIndex]);
            previousDirection = directionIndex;
        }

        private static int GetDirectionIndex(Vector3 direction)
        {
            int closestDirection = 0;
            float closestDot = Vector3.Dot(direction, DirectionVectors[0]);

            for (int index = 1; index < DirectionVectors.Length; index++)
            {
                float dot = Vector3.Dot(direction, DirectionVectors[index]);
                if (dot > closestDot)
                {
                    closestDot = dot;
                    closestDirection = index;
                }
            }

            return closestDirection;
        }
    }
}