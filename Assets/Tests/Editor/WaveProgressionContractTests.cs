using NUnit.Framework;
using TD.GameLoop;
using TD.Monsters;
using UnityEngine;

namespace TD.Tests
{
	public class WaveProgressionContractTests
	{
		[Test]
		public void MidRunWaveDefersBerserkerBossUntilFinalWave()
		{
			var midRun = Resources.Load<WaveConfig>("WaveConfigs/Wave_02");
			var final = Resources.Load<WaveConfig>("WaveConfigs/Wave_03");

			Assert.That(midRun, Is.Not.Null);
			Assert.That(final, Is.Not.Null);
			Assert.That(ContainsArchetype(midRun, MonsterArchetype.Berserker), Is.False);
			Assert.That(ContainsArchetype(final, MonsterArchetype.Berserker), Is.True);
		}

		[Test]
		public void OpeningWaveUsesAuthoredPlayableHealthPressure()
		{
			var openingWave = Resources.Load<WaveConfig>("WaveConfigs/Wave_01");

			Assert.That(openingWave, Is.Not.Null);
			Assert.That(openingWave.EnemySpawns, Is.Not.Empty);
			Assert.That(openingWave.EnemySpawns[0].healthMultiplier, Is.EqualTo(0.8f).Within(0.0001f));
		}

		[Test]
		public void MidRunWaveKeepsRoleCompositionWithPlayableHealthPressure()
		{
			var midRun = Resources.Load<WaveConfig>("WaveConfigs/Wave_02");

			Assert.That(midRun, Is.Not.Null);
			Assert.That(midRun.EnemySpawns, Has.Count.EqualTo(2));
			Assert.That(midRun.HealthScaling, Is.EqualTo(0.9f).Within(0.0001f));
		}

		[Test]
		public void MidRunUsesPlayableEnemyCountBeforeFinalWave()
		{
			var midRun = Resources.Load<WaveConfig>("WaveConfigs/Wave_02");

			Assert.That(midRun, Is.Not.Null);
			Assert.That(midRun.CountScaling, Is.EqualTo(1f).Within(0.0001f));
			var totalEnemies = 0;
			foreach (var spawn in midRun.EnemySpawns)
				totalEnemies += Mathf.Max(1, Mathf.RoundToInt(spawn.count * midRun.CountScaling));

			Assert.That(midRun.EnemySpawns[0].count, Is.EqualTo(8));
			Assert.That(midRun.EnemySpawns[1].count, Is.EqualTo(4));
			Assert.That(totalEnemies, Is.EqualTo(12));
		}

		[Test]
		public void FinalWaveKeepsBossCompositionWithFormedBuildPressure()
		{
			var final = Resources.Load<WaveConfig>("WaveConfigs/Wave_03");

			Assert.That(final, Is.Not.Null);
			Assert.That(final.EnemySpawns, Is.Not.Empty);
			Assert.That(ContainsArchetype(final, MonsterArchetype.Berserker), Is.True);
			Assert.That(final.CountScaling, Is.EqualTo(1f).Within(0.0001f));
			Assert.That(final.EnemySpawns[0].count, Is.EqualTo(12));
			Assert.That(final.EnemySpawns[1].count, Is.EqualTo(8));
			Assert.That(final.EnemySpawns[2].count, Is.EqualTo(1));
			Assert.That(final.GetTotalEnemyCount(), Is.EqualTo(21));
		}

		private static bool ContainsArchetype(WaveConfig wave, MonsterArchetype archetype)
		{
			foreach (var spawn in wave.EnemySpawns)
			{
				var stats = spawn != null && spawn.enemyPrefab != null
					? spawn.enemyPrefab.GetComponent<MonsterStats>()
					: null;
				if (stats != null && stats.statsSO != null && stats.statsSO.Archetype == archetype)
					return true;
			}

			return false;
		}
	}
}
