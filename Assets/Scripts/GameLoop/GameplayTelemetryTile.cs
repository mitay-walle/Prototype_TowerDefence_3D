using System;

namespace TD.GameLoop
{
	[Serializable]
	public class GameplayTelemetryTile
	{
		public int GridX;
		public int GridY;
		public int Rotation;
		public string Name;
		public string Connections;
		public int ConnectionMask;
		public bool HasOpenRoadEnd;
	}
}
