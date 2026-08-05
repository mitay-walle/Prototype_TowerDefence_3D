using System;

namespace TD.GameLoop
{
	[Serializable]
	public class GameplayTelemetryTower
	{
		public string Name;
		public string Path;
		public int Level;
		public float Damage;
		public float FireRate;
		public float Range;
		public string TargetPriority;
		public string CurrentTarget;
		public bool HasTarget;
		public float WorldPositionX;
		public float WorldPositionY;
		public float WorldPositionZ;
		public float DistanceToBase;
	}
}
