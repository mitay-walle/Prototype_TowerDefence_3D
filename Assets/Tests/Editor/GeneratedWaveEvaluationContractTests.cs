using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TD.GameLoop;
using TD.Towers;
using UnityEngine;

namespace TD.Tests
{
	public class GeneratedWaveEvaluationContractTests
	{
		[Test]
		public void RecordEvaluationUpdatesOnlyTrackedGeneratedWave()
		{
			var managerObject = new GameObject("GeneratedWaveEvaluationContractTests.Manager");
			var waveManager = managerObject.AddComponent<WaveManager>();
			var enemyPrefab = new GameObject("GeneratedWaveEvaluationContractTests.Enemy");
			var trackedWave = CreateGeneratedWave("Tracked", enemyPrefab);
			var unrelatedWave = CreateGeneratedWave("Unrelated", enemyPrefab);

			typeof(WaveManager)
				.GetField("pendingGeneratedWave", BindingFlags.Instance | BindingFlags.NonPublic)
				.SetValue(waveManager, trackedWave);

			var evaluation = GameplayEvaluationMetrics.Create(
				new GameplayTelemetrySnapshot
				{
					TotalWaves = 1,
					WavesCompleted = 1,
					BaseMaxHealth = 100,
					BaseHealth = 75
				},
				true,
				false);

			waveManager.RecordGeneratedWaveEvaluation(evaluation, true, false);

			Assert.That(trackedWave.EvaluationCount, Is.EqualTo(1));
			Assert.That(unrelatedWave.EvaluationCount, Is.EqualTo(0));

			Object.DestroyImmediate(trackedWave);
			Object.DestroyImmediate(unrelatedWave);
			Object.DestroyImmediate(enemyPrefab);
			Object.DestroyImmediate(managerObject);
		}

		[Test]
		public void PredictedDamageFractionUsesBaseMaximumHealth()
		{
			var managerObject = new GameObject("GeneratedWaveEvaluationContractTests.DamageManager");
			var waveManager = managerObject.AddComponent<WaveManager>();
			var baseObject = new GameObject("GeneratedWaveEvaluationContractTests.Base");
			baseObject.AddComponent<BoxCollider>();
			var playerBase = baseObject.AddComponent<PlayerBase>();
			playerBase.Initialize(100);
			playerBase.TakeDamage(90);
			waveManager.Initialize(null, new Transform[0], playerBase);
			var enemyPrefab = new GameObject("GeneratedWaveEvaluationContractTests.DamageEnemy");
			var trackedWave = WaveConfig.CreateGenerated(
				"DamageTracked",
				1,
				new List<EnemySpawnData>
				{
					new EnemySpawnData { enemyPrefab = enemyPrefab }
				},
				1f,
				25,
				1f,
				1f,
				1,
				95f,
				0.05f,
				0.5f);

			typeof(WaveManager)
				.GetField("pendingGeneratedWave", BindingFlags.Instance | BindingFlags.NonPublic)
				.SetValue(waveManager, trackedWave);

			Assert.That(waveManager.GeneratedWavePredictedDamageFraction, Is.EqualTo(0.95f).Within(0.0001f));

			Object.DestroyImmediate(trackedWave);
			Object.DestroyImmediate(enemyPrefab);
			Object.DestroyImmediate(baseObject);
			Object.DestroyImmediate(managerObject);
		}

		[Test]
		public void GeneratedWaveStoresPredictedCombatSeconds()
		{
			var enemyPrefab = new GameObject("GeneratedWaveEvaluationContractTests.CombatEnemy");
			var wave = WaveConfig.CreateGenerated(
				"CombatTracked",
				1,
				new List<EnemySpawnData>
				{
					new EnemySpawnData { enemyPrefab = enemyPrefab }
				},
				1f,
				25,
				1f,
				1f,
				1,
				1f,
				0.5f,
				0.5f,
				12.5f);

			Assert.That(wave.PredictedCombatSeconds, Is.EqualTo(12.5f).Within(0.0001f));

			Object.DestroyImmediate(wave);
			Object.DestroyImmediate(enemyPrefab);
		}

		[Test]
		public void GeneratedWaveStoresAppliedAdaptiveBalanceFactors()
		{
			var enemyPrefab = new GameObject("GeneratedWaveEvaluationContractTests.BalanceEnemy");
			var wave = WaveConfig.CreateGenerated(
				"BalanceTracked",
				1,
				new List<EnemySpawnData>
				{
					new EnemySpawnData { enemyPrefab = enemyPrefab }
				},
				1f,
				25,
				1f,
				1f,
				1,
				1f,
				0.5f,
				0.5f,
				12.5f,
				0.8f,
				1.2f,
				1.1f,
				0.9f);

			Assert.That(wave.AppliedAdaptiveEnemyHealthFactor, Is.EqualTo(0.8f).Within(0.0001f));
			Assert.That(wave.AppliedAdaptiveEnemyCountFactor, Is.EqualTo(1.2f).Within(0.0001f));
			Assert.That(wave.AppliedAdaptiveEnemySpeedFactor, Is.EqualTo(1.1f).Within(0.0001f));
			Assert.That(wave.AppliedAdaptiveRewardFactor, Is.EqualTo(0.9f).Within(0.0001f));

			Object.DestroyImmediate(wave);
			Object.DestroyImmediate(enemyPrefab);
		}

		private static WaveConfig CreateGeneratedWave(string name, GameObject enemyPrefab)
		{
			return WaveConfig.CreateGenerated(
				name,
				1,
				new List<EnemySpawnData>
				{
					new EnemySpawnData { enemyPrefab = enemyPrefab }
				},
				1f,
				25,
				1f,
				1f,
				1,
				1f,
				0.5f,
				0.5f);
		}
	}
}
