using System;

namespace TD.GameLoop
{
	[Serializable]
	public class GameplayTelemetryEvent
	{
		public int Sequence;
		public int Frame;
		public float Time;
		public string Category;
		public string Name;
		public string Owner;
		public string Phase;
		public string Control;
		public string Value;
		public string Details;
		public string BeforeState;
		public string AfterState;
		public int BeforeWave;
		public int AfterWave;
		public int BeforeCurrency;
		public int AfterCurrency;
        public int BeforeBaseHealth;
        public int AfterBaseHealth;
        public int BeforeEnemiesAlive;
        public int AfterEnemiesAlive;
        public int BeforeTowerCount;
        public int AfterTowerCount;
        public bool BeforePaused;
        public bool AfterPaused;
        public bool BeforeRewardOfferPending;
        public bool AfterRewardOfferPending;
        public bool BeforeRewardOfferResolved;
        public bool AfterRewardOfferResolved;
        public string BeforeRewardOfferId;
        public string AfterRewardOfferId;
        public string BeforeSelectedRewardId;
        public string AfterSelectedRewardId;
        public int BeforeRewardOfferCreatedForWave;
        public int AfterRewardOfferCreatedForWave;
        public string BeforeSelectedReward;
        public string AfterSelectedReward;
        public string BeforeChallengeModifier;
        public string AfterChallengeModifier;
        public bool BeforeTowerPlacing;
        public bool AfterTowerPlacing;
        public bool BeforeTilePlacing;
        public bool AfterTilePlacing;
        public int BeforeSelectedTileIndex;
        public int AfterSelectedTileIndex;
        public int BeforeTileOptionCount;
        public int AfterTileOptionCount;
        public int BeforeActiveEnemyCount;
        public int AfterActiveEnemyCount;
    }
}
