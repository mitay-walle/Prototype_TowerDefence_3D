using UnityEngine;

namespace TD.GameLoop
{
	public class NavMeshSurfaceWrapper : MonoBehaviour
	{
		public void BuildNavMesh()
		{
			var surfaces = FindObjectsOfType<Component>();
			foreach (var surface in surfaces)
			{
				if (surface.GetType().Name == "NavMeshSurface")
				{
					var method = surface.GetType().GetMethod("BuildNavMesh");
					method?.Invoke(surface, null);
					break;
				}
			}
		}
	}
}