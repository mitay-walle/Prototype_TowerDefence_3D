using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TD.Levels;

namespace TD.Automation
{
	public static class SetupMapVisualizer
	{
		[MenuItem("TD/Automation/Setup Map Visualizer")]
		public static void AddMapVisualizerToLevelGenerator()
		{
			var scene = EditorSceneManager.GetActiveScene();
			if (scene.name != "Gameplay")
			{
				Debug.LogError("[SetupMapVisualizer] Please load the Gameplay scene first!");
				return;
			}

			var levelGenerators = Object.FindObjectsByType<LevelGenerator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			if (levelGenerators.Length == 0)
			{
				Debug.LogError("[SetupMapVisualizer] LevelGenerator not found in scene!");
				return;
			}

			var levelGen = levelGenerators[0];
			var mapVis = levelGen.GetComponent<MapVisualizer>();
			if (mapVis != null)
			{
				Debug.LogWarning("[SetupMapVisualizer] MapVisualizer already exists on LevelGenerator!");
				return;
			}

			mapVis = levelGen.gameObject.AddComponent<MapVisualizer>();
			var tileMapManager = levelGen.GetComponent<TileMapManager>();
			if (tileMapManager != null)
			{
				SerializedObject serializedObj = new SerializedObject(mapVis);
				serializedObj.FindProperty("tileMapManager").objectReferenceValue = tileMapManager;
				serializedObj.ApplyModifiedProperties();
			}

			EditorSceneManager.MarkSceneDirty(scene);
			Debug.Log("[SetupMapVisualizer] MapVisualizer component added to LevelGenerator!");
		}
	}
}
