using System;
using System.Collections.Generic;

namespace TD.Voxels
{
	[System.Serializable]
	public class Part
	{
		public string name;
		[NonSerialized] public List<VoxelData> voxels = new List<VoxelData>();
	}
}