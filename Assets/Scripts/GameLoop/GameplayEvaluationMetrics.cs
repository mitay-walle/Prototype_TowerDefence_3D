using UnityEngine;

namespace TD.GameLoop
{
	public readonly struct GameplayEvaluationMetrics
	{
		public const float TargetSurvivalFraction = 0.35f;
		public const float SurvivalTolerance = 0.35f;

		public readonly bool IsVictory;
		public readonly bool IsDefeat;
		public readonly bool IsTimedOut;
		public readonly int WavesCompleted;
		public readonly int EnemiesKilled;
		public readonly float CompletionRatio;
		public readonly float BaseHealthFraction;
		public readonly float BaseHealthLossFraction;
		public readonly float CurrencySavingsRatio;
		public readonly float UpgradeScore;
		public readonly int TotalEntrances;
		public readonly int CoveredEntrances;
		public readonly float EntryCoverageRatio;
		public readonly float TowerBaseConcentration;
		public readonly float SuccessScore;
		public readonly float DifficultyScore;
		public readonly float PlayerReward;
		public readonly float BalanceReward;

		private GameplayEvaluationMetrics(
			bool isVictory,
			bool isDefeat,
			bool isTimedOut,
			int wavesCompleted,
			int enemiesKilled,
			float completionRatio,
			float baseHealthFraction,
			float baseHealthLossFraction,
			float currencySavingsRatio,
			float upgradeScore,
			int totalEntrances,
			int coveredEntrances,
			float entryCoverageRatio,
			float towerBaseConcentration,
			float successScore,
			float difficultyScore,
			float playerReward,
			float balanceReward)
		{
			IsVictory = isVictory;
			IsDefeat = isDefeat;
			IsTimedOut = isTimedOut;
			WavesCompleted = wavesCompleted;
			EnemiesKilled = enemiesKilled;
			CompletionRatio = completionRatio;
			BaseHealthFraction = baseHealthFraction;
			BaseHealthLossFraction = baseHealthLossFraction;
			CurrencySavingsRatio = currencySavingsRatio;
			UpgradeScore = upgradeScore;
			TotalEntrances = totalEntrances;
			CoveredEntrances = coveredEntrances;
			EntryCoverageRatio = entryCoverageRatio;
			TowerBaseConcentration = towerBaseConcentration;
			SuccessScore = successScore;
			DifficultyScore = difficultyScore;
			PlayerReward = playerReward;
			BalanceReward = balanceReward;
		}

		public static GameplayEvaluationMetrics Create(GameplayTelemetrySnapshot snapshot, bool victory, bool defeat)
		{
			return Create(snapshot, victory, defeat, false, snapshot.AdaptiveDifficultyScore);
		}

		public static GameplayEvaluationMetrics CreateForGeneratedWave(GameplayTelemetrySnapshot snapshot, bool victory, bool defeat)
		{
			return Create(snapshot, victory, defeat, false, snapshot.EnemyLevelDifficultyScore);
		}

		public static GameplayEvaluationMetrics CreateTimeout(GameplayTelemetrySnapshot snapshot)
		{
			return Create(snapshot, false, true, true, snapshot.AdaptiveDifficultyScore);
		}

		public static GameplayEvaluationMetrics CreateGeneratedWaveTimeout(GameplayTelemetrySnapshot snapshot)
		{
			return Create(snapshot, false, true, true, snapshot.EnemyLevelDifficultyScore);
		}

		private static GameplayEvaluationMetrics Create(
			GameplayTelemetrySnapshot snapshot,
			bool victory,
			bool defeat,
			bool timedOut,
			float difficultyScore)
		{
			var totalWaves = Mathf.Max(1, snapshot.TotalWaves);
			var completionRatio = Mathf.Clamp01((float)snapshot.WavesCompleted / totalWaves);
			var baseHealthFraction = snapshot.BaseMaxHealth > 0
				? Mathf.Clamp01((float)snapshot.BaseHealth / snapshot.BaseMaxHealth)
				: 0f;
			difficultyScore = Mathf.Clamp01(difficultyScore);
			var baseHealthLossFraction = 1f - baseHealthFraction;
			var availableCurrency = Mathf.Max(1, snapshot.StartingCurrency + snapshot.CurrencyGained);
			var currencySavingsRatio = Mathf.Clamp01(Mathf.Max(0, snapshot.Currency) / (float)availableCurrency);
			var upgradeScore = Mathf.Clamp01(snapshot.TowersUpgraded / Mathf.Max(1f, totalWaves * 2f));
			var entryCoverageRatio = snapshot.TotalEntrances > 0 ? Mathf.Clamp01(snapshot.EntryCoverageRatio) : 0f;
			var towerBaseConcentration = Mathf.Clamp01(snapshot.TowerBaseConcentration);
			var successScore = victory ? 1f : completionRatio;
			var survivalFit = 1f - Mathf.Clamp01(
				Mathf.Abs(baseHealthFraction - TargetSurvivalFraction) / SurvivalTolerance);

			var playerReward = (victory ? 1f : -1f) + completionRatio * 0.5f +
				baseHealthFraction * 0.25f - baseHealthLossFraction * 0.25f +
				currencySavingsRatio * 0.05f + upgradeScore * 0.2f + entryCoverageRatio * 0.75f +
				towerBaseConcentration * 0.2f - (1f - entryCoverageRatio) * 0.35f;
			var balanceReward = victory
				? survivalFit * 0.65f + difficultyScore * 0.35f
				: -0.75f + completionRatio * 0.25f;

			return new GameplayEvaluationMetrics(
				victory,
				defeat,
				timedOut,
				snapshot.WavesCompleted,
				snapshot.EnemiesKilled,
				completionRatio,
				baseHealthFraction,
				baseHealthLossFraction,
				currencySavingsRatio,
				upgradeScore,
				snapshot.TotalEntrances,
				snapshot.CoveredEntrances,
				entryCoverageRatio,
				towerBaseConcentration,
				successScore,
				difficultyScore,
				playerReward,
				balanceReward);
		}
	}
}
