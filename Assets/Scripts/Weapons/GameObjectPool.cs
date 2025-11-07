using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace TD.Weapons
{
	public class PrefabSource : MonoBehaviour
	{
		public GameObject SourcePrefab;
	}

	public class GameObjectPool
	{
		public static GameObjectPool Instance { get; private set; } = new();
#if UNITY_EDITOR
		[InitializeOnEnterPlayMode]
#endif
		private static void InitializeOnEnterPlayMode()
		{
			Instance = new();
		}

		Dictionary<GameObject, ObjectPool<GameObject>> _pools = new();

		public ObjectPool<GameObject> this[GameObject prefab] => _pools.GetValueOrDefault(prefab);

		public T Get<T>(T prefabComponent) where T : Component => Get(prefabComponent.gameObject).GetComponent<T>();

		public GameObject Get(GameObject prefab)
		{
			if (!_pools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
			{
				pool = new ObjectPool<GameObject>(() =>
				{
					var copy = Object.Instantiate(prefab);
					copy.AddComponent<PrefabSource>().SourcePrefab = prefab;

					return copy;
				}, copy => copy.SetActive(true), copy => copy.SetActive(false), Object.Destroy);

				_pools.Add(prefab, pool);
			}

			return pool.Get();
		}

		public void Return(GameObject instance)
		{
			if (!instance) return;
			if (!instance.TryGetComponent<PrefabSource>(out var source)) return;

			if (!source.SourcePrefab)
			{
				Debug.LogWarning("PrefabSource: Source prefab is null");
				return;
			}

			_pools[source.SourcePrefab].Release(instance);
		}
	}
}