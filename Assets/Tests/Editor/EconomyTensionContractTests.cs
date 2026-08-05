using System.Reflection;
using NUnit.Framework;
using TD.GameLoop;
using TD.Towers;
using UnityEngine;

namespace TD.Tests
{
	public class EconomyTensionContractTests
	{
		private const string StartingReserveKey = "TD3D.StartingReserve";
		private GameObject resourceManagerObject;
		private ResourceManager resourceManager;
		private GameObject waveManagerObject;
		private WaveManager waveManager;

		[SetUp]
		public void SetUp()
		{
			PlayerPrefs.DeleteKey(StartingReserveKey);
			PlayerPrefs.Save();
			resourceManagerObject = new GameObject("EconomyTensionContractTests");
			resourceManager = resourceManagerObject.AddComponent<ResourceManager>();
			SetPrivateStaticField(typeof(ResourceManager), "<Instance>k__BackingField", resourceManager);
			waveManagerObject = new GameObject("EconomyTensionRewardContractTests");
			waveManager = waveManagerObject.AddComponent<WaveManager>();
			SetPrivateStaticField(typeof(WaveManager), "<Instance>k__BackingField", waveManager);
		}

		[TearDown]
		public void TearDown()
		{
			PlayerPrefs.DeleteKey(StartingReserveKey);
			PlayerPrefs.Save();
			Object.DestroyImmediate(waveManagerObject);
			Object.DestroyImmediate(resourceManagerObject);
		}

		[Test]
		public void StartingBankCannotBuyGeneralistAndAreaTowerTogether()
		{
			var generalist = Resources.Load<TowerStatsSO>("TowerStats/TowerStatsSO 00 Basic");
			var area = Resources.Load<TowerStatsSO>("TowerStats/TowerStatsSO 01 Tesla");

			Assert.That(generalist, Is.Not.Null);
			Assert.That(area, Is.Not.Null);
			Assert.That(resourceManager.CurrentCurrency, Is.EqualTo(50));
			Assert.That(generalist.Cost + area.Cost, Is.GreaterThan(resourceManager.CurrentCurrency));

			Assert.That(resourceManager.TrySpend(generalist.Cost), Is.True);
			Assert.That(resourceManager.CanAfford(area.Cost), Is.False);

			resourceManager.Reset();

			Assert.That(resourceManager.TrySpend(area.Cost), Is.True);
			Assert.That(resourceManager.CanAfford(generalist.Cost), Is.False);
		}

		[Test]
		public void ResourceCacheCatchesUpToOneBasicPurchaseAfterWeakWave()
		{
			var basic = Resources.Load<TowerStatsSO>("TowerStats/TowerStatsSO 00 Basic");
			Assert.That(basic, Is.Not.Null);
			Assert.That(resourceManager.TrySpend(40), Is.True);
			Assert.That(resourceManager.CurrentCurrency, Is.EqualTo(10));

			SetPrivateField(waveManager, "rewardOfferPending", true);
			SetPrivateField(waveManager, "rewardOfferId", "offer-1");
			Assert.That(waveManager.IsRewardOfferPending, Is.True);
			Assert.That(waveManager.RewardOfferId, Is.EqualTo("offer-1"));
			Assert.That(ResourceManager.Instance, Is.SameAs(resourceManager));
			Assert.That(waveManager.SelectRewardOffer("offer-1", (int)RewardOfferChoice.ResourceCache), Is.True);

			Assert.That(waveManager.LastRewardCurrencyAmount, Is.EqualTo(basic.Cost - 10));
			Assert.That(resourceManager.CurrentCurrency, Is.EqualTo(basic.Cost));
			Assert.That(resourceManager.CurrentCurrency, Is.LessThan(basic.Cost + basic.UpgradeCost.BaseValueInt));
		}

		private static void SetPrivateField(object target, string fieldName, object value)
		{
			var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, $"Missing private field: {fieldName}");
			field.SetValue(target, value);
		}

		private static void SetPrivateStaticField(System.Type type, string fieldName, object value)
		{
			var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, $"Missing private static field: {type.Name}.{fieldName}");
			field.SetValue(null, value);
		}
	}
}
