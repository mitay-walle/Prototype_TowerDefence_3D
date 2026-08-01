using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace TD.Rendering
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteShadowCaster3D : MonoBehaviour
    {
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        [SerializeField] private Material shadowMaterial;
        [SerializeField] private float _shadowOffset = .25f;

        private SpriteRenderer _spriteRenderer;
        [SerializeField, ReadOnly] private MeshFilter _meshFilter;
        [SerializeField, ReadOnly] private MeshRenderer _meshRenderer;
        private Mesh _mesh;
        private MaterialPropertyBlock _propertyBlock;
        private Sprite _currentSprite;
        private Vector3 _currentSunDirection;
        private float _currentOffsetMultiplier;

        private void OnEnable()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (!_meshFilter)
            {
                GameObject shadowObject = new GameObject("3D Shadow");
                shadowObject.transform.SetParent(transform, false);

                _meshFilter = shadowObject.AddComponent<MeshFilter>();
                _meshRenderer = shadowObject.AddComponent<MeshRenderer>();
                _meshRenderer.sharedMaterial = shadowMaterial;
                _meshRenderer.shadowCastingMode = ShadowCastingMode.TwoSided;
                _meshRenderer.receiveShadows = false;


            }

            _mesh = new Mesh
            {
                name = $"{name} Sprite Shadow Mesh"
            };
            _meshFilter.sharedMesh = _mesh;
            Refresh();
        }

        private void LateUpdate()
        {
            Vector3 sunDirection = GetSunDirection();
            if (_currentSprite != _spriteRenderer.sprite ||
                _currentSunDirection != sunDirection ||
                !Mathf.Approximately(_currentOffsetMultiplier, _shadowOffset))
            {
                Refresh(sunDirection);
            }

            _meshRenderer.enabled = _spriteRenderer.sprite != null;
        }

        private void Refresh()
        {
            Refresh(GetSunDirection());
        }

        private void Refresh(Vector3 sunDirection)
        {
            Sprite sprite = _spriteRenderer.sprite;
            _currentSprite = sprite;
            _currentSunDirection = sunDirection;
            _currentOffsetMultiplier = _shadowOffset;

            _mesh.Clear();

            if (sprite == null)
                return;

            Vector2[] spriteVertices = sprite.vertices;
            Vector2[] spriteUv = sprite.uv;
            ushort[] spriteTriangles = sprite.triangles;

            Vector3[] vertices = new Vector3[spriteVertices.Length];
            int[] triangles = new int[spriteTriangles.Length];
            Vector3 localOffset = transform.InverseTransformDirection(sunDirection) * _shadowOffset;

            for (int i = 0; i < spriteVertices.Length; i++)
                vertices[i] = new Vector3(spriteVertices[i].x, spriteVertices[i].y, 0f) + localOffset;

            for (int i = 0; i < spriteTriangles.Length; i++)
                triangles[i] = spriteTriangles[i];

            _mesh.vertices = vertices;
            _mesh.uv = spriteUv;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();

            if (_meshRenderer.sharedMaterial != shadowMaterial)
            {
                _meshRenderer.sharedMaterial = shadowMaterial;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.SetTexture(BaseMap, sprite.texture);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }

        private static Vector3 GetSunDirection()
        {
            Light sun = RenderSettings.sun;
            if (sun == null)
                return Vector3.zero;

            Vector3 sunDirection = new Vector3(-sun.transform.forward.x, 0f, -sun.transform.forward.z);
            return sunDirection.sqrMagnitude > 0.0001f ? sunDirection.normalized : Vector3.zero;
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                if (Application.isPlaying)
                    Destroy(_mesh);
                else
                    DestroyImmediate(_mesh);
            }
        }
    }
}
