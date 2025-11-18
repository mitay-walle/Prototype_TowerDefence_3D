using System.Collections.Generic;
using System.Linq;
using GameJam.Plugins.Randomize;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TD.Levels
{
	public class TileDatabase : MonoBehaviour
	{
		[SerializeField] private SerializedDictionary<RoadConnections, RoadTileComponent> tilePrefabs = new();

		public static TileDatabase Instance { get; private set; }

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
			DontDestroyOnLoad(gameObject);
		}

		public RoadTileComponent GetRandomTilePrefab()
		{
			return tilePrefabs.Values.Random();
		}

public System.Collections.Generic.List<RoadTileComponent> GetAllTilePrefabs()
	{
		return new System.Collections.Generic.List<RoadTileComponent>(tilePrefabs.Values);
	}


#if UNITY_EDITOR
		[Button]
		private void LoadPrefabs()
		{
			tilePrefabs.Clear();

			AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Prefabs/Tiles" })
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<RoadTileComponent>)
				.ToList()
				.ForEach(c => tilePrefabs[c.GetConnections()] = c);
		}
#endif
	}
}