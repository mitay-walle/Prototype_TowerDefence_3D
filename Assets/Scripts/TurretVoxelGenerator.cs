using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class TurretVoxelGenerator : MonoBehaviour
{
	[System.Serializable]
	public class VoxelData
	{
		public Vector3 position;
		public Vector3 size = Vector3.one;
		public int colorIndex;
		public int emissionColorIndex = -1;
		public float emissionIntensity;
	}

	[System.Serializable]
	public class TurretPart
	{
		public string name;
		public List<VoxelData> voxels = new List<VoxelData>();
	}

	public Material voxelMaterial;
	public float voxelSize = 0.1f;

	[OnValueChanged("Generate"), InlineButton("Next", ">")] public int seed;

	[SerializeReference]
	public TurretGenerationProfile profile = new TurretGenerationProfile();

	public TurretPart basePlate = new TurretPart { name = "Base" };
	public TurretPart rotatingTurret = new TurretPart { name = "Turret" };

	[SerializeField, ReadOnly] private GameObject baseObject;
	[SerializeField, ReadOnly] private GameObject turretObject;

	private class MaterialKey
	{
		public Color color;
		public Color emissionColor;
		public float emissionIntensity;

		public override bool Equals(object obj)
		{
			if (obj is MaterialKey other)
			{
				return color.Equals(other.color) && emissionColor.Equals(other.emissionColor) &&
				       Mathf.Approximately(emissionIntensity, other.emissionIntensity);
			}

			return false;
		}

		public override int GetHashCode()
		{
			return color.GetHashCode() ^ emissionColor.GetHashCode() ^ emissionIntensity.GetHashCode();
		}
	}

	private void Next()
	{
		seed += 1;
		Generate();
	}

	public void GenerateTurret()
	{
		float baseTopY = profile.GetBaseTopY();

		if (baseObject == null)
		{
			baseObject = new GameObject("Base");
			baseObject.transform.SetParent(transform);
			baseObject.transform.localPosition = Vector3.zero;
		}

		if (turretObject == null)
		{
			turretObject = new GameObject("Turret");
			turretObject.transform.SetParent(transform);
		}

		turretObject.transform.localPosition = new Vector3(0, baseTopY * voxelSize, 0);

		ClearMeshes(baseObject.transform);
		ClearMeshes(turretObject.transform);

		GeneratePart(basePlate, baseObject.transform, Vector3.zero);
		GeneratePart(rotatingTurret, turretObject.transform, Vector3.zero);
	}

	private void ClearMeshes(Transform parent)
	{
		Transform found = parent.Find("Combined");
		if (found)
		{
			DestroyImmediate(found.gameObject);
		}
	}

	private void GeneratePart(TurretPart part, Transform parent, Vector3 offset)
	{
		Dictionary<MaterialKey, List<GameObject>> materialGroups = new Dictionary<MaterialKey, List<GameObject>>();

		foreach (var voxelData in part.voxels)
		{
			if (voxelData.size == Vector3.zero) continue;

			MaterialKey key = new MaterialKey
			{
				color = profile.GetColor(voxelData.colorIndex),
				emissionColor = voxelData.emissionColorIndex >= 0 ? profile.GetEmissionColor(voxelData.emissionColorIndex) : Color.black,
				emissionIntensity = voxelData.emissionIntensity
			};

			if (!materialGroups.ContainsKey(key))
			{
				materialGroups[key] = new List<GameObject>();
			}

			GameObject voxel = CreateVoxel(voxelData, parent, offset);
			materialGroups[key].Add(voxel);
		}

		CombineMeshes(materialGroups, parent);
	}

	private GameObject CreateVoxel(VoxelData data, Transform parent, Vector3 offset)
	{
		GameObject voxel = GameObject.CreatePrimitive(PrimitiveType.Cube);
		voxel.transform.SetParent(parent);
		voxel.transform.localPosition = (data.position + offset) * voxelSize;
		voxel.transform.localScale = data.size * voxelSize;

		MeshRenderer renderer = voxel.GetComponent<MeshRenderer>();
		Material mat = new Material(voxelMaterial);
		mat.color = profile.GetColor(data.colorIndex);

		if (data.emissionIntensity > 0 && data.emissionColorIndex >= 0)
		{
			mat.EnableKeyword("_EMISSION");
			mat.SetColor("_EmissionColor", profile.GetEmissionColor(data.emissionColorIndex) * data.emissionIntensity);
		}

		renderer.material = mat;
		return voxel;
	}

	private void CombineMeshes(Dictionary<MaterialKey, List<GameObject>> materialGroups, Transform parent)
	{
		if (materialGroups.Count == 0) return;

		GameObject combined = new GameObject("Combined");
		combined.transform.SetParent(parent);
		combined.transform.localPosition = Vector3.zero;

		MeshFilter combinedMF = combined.AddComponent<MeshFilter>();
		MeshRenderer combinedMR = combined.AddComponent<MeshRenderer>();

		Mesh finalMesh = new Mesh();
		List<Material> materials = new List<Material>();
		List<CombineInstance> submeshCombines = new List<CombineInstance>();

		foreach (var kvp in materialGroups)
		{
			List<CombineInstance> combines = new List<CombineInstance>();

			foreach (var voxel in kvp.Value)
			{
				MeshFilter mf = voxel.GetComponent<MeshFilter>();
				combines.Add(new CombineInstance
				{
					mesh = mf.sharedMesh,
					transform = parent.worldToLocalMatrix * voxel.transform.localToWorldMatrix
				});
			}

			Mesh submesh = new Mesh();
			submesh.CombineMeshes(combines.ToArray(), true, true);

			submeshCombines.Add(new CombineInstance
			{
				mesh = submesh,
				transform = Matrix4x4.identity
			});

			Material mat = kvp.Value[0].GetComponent<MeshRenderer>().sharedMaterial;
			materials.Add(mat);
		}

		finalMesh.CombineMeshes(submeshCombines.ToArray(), false, false);
		combinedMF.mesh = finalMesh;
		combinedMR.materials = materials.ToArray();

		foreach (var kvp in materialGroups)
		{
			foreach (var voxel in kvp.Value)
			{
				DestroyImmediate(voxel);
			}
		}
	}

	[Button]
	public void Generate()
	{
		if (profile == null) return;

		profile.seed = seed;
		profile.Randomize();

		basePlate.voxels.Clear();
		rotatingTurret.voxels.Clear();

		profile.Generate(basePlate, rotatingTurret);
		GenerateTurret();
	}
}