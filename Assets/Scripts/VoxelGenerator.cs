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
		public float emissionScale = 1.02f;

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

			if (voxelMaterial == null)
			{
				voxelMaterial = Resources.Load<Material>("Materials/Default-Voxel-Lit");
			}
			
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
				bool hasEmission = kvp.Key.emissionIntensity > 0;
				Mesh submesh = CreateMeshFromVoxels(optimizedVoxels, hasEmission);

				submeshCombines.Add(new CombineInstance
				{
					mesh = submesh,
					transform = Matrix4x4.identity
				});

				Material mat = new Material(voxelMaterial);
				mat.color = kvp.Key.color;

				if (hasEmission)
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
			if (combined.TryGetComponent<MeshCollider>(out var coll))
			{
				coll.sharedMesh = combined.GetComponent<MeshFilter>().sharedMesh;
			}
		#if UNITY_EDITOR
			EditorUtility.SetDirty(combinedMR);
			EditorUtility.SetDirty(combinedMF);
		#endif
		}

		private List<VoxelData> OptimizeVoxels(List<VoxelData> voxels)
		{
			if (voxels.Count == 0) return voxels;

			var result = new List<VoxelData>();
			var groupedBySize = new Dictionary<Vector3, List<VoxelData>>();

			foreach (var voxel in voxels)
			{
				if (!groupedBySize.ContainsKey(voxel.size))
				{
					groupedBySize[voxel.size] = new List<VoxelData>();
				}

				groupedBySize[voxel.size].Add(voxel);
			}

			foreach (var group in groupedBySize.Values)
			{
				if (group.Count == 0) continue;

				Vector3 voxelSize = group[0].size;
				var voxelMap = new Dictionary<Vector3, VoxelData>(group.Count);

				foreach (var voxel in group)
				{
					voxelMap[voxel.position] = voxel;
				}

				var sortedPositions = new List<Vector3>(voxelMap.Keys);
				sortedPositions.Sort((a, b) =>
				{
					int cmpY = a.y.CompareTo(b.y);
					if (cmpY != 0) return cmpY;

					int cmpZ = a.z.CompareTo(b.z);
					if (cmpZ != 0) return cmpZ;

					return a.x.CompareTo(b.x);
				});

				var processed = new HashSet<Vector3>();

				foreach (var pos in sortedPositions)
				{
					if (processed.Contains(pos)) continue;

					var voxel = voxelMap[pos];
					var merged = GreedyMerge(voxel, voxelMap, processed, voxelSize);
					result.Add(merged);
				}
			}

			return result;
		}

		private VoxelData GreedyMerge(VoxelData start, Dictionary<Vector3, VoxelData> voxelMap, HashSet<Vector3> processed, Vector3 voxelSize)
		{
			Vector3 startPos = start.position;
			processed.Add(startPos);

			int maxX = FindMaxExtent(start, voxelMap, processed, voxelSize, startPos, Vector3.right);

			int maxZ = 1;
			for (int z = 1; z < 100; z++)
			{
				bool rowValid = true;
				for (int x = 0; x < maxX; x++)
				{
					Vector3 testPos = startPos + new Vector3(x * voxelSize.x, 0, z * voxelSize.z);
					if (!CanMerge(start, testPos, voxelMap, processed))
					{
						rowValid = false;
						break;
					}
				}

				if (!rowValid) break;

				for (int x = 0; x < maxX; x++)
				{
					processed.Add(startPos + new Vector3(x * voxelSize.x, 0, z * voxelSize.z));
				}

				maxZ++;
			}

			int maxY = 1;
			for (int y = 1; y < 100; y++)
			{
				bool layerValid = true;
				for (int z = 0; z < maxZ; z++)
				{
					for (int x = 0; x < maxX; x++)
					{
						Vector3 testPos = startPos + new Vector3(x * voxelSize.x, y * voxelSize.y, z * voxelSize.z);
						if (!CanMerge(start, testPos, voxelMap, processed))
						{
							layerValid = false;
							break;
						}
					}

					if (!layerValid) break;
				}

				if (!layerValid) break;

				for (int z = 0; z < maxZ; z++)
				{
					for (int x = 0; x < maxX; x++)
					{
						processed.Add(startPos + new Vector3(x * voxelSize.x, y * voxelSize.y, z * voxelSize.z));
					}
				}

				maxY++;
			}

			return new VoxelData
			{
				position = startPos + new Vector3((maxX - 1) * voxelSize.x, (maxY - 1) * voxelSize.y, (maxZ - 1) * voxelSize.z) * 0.5f,
				size = new Vector3(maxX * voxelSize.x, maxY * voxelSize.y, maxZ * voxelSize.z),
				colorIndex = start.colorIndex,
				emissionColorIndex = start.emissionColorIndex,
				emissionIntensity = start.emissionIntensity
			};
		}

		private int FindMaxExtent(VoxelData start,
		                          Dictionary<Vector3, VoxelData> voxelMap,
		                          HashSet<Vector3> processed,
		                          Vector3 voxelSize,
		                          Vector3 startPos,
		                          Vector3 direction)
		{
			int extent = 1;
			for (int i = 1; i < 100; i++)
			{
				Vector3 offset = new Vector3(direction.x * i * voxelSize.x, direction.y * i * voxelSize.y, direction.z * i * voxelSize.z);
				Vector3 testPos = startPos + offset;
				if (!CanMerge(start, testPos, voxelMap, processed))
					break;

				processed.Add(testPos);
				extent++;
			}

			return extent;
		}

		private bool CanMerge(VoxelData reference, Vector3 position, Dictionary<Vector3, VoxelData> voxelMap, HashSet<Vector3> processed)
		{
			if (!voxelMap.TryGetValue(position, out var voxel)) return false;
			if (processed.Contains(position)) return false;
			if (voxel.colorIndex != reference.colorIndex) return false;
			if (voxel.emissionColorIndex != reference.emissionColorIndex) return false;
			if (!Mathf.Approximately(voxel.emissionIntensity, reference.emissionIntensity)) return false;

			return true;
		}

		private Mesh CreateMeshFromVoxels(List<VoxelData> voxels, bool hasEmission)
		{
			var vertices = new List<Vector3>();
			var triangles = new List<int>();

			float scale = hasEmission ? emissionScale : 1f;

			foreach (var voxel in voxels)
			{
				AddCube(vertices, triangles, voxel.position * voxelSize, voxel.size * voxelSize * scale);
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
				new[] { 0, 2, 1, 0, 3, 2 },
				new[] { 5, 7, 4, 5, 6, 7 },
				new[] { 4, 3, 0, 4, 7, 3 },
				new[] { 1, 6, 5, 1, 2, 6 },
				new[] { 4, 1, 5, 4, 0, 1 },
				new[] { 3, 6, 2, 3, 7, 6 }
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