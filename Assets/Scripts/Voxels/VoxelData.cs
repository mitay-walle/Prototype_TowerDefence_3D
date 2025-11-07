using UnityEngine;

namespace TD.Voxels
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
}