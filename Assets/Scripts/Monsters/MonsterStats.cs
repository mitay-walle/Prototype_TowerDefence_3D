using Sirenix.OdinInspector;
using TD.Stats;

namespace TD.Monsters
{
	public class MonsterStats : ComponentStats<MonsterStatsSO>
	{
		public Stat Damage = new();
		public Stat MoveSpeed = new();
		public Stat Health = new();
		public Stat InstantReward = new();
		public Stat IncomeReward = new();
		public Stat EarlyKillModifier = new();

		public Stat this[MonsterStat type] => MonsterStatUtility.Indexer(this, type);

		protected override void InitializeStats() => MonsterStatUtility.Initialize(this);
		protected override void OnRecalculateStats() => MonsterStatUtility.Calculate(this);
	}
}