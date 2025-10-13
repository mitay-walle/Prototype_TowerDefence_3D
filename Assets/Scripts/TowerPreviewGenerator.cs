using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[ExecuteAlways]
public class TowerPreviewGenerator : MonoBehaviour
{
	public List<GameObject> towers;
	public Camera previewCamera;
	public int resolution = 256;
	public int layer = 3;
	public float YOffset = .75f;

	[ShowInInspector] public Dictionary<GameObject, Texture2D> previews = new();

	void Awake()
	{
		GeneratePreviews();
	}

	[Button]
	public void GeneratePreviews()
	{
		if (!previewCamera) return;

		var rt = new RenderTexture(resolution, resolution, 16);
		var oldRT = RenderTexture.active;
		previewCamera.enabled = false;

		foreach (var tower in towers)
		{
			if (!tower) continue;

			var instance = Instantiate(tower, Vector3.zero, Quaternion.identity);
			instance.GetComponent<TurretVoxelGenerator>().Generate();
			instance.hideFlags = HideFlags.HideAndDontSave;

			// центрируем башню
			var bounds = new Bounds();
			foreach (var r in instance.GetComponentsInChildren<Renderer>())
			{
				r.gameObject.layer = layer;
				bounds.Encapsulate(r.bounds);
			}

			previewCamera.transform.position = bounds.center + -previewCamera.transform.forward * bounds.size.magnitude * 2 + Vector3.up * YOffset;

			previewCamera.targetTexture = rt;
			previewCamera.Render();

			RenderTexture.active = rt;
			var tex = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
			tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
			tex.Apply();

			previews[tower] = tex;

			DestroyImmediate(instance);
		}

		RenderTexture.active = oldRT;
		previewCamera.targetTexture = null;
		DestroyImmediate(rt);
	}
}