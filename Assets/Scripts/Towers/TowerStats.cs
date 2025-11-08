using Sirenix.OdinInspector;
using TD.Towers;

namespace TD.Stats
{
	public class TowerStats : ComponentStats<TowerStatsSO>
	{
		public Stat Damage = new Stat();
		public Stat FireRate = new Stat();
		public Stat Range = new Stat();
		public Stat CritChance = new Stat();
		public Stat ProjectileSpeed = new Stat();
		public Stat RotateSpeed = new Stat();
		public Stat UpgradeCost = new Stat();

		public Stat this[TowerStat type] => TowerStatUtility.Indexer(this, type);

		protected override void InitializeStats() => TowerStatUtility.Initialize(this);
		protected override void OnRecalculateStats() => TowerStatUtility.Calculate(this);
	}
}