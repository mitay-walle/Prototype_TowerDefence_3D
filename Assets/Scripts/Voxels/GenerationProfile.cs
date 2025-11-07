using System.Collections.Generic;
using UnityEngine;
using TD.Voxels;

namespace TD
{
	[System.Serializable]
	public abstract class GenerationProfile
	{
		public int seed;
		[SerializeField] int _lastSeed;
		public int Seed => _lastSeed = seed == 0 ? Random.Range(1, int.MaxValue) : seed;

		protected void ClearMeshes(Transform parent)
		{
			Transform found = parent.Find("Combined");
			if (found)
			{
				Object.DestroyImmediate(found.GetComponent<MeshFilter>().sharedMesh);
			}
		}

		protected void GeneratePart(VoxelGenerator generator, Part part, Transform parent, Vector3 offset)
		{
			Dictionary<MaterialKey, List<VoxelData>> materialGroups = new Dictionary<MaterialKey, List<VoxelData>>();

			foreach (var voxelData in part.voxels)
			{
				if (voxelData.size == Vector3.zero) continue;

				MaterialKey key = new MaterialKey
				{
					color = GetColor(voxelData.colorIndex),
					emissionColor = voxelData.emissionColorIndex >= 0 ? GetEmissionColor(voxelData.emissionColorIndex) : Color.black,
					emissionIntensity = voxelData.emissionIntensity
				};

				if (!materialGroups.ContainsKey(key))
				{
					materialGroups[key] = new List<VoxelData>();
				}

				var offsetData = new VoxelData
				{
					position = voxelData.position + offset,
					size = voxelData.size,
					colorIndex = voxelData.colorIndex,
					emissionColorIndex = voxelData.emissionColorIndex,
					emissionIntensity = voxelData.emissionIntensity
				};
				materialGroups[key].Add(offsetData);
			}

			Transform found = parent.Find(part.name);
			if (!found)
			{
				GameObject partObj = new GameObject(part.name);
				partObj.transform.SetParent(parent);
				partObj.transform.localPosition = Vector3.zero;
				found = partObj.transform;
			}

			generator.BuildMeshes(materialGroups, found, this);
		}

		protected abstract Color GetEmissionColor(int index);
		protected abstract void Randomize();
		protected abstract Color GetColor(int index);

		public virtual void Generate(VoxelGenerator generator)
		{
			Randomize();
		}
	}
}