using System.Collections.Generic;
using System.Linq;
using GameJam.Plugins.Randomize;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TD.Levels
{
	[ExecuteAlways]
	public class TileDatabase : MonoBehaviour
	{
		[SerializeField] private SerializedDictionary<RoadConnections, RoadTileComponent> tilePrefabs = new();

		public static TileDatabase _instance;

		public static TileDatabase Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindAnyObjectByType<TileDatabase>();
				}

				return _instance;
			}
		}

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			_instance = this;
			if (Application.isPlaying) DontDestroyOnLoad(gameObject);
		}

		public RoadTileComponent GetRandomTilePrefab()
		{
			return tilePrefabs.Values.Random();
		}

		public List<RoadConnections> GetAllTileKinds() => tilePrefabs.Keys.ToList();

		public List<RoadTileComponent> GetAllTilePrefabs()
		{
			return new List<RoadTileComponent>(tilePrefabs.Values);
		}

#if UNITY_EDITOR
		[Button]
		private void LoadPrefabs()
		{
			tilePrefabs.Clear();

			AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Prefabs/Tiles" }).Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<RoadTileComponent>).ToList().ForEach(c => tilePrefabs[c.GetConnections()] = c);
		}
#endif
	}
}