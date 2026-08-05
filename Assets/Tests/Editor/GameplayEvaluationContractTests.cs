using NUnit.Framework;
using TD.GameLoop;
using UnityEngine;

namespace TD.Tests
{
	public class GameplayEvaluationContractTests
	{
		[Test]
		public void VictoryAtTargetHealthAndHighPressureRewardsBalanceAgent()
		{
			var snapshot = new GameplayTelemetrySnapshot
			{
				TotalWaves = 3,
				WavesCompleted = 3,
				BaseHealth = 35,
				BaseMaxHealth = 100,
				AdaptiveDifficultyScore = 1f,
				EnemiesKilled = 21
			};

			var evaluation = GameplayEvaluationMetrics.Create(snapshot, true, false);

			Assert.That(evaluation.CompletionRatio, Is.EqualTo(1f));
			Assert.That(evaluation.BaseHealthFraction, Is.EqualTo(0.35f));
			Assert.That(evaluation.BalanceReward, Is.EqualTo(1f));
			Assert.That(evaluation.PlayerReward, Is.GreaterThan(1f));
		}

		[Test]
		public void DefeatProducesNegativeBalanceReward()
		{
			var snapshot = new GameplayTelemetrySnapshot
			{
				TotalWaves = 3,
				WavesCompleted = 1,
				BaseHealth = 0,
				BaseMaxHealth = 100,
				AdaptiveDifficultyScore = 0.75f
			};

			var evaluation = GameplayEvaluationMetrics.Create(snapshot, false, true);

			Assert.That(evaluation.CompletionRatio, Is.EqualTo(1f / 3f).Within(0.0001f));
			Assert.That(evaluation.BalanceReward, Is.LessThan(0f));
			Assert.That(evaluation.PlayerReward, Is.LessThan(-0.5f));
		}

		[Test]
		public void PlayerEvaluationIncludesSavingsAndTowerUpgrades()
		{
			var snapshot = new GameplayTelemetrySnapshot
			{
				TotalWaves = 3,
				WavesCompleted = 3,
				BaseHealth = 80,
				BaseMaxHealth = 100,
				StartingCurrency = 50,
				CurrencyGained = 50,
				Currency = 75,
				TowersUpgraded = 2
			};

			var evaluation = GameplayEvaluationMetrics.Create(snapshot, true, false);

			Assert.That(evaluation.SuccessScore, Is.EqualTo(1f));
			Assert.That(evaluation.BaseHealthLossFraction, Is.EqualTo(0.2f).Within(0.0001f));
			Assert.That(evaluation.CurrencySavingsRatio, Is.EqualTo(0.75f).Within(0.0001f));
			Assert.That(evaluation.UpgradeScore, Is.EqualTo(1f / 3f).Within(0.0001f));
			Assert.That(evaluation.PlayerReward, Is.GreaterThan(1f));
		}

		[Test]
		public void GeneratedWaveEvaluationUsesEnemyLevelPressure()
		{
			var snapshot = new GameplayTelemetrySnapshot
			{
				TotalWaves = 1,
				WavesCompleted = 1,
				BaseHealth = 35,
				BaseMaxHealth = 100,
				AdaptiveDifficultyScore = 0f,
				EnemyLevelDifficultyScore = 1f
			};

			var adaptiveEvaluation = GameplayEvaluationMetrics.Create(snapshot, true, false);
			var generatedWaveEvaluation = GameplayEvaluationMetrics.CreateForGeneratedWave(snapshot, true, false);

			Assert.That(generatedWaveEvaluation.DifficultyScore, Is.EqualTo(1f));
			Assert.That(generatedWaveEvaluation.BalanceReward, Is.GreaterThan(adaptiveEvaluation.BalanceReward));
		}

		[Test]
		public void TimeoutIsRecordedAsDefeatWithNegativeReward()
		{
			var snapshot = new GameplayTelemetrySnapshot
			{
				TotalWaves = 3,
				WavesCompleted = 1,
				BaseHealth = 60,
				BaseMaxHealth = 100,
				AdaptiveDifficultyScore = 0.5f
			};

			var evaluation = GameplayEvaluationMetrics.CreateTimeout(snapshot);

			Assert.That(evaluation.IsVictory, Is.False);
			Assert.That(evaluation.IsDefeat, Is.True);
			Assert.That(evaluation.IsTimedOut, Is.True);
			Assert.That(evaluation.CompletionRatio, Is.EqualTo(1f / 3f).Within(0.0001f));
			Assert.That(evaluation.BalanceReward, Is.LessThan(0f));
		}

		[Test]
		public void TowerTelemetryPreservesPlacementPositionThroughJson()
		{
			var telemetryTower = new GameplayTelemetryTower
			{
				WorldPositionX = 4.5f,
				WorldPositionY = 0.62f,
				WorldPositionZ = -3.25f,
				DistanceToBase = 5.5f
			};

			var restored = JsonUtility.FromJson<GameplayTelemetryTower>(JsonUtility.ToJson(telemetryTower));

			Assert.That(restored.WorldPositionX, Is.EqualTo(4.5f));
			Assert.That(restored.WorldPositionY, Is.EqualTo(0.62f));
			Assert.That(restored.WorldPositionZ, Is.EqualTo(-3.25f));
			Assert.That(restored.DistanceToBase, Is.EqualTo(5.5f));
		}

		[Test]
		public void WaveCombatTelemetryPreservesCausalCounters()
		{
			var details = GameplayTelemetry.FormatWaveCombatDetails(4, 6, 5, 2, 1);

			Assert.That(details, Does.Contain("targetAcquisitions=4"));
			Assert.That(details, Does.Contain("towerFires=6"));
			Assert.That(details, Does.Contain("damageApplications=5"));
			Assert.That(details, Does.Contain("kills=2"));
			Assert.That(details, Does.Contain("leaks=1"));
			Assert.That(details, Does.Contain("firePerTarget=1.50"));
			Assert.That(details, Does.Contain("damagePerFire=0.83"));
		}

		[Test]
		public void EnemySpawnTelemetryPreservesRoleAndPacing()
		{
			var details = GameplayTelemetry.FormatEnemySpawnDetails(2, 3, 8, "Runner", 97.5f, 4.2f, 1f);

			Assert.That(details, Does.Contain("group=2"));
			Assert.That(details, Does.Contain("enemy=3/8"));
			Assert.That(details, Does.Contain("archetype=Runner"));
			Assert.That(details, Does.Contain("health=97.50"));
			Assert.That(details, Does.Contain("speed=4.20"));
			Assert.That(details, Does.Contain("spawnDelay=1.00"));
		}

		[Test]
		public void WaveCombatTelemetryParserPreservesCausalCounters()
		{
			var parsed = GameplayTelemetry.TryParseWaveCombatDetails(
				"targetAcquisitions=12;towerFires=10;damageApplications=25;kills=2;leaks=1;firePerTarget=0.83;damagePerFire=2.50",
				out var targetAcquisitions,
				out var towerFires,
				out var damageApplications,
				out var kills,
				out var leaks);

			Assert.That(parsed, Is.True);
			Assert.That(targetAcquisitions, Is.EqualTo(12));
			Assert.That(towerFires, Is.EqualTo(10));
			Assert.That(damageApplications, Is.EqualTo(25));
			Assert.That(kills, Is.EqualTo(2));
			Assert.That(leaks, Is.EqualTo(1));
		}
	}
}
