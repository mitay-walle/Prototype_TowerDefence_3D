using UnityEngine;

public class VA_MeshLinear
{
	private static VA_Triangle triangle = new VA_Triangle();

	public static Vector3 FindClosestPoint(Mesh mesh, Vector3 point)
	{
		if (mesh != null)
		{
			var positions       = mesh.vertices;
			var closestDistance = float.PositiveInfinity;
			var closestPoint    = point;

			for (var i = 0; i < mesh.subMeshCount; i++)
			{
				switch (mesh.GetTopology(i))
				{
					case MeshTopology.Triangles:
					{
						var indices = mesh.GetTriangles(i);

						for (var j = 0; j < indices.Length; j += 3)
						{
							triangle.A = positions[indices[j + 0]];
							triangle.B = positions[indices[j + 1]];
							triangle.C = positions[indices[j + 2]];

							triangle.CalculatePlanes();

							var closePoint    = triangle.ClosestTo(point);
							var closeDistance = (closePoint - point).sqrMagnitude;

							if (closeDistance < closestDistance)
							{
								closestDistance = closeDistance;
								closestPoint    = closePoint;
							}
						}
					}
					break;
				}
			}

			return closestPoint;
		}

		return point;
	}
}