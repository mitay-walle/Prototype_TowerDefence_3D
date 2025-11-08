using Sirenix.OdinInspector;

namespace TD.Stats
{
	public class TowerStats : ComponentStats<TowerStatsSO>
	{
		public Stat Damage = new Stat();
		public Stat FireDelay = new Stat();
		public Stat Range = new Stat();
		public Stat CritChance = new Stat();
		public Stat ProjectileSpeed = new Stat();

		public Stat this[TowerStat type] => StatUtility.Indexer(this, type);

		protected override void InitializeStats() => StatUtility.Initialize(this);
		[Button] protected override void RecalculateStats() => StatUtility.Calculate(this);
	}
}