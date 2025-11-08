using System;
using Sirenix.OdinInspector;

namespace TD.Stats
{
	[Serializable]
	public abstract class StatModifier
	{
		[PropertyRange(0, 100)]
		public int priority = 50;

		public abstract bool IsMultplicative { get; }
		public abstract float Calculate(float currentValue, float baseValue, float sumBeforeMultipliers);
		public virtual void OnAdd(IStats stats) { }
		public virtual void OnRemove(IStats stats) { }
	}
}