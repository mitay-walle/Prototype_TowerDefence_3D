using UnityEngine;

namespace TD.Levels
{
	public class RoadTileComponent : MonoBehaviour
	{
		[SerializeField] private RoadConnections connections;

		public void Initialize(RoadConnections roadConnections)
		{
			connections = roadConnections;
		}

		public RoadConnections GetConnections() => connections;
	}
}