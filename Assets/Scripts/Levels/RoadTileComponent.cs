using UnityEngine;

namespace TD.Levels
{
	public class RoadTileComponent : MonoBehaviour
	{
		[SerializeField] private RoadConnections connections;
		[SerializeField] private bool showGizmo = true;

		private const float GIZMO_ARROW_SIZE = 1f;
		private const float GIZMO_ARROW_HEAD_SIZE = 0.3f;

		public void Initialize(RoadConnections roadConnections)
		{
			connections = roadConnections;
		}

		public RoadConnections GetConnections() => connections;

		private void OnDrawGizmos()
	{
		if (!showGizmo) return;

		Vector3 center = transform.position;
		float offset = 2f;

		if (connections.HasConnection(RoadSide.North))
		{
			Vector3 direction = transform.rotation * Vector3.forward;
			DrawArrow(center + direction * offset, direction, Color.green);
		}

		if (connections.HasConnection(RoadSide.South))
		{
			Vector3 direction = transform.rotation * Vector3.back;
			DrawArrow(center + direction * offset, direction, Color.red);
		}

		if (connections.HasConnection(RoadSide.East))
		{
			Vector3 direction = transform.rotation * Vector3.right;
			DrawArrow(center + direction * offset, direction, Color.cyan);
		}

		if (connections.HasConnection(RoadSide.West))
		{
			Vector3 direction = transform.rotation * Vector3.left;
			DrawArrow(center + direction * offset, direction, Color.yellow);
		}
	}


		private void DrawArrow(Vector3 position, Vector3 direction, Color color)
		{
			Gizmos.color = color;

			Vector3 arrowStart = position;
			Vector3 arrowEnd = position + direction * GIZMO_ARROW_SIZE;

			Gizmos.DrawLine(arrowStart, arrowEnd);

			Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
			Vector3 up = Vector3.Cross(right, direction).normalized;

			Vector3 headLeft = arrowEnd - direction * GIZMO_ARROW_HEAD_SIZE + right * GIZMO_ARROW_HEAD_SIZE;
			Vector3 headRight = arrowEnd - direction * GIZMO_ARROW_HEAD_SIZE - right * GIZMO_ARROW_HEAD_SIZE;
			Vector3 headUp = arrowEnd - direction * GIZMO_ARROW_HEAD_SIZE + up * GIZMO_ARROW_HEAD_SIZE;
			Vector3 headDown = arrowEnd - direction * GIZMO_ARROW_HEAD_SIZE - up * GIZMO_ARROW_HEAD_SIZE;

			Gizmos.DrawLine(arrowEnd, headLeft);
			Gizmos.DrawLine(arrowEnd, headRight);
			Gizmos.DrawLine(arrowEnd, headUp);
			Gizmos.DrawLine(arrowEnd, headDown);
		}
	}
}