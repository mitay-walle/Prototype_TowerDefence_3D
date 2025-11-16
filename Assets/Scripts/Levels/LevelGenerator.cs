using Sirenix.OdinInspector;
using TD.Plugins.Randomization;
using TD.Voxels;
using UnityEngine;

namespace TD
{
	public class LevelGenerator : MonoBehaviour
	{
		[SerializeField] private bool randomSeed = true;
		[SerializeField] private int levelSeed = 0;
		[SerializeField, Required] VoxelGenerator generatedLevel;

		[Button]
		public void GenerateLevel()
		{
			generatedLevel.transform.position = Vector3.zero;
			generatedLevel.transform.rotation = Quaternion.identity;
			generatedLevel.name = "LevelRoad (Generated)";

			// Find VoxelGenerator and generate
			VoxelGenerator voxelGen = generatedLevel.GetComponentInChildren<VoxelGenerator>();
			if (voxelGen != null)
			{
				if (randomSeed)
				{
					levelSeed = System.DateTime.Now.Millisecond;
				}

				// Access profile through reflection or make it public
				voxelGen.seed = levelSeed;

				voxelGen.Generate();
			}

			foreach (Randomizer randomizer in GetComponentsInChildren<Randomizer>())
			{
				randomizer.Randomize();
			}
		}
	}
}