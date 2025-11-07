using UnityEngine;

namespace WizzardSurvivors.Plugins.TransformOperations
{
	public static class TransformExtensions
	{
		public static string PathRecursive(this Transform transform)
		{
			string result = string.Empty;
			while (transform)
			{
				result = $"{transform.gameObject.name}/{result}";
				transform = transform.parent;
			}

			// Drop the trailing '/':
			return result.Remove(result.Length - 1);
		}

		public static float SqrDistanceTo(this Transform transform, Transform target)
		{
			return SqrDistanceTo(transform, target.position);
		}

		public static float SqrDistanceTo(this Transform transform, Vector3 point)
		{
			return Vector3.SqrMagnitude(transform.position - point);
		}
	}
}