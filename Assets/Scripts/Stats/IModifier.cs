namespace TD.Stats
{
	public interface IModifier
	{
		public bool IsMultplicative { get; }
		float Calculate(float currentValue, float baseValue, float sumBeforeMultipliers);
		void OnAdd(IStats stats);
		void OnRemove(IStats stats);
	}
}