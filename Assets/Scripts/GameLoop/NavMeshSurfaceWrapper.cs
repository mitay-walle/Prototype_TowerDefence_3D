using Unity.AI.Navigation;
using UnityEngine;

namespace TD.GameLoop
{
	public class NavMeshSurfaceWrapper : MonoBehaviour
	{
		private NavMeshSurface navMeshSurface;

		private void Awake()
		{
			navMeshSurface = GetComponent<NavMeshSurface>();
		}

		public bool BuildNavMesh()
		{
			if (navMeshSurface == null)
				return false;

			navMeshSurface.BuildNavMesh();
			return navMeshSurface.navMeshData != null;
		}
	}
}