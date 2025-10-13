using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD
{
	[System.Serializable]
	public class Part
	{
		public string name;
		[NonSerialized] public List<VoxelData> voxels = new List<VoxelData>();
	}
}