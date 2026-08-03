using System.Linq;
using NUnit.Framework;
using TD.GameLoop;
using TD.Monsters;
using TD.Towers;
using UnityEngine;

namespace TD.Tests
{
	public class EconomyContractTests
	{
		private const string StartingReserveKey = "TD3D.StartingReserve";
		private GameObject resourceManagerObject;
		private ResourceManager resourceManager;

		[SetUp]
		public void SetUp()
		{
			PlayerPrefs.DeleteKey(StartingReserveKey);
			PlayerPrefs.Save();
			resourceManagerObject = new GameObject("EconomyContractTests");
			resourceManager = resourceManagerObject.AddComponent<ResourceManager>();
		}

		[TearDown]
		public void TearDown()
		{
			PlayerPrefs.DeleteKey(StartingReserveKey);
			PlayerPrefs.Save();
			Object.DestroyImmediate(resourceManagerObject);
		}

		[Test]
		public void WaveOneReserveKeepsTowerChoicesSeparate()
		{
			var basicStats = Resources.Load<TowerStatsSO>("TowerStats/TowerStatsSO 00 Basic");
			var waveOne = Resources.Load<WaveConfig>("WaveConfigs/Wave_01");
			Assert.That(basicStats, Is.Not.Null);
			Assert.That(waveOne, Is.Not.Null);
			Assert.That(waveOne.EnemySpawns, Has.Count.EqualTo(1));
			Assert.That(waveOne.EnemySpawns.Single().count, Is.EqualTo(7));

			MonsterStats turtle;
			Assert.That(waveOne.EnemySpawns.Single().enemyPrefab.TryGetComponent(out turtle), Is.True);
			var turtleStats = turtle.statsSO;
			Assert.That(turtleStats.InstantReward.BaseValueInt, Is.EqualTo(2));
			Assert.That(waveOne.CompletionReward, Is.EqualTo(5));
			Assert.That(basicStats.Cost, Is.EqualTo(25));
			Assert.That(basicStats.UpgradeCost.BaseValueInt, Is.EqualTo(30));
			Assert.That(resourceManager.CurrentCurrency, Is.EqualTo(50));
			Assert.That(resourceManager.TrySpend(basicStats.Cost), Is.True);

			var waveOneKillReward = waveOne.EnemySpawns.Sum(enemySpawn =>
				enemySpawn.count * turtleStats.InstantReward.BaseValueInt);
			resourceManager.AddCurrency(waveOneKillReward + waveOne.CompletionReward);
			resourceManager.GivePassiveIncome();

			var reserveAfterWaveOne = resourceManager.CurrentCurrency;
			var reserveWithCache = reserveAfterWaveOne + WaveManager.ResourceCacheAmount;
			var nextCompletionWithBounty = waveOne.CompletionReward + WaveManager.BountyContractBonus;
			var combinedTowerSpend = basicStats.Cost + basicStats.UpgradeCost.BaseValueInt;

			Assert.That(reserveAfterWaveOne, Is.EqualTo(49));
			Assert.That(reserveWithCache, Is.EqualTo(54));
			Assert.That(nextCompletionWithBounty, Is.EqualTo(20));
			Assert.That(reserveAfterWaveOne, Is.GreaterThanOrEqualTo(basicStats.Cost));
			Assert.That(reserveAfterWaveOne, Is.GreaterThanOrEqualTo(basicStats.UpgradeCost.BaseValueInt));
			Assert.That(reserveAfterWaveOne, Is.LessThan(combinedTowerSpend));
			Assert.That(reserveWithCache, Is.LessThan(combinedTowerSpend));
		}
	}
}