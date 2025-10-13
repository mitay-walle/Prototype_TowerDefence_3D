using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace TD
{
	public class VoxelGenerator : MonoBehaviour
	{
		public Material voxelMaterial;
		public float voxelSize = 0.1f;

		[OnValueChanged("Generate"), InlineButton("Next", ">")] public int seed;

		[SerializeReference]
		public GenerationProfile profile = new TurretGenerationProfile();

		void Awake() => Generate();

		private void Next()
		{
			seed += 1;
			Generate();
		}

		public void BuildMeshes(Dictionary<MaterialKey, List<VoxelData>> materialGroups, Transform parent, GenerationProfile profile)
		{
			if (materialGroups.Count == 0) return;

			Transform found = parent.Find("Combined");
			GameObject combined = null;

			if (found)
			{
				combined = found.gameObject;
				DestroyImmediate(combined.GetComponent<MeshFilter>().sharedMesh);
			}
			else
			{
				combined = new GameObject("Combined");
				combined.transform.SetParent(parent);
				combined.transform.localPosition = Vector3.zero;
				combined.AddComponent<MeshFilter>();
				combined.AddComponent<MeshRenderer>();
			}

			var combinedMF = combined.GetComponent<MeshFilter>();
			var combinedMR = combined.GetComponent<MeshRenderer>();

			Mesh finalMesh = new Mesh();
			finalMesh.name = $"{profile.GetType().Name}_{profile.seed}";
			finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			List<Material> materials = new List<Material>();
			List<CombineInstance> submeshCombines = new List<CombineInstance>();

			foreach (var kvp in materialGroups)
			{
				var optimizedVoxels = OptimizeVoxels(kvp.Value);
				Mesh submesh = CreateMeshFromVoxels(optimizedVoxels);

				submeshCombines.Add(new CombineInstance
				{
					mesh = submesh,
					transform = Matrix4x4.identity
				});

				Material mat = new Material(voxelMaterial);
				mat.color = kvp.Key.color;

				if (kvp.Key.emissionIntensity > 0)
				{
					mat.EnableKeyword("_EMISSION");
					mat.SetColor("_EmissionColor", kvp.Key.emissionColor * kvp.Key.emissionIntensity);
				}

				materials.Add(mat);
			}

			finalMesh.CombineMeshes(submeshCombines.ToArray(), false, false);
			combinedMF.mesh = finalMesh;
			combinedMR.sharedMaterials = materials.ToArray();
			finalMesh.RecalculateBounds();
			finalMesh.RecalculateNormals(UnityEngine.Rendering.MeshUpdateFlags.Default);
		#if UNITY_EDITOR
			EditorUtility.SetDirty(combinedMR);
			EditorUtility.SetDirty(combinedMF);
		#endif
		}

		private List<VoxelData> OptimizeVoxels(List<VoxelData> voxels)
		{
			if (voxels.Count == 0) return voxels;

			var result = new List<VoxelData>();
			var processed = new HashSet<VoxelData>();

			voxels.Sort((a, b) =>
			{
				int cmpY = a.position.y.CompareTo(b.position.y);
				if (cmpY != 0) return cmpY;

				int cmpZ = a.position.z.CompareTo(b.position.z);
				if (cmpZ != 0) return cmpZ;

				return a.position.x.CompareTo(b.position.x);
			});

			foreach (var voxel in voxels)
			{
				if (processed.Contains(voxel)) continue;

				if (voxel.size != Vector3.one)
				{
					result.Add(voxel);
					processed.Add(voxel);
					continue;
				}

				var merged = TryMergeVoxels(voxel, voxels, processed);
				result.Add(merged);
			}

			return result;
		}

		private VoxelData TryMergeVoxels(VoxelData start, List<VoxelData> voxels, HashSet<VoxelData> processed)
		{
			processed.Add(start);

			int maxX = 1;
			for (int x = 1; x < 50; x++)
			{
				var testPos = start.position + new Vector3(x, 0, 0);
				var found = voxels.Find(v
					=> !processed.Contains(v) && v.position == testPos && v.size == Vector3.one && v.colorIndex == start.colorIndex &&
					   v.emissionColorIndex == start.emissionColorIndex && Mathf.Approximately(v.emissionIntensity, start.emissionIntensity));

				if (found == null) break;

				processed.Add(found);
				maxX++;
			}

			int maxZ = 1;
			bool canExpandZ = true;
			for (int z = 1; z < 50 && canExpandZ; z++)
			{
				for (int x = 0; x < maxX; x++)
				{
					var testPos = start.position + new Vector3(x, 0, z);
					var found = voxels.Find(v
						=> !processed.Contains(v) && v.position == testPos && v.size == Vector3.one && v.colorIndex == start.colorIndex &&
						   v.emissionColorIndex == start.emissionColorIndex && Mathf.Approximately(v.emissionIntensity, start.emissionIntensity));

					if (found == null)
					{
						canExpandZ = false;
						break;
					}
				}

				if (canExpandZ)
				{
					for (int x = 0; x < maxX; x++)
					{
						var testPos = start.position + new Vector3(x, 0, z);
						var found = voxels.Find(v => v.position == testPos);
						processed.Add(found);
					}

					maxZ++;
				}
			}

			int maxY = 1;
			bool canExpandY = true;
			for (int y = 1; y < 50 && canExpandY; y++)
			{
				for (int z = 0; z < maxZ; z++)
				{
					for (int x = 0; x < maxX; x++)
					{
						var testPos = start.position + new Vector3(x, y, z);
						var found = voxels.Find(v
							=> !processed.Contains(v) && v.position == testPos && v.size == Vector3.one && v.colorIndex == start.colorIndex &&
							   v.emissionColorIndex == start.emissionColorIndex && Mathf.Approximately(v.emissionIntensity, start.emissionIntensity));

						if (found == null)
						{
							canExpandY = false;
							break;
						}
					}

					if (!canExpandY) break;
				}

				if (canExpandY)
				{
					for (int z = 0; z < maxZ; z++)
					{
						for (int x = 0; x < maxX; x++)
						{
							var testPos = start.position + new Vector3(x, y, z);
							var found = voxels.Find(v => v.position == testPos);
							processed.Add(found);
						}
					}

					maxY++;
				}
			}

			return new VoxelData
			{
				position = start.position + new Vector3(maxX - 1, maxY - 1, maxZ - 1) * 0.5f,
				size = new Vector3(maxX, maxY, maxZ),
				colorIndex = start.colorIndex,
				emissionColorIndex = start.emissionColorIndex,
				emissionIntensity = start.emissionIntensity
			};
		}

		private Mesh CreateMeshFromVoxels(List<VoxelData> voxels)
		{
			var vertices = new List<Vector3>();
			var triangles = new List<int>();

			foreach (var voxel in voxels)
			{
				AddCube(vertices, triangles, voxel.position * voxelSize, voxel.size * voxelSize);
			}

			Mesh mesh = new Mesh();
			mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			mesh.vertices = vertices.ToArray();
			mesh.triangles = triangles.ToArray();
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();

			return mesh;
		}

		private void AddCube(List<Vector3> vertices, List<int> triangles, Vector3 center, Vector3 size)
		{
			Vector3 halfSize = size * 0.5f;
			int startIndex = vertices.Count;

			Vector3[] corners = new Vector3[8]
			{
				center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
				center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z),
				center + new Vector3(halfSize.x, halfSize.y, -halfSize.z),
				center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z),
				center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z),
				center + new Vector3(halfSize.x, -halfSize.y, halfSize.z),
				center + new Vector3(halfSize.x, halfSize.y, halfSize.z),
				center + new Vector3(-halfSize.x, halfSize.y, halfSize.z)
			};

			int[][] faces =
			{
				new[] { 0, 2, 1, 0, 3, 2 }, // Front
				new[] { 5, 7, 4, 5, 6, 7 }, // Back
				new[] { 4, 3, 0, 4, 7, 3 }, // Left
				new[] { 1, 6, 5, 1, 2, 6 }, // Right
				new[] { 4, 1, 5, 4, 0, 1 }, // Bottom
				new[] { 3, 6, 2, 3, 7, 6 } // Top
			};

			foreach (var face in faces)
			{
				int faceStart = vertices.Count;
				for (int i = 0; i < 6; i++)
				{
					vertices.Add(corners[face[i]]);
					triangles.Add(faceStart + i);
				}
			}
		}

		[Button]
		public void Generate()
		{
			if (profile == null) return;

			profile.seed = seed;
			profile.Generate(this);
		}
	}
}