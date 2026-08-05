using System.Reflection;
using NUnit.Framework;
using TD.GameLoop;
using TD.Levels;
using TD.Towers;
using UnityEngine;
using UnityEngine.Events;

namespace TD.Tests
{
	public class RewardOfferContractTests
	{
		[Test]
		public void RewardOfferRequiresMatchingPendingIdAndRejectsDuplicateSelection()
		{
			var gameObject = new GameObject("RewardOfferContractTests");
			var waveManager = gameObject.AddComponent<WaveManager>();
			SetPrivateField(waveManager, "rewardOfferPending", true);
			SetPrivateField(waveManager, "rewardOfferId", "offer-1");
			SetPrivateField(waveManager, "rewardOfferResolved", false);
			SetPrivateField(waveManager, "selectedRewardId", string.Empty);

			try
			{
				Assert.That(
					waveManager.SelectRewardOffer("wrong-offer", (int)RewardOfferChoice.BountyContract),
					Is.False);
				Assert.That(waveManager.IsRewardOfferPending, Is.True);

				Assert.That(
					waveManager.SelectRewardOffer("offer-1", (int)RewardOfferChoice.BountyContract),
					Is.True);
				Assert.That(waveManager.IsRewardOfferPending, Is.False);
				Assert.That(waveManager.HasSelectedReward, Is.True);
				Assert.That(waveManager.SelectedRewardId, Is.EqualTo(nameof(RewardOfferChoice.BountyContract)));

				Assert.That(
					waveManager.SelectRewardOffer("offer-1", (int)RewardOfferChoice.BountyContract),
					Is.False);
				Assert.That(waveManager.SelectedRewardId, Is.EqualTo(nameof(RewardOfferChoice.BountyContract)));
			}
			finally
			{
				Object.DestroyImmediate(gameObject);
			}
		}

		[Test]
		public void RewardOfferCreatedExposesOfferIdEventContract()
		{
			var eventField = typeof(WaveManager).GetField("onRewardOfferCreated", BindingFlags.Instance | BindingFlags.Public);

			Assert.That(eventField, Is.Not.Null);
			Assert.That(eventField.FieldType, Is.EqualTo(typeof(UnityEvent<string>)));
			Assert.That(typeof(WaveManager).GetProperty(nameof(WaveManager.RewardOfferId)), Is.Not.Null);
		}

		[Test]
		public void EmergencyRepairsRestoreBaseToRecoveryBand()
		{
			Assert.That(WaveManager.EmergencyRepairTargetHealthFraction, Is.EqualTo(0.75f));
			Assert.That(WaveManager.GetEmergencyRepairAmount(1, 20), Is.EqualTo(14));
			Assert.That(WaveManager.GetEmergencyRepairAmount(5, 20), Is.EqualTo(10));
			Assert.That(WaveManager.GetEmergencyRepairAmount(15, 20), Is.EqualTo(0));
			Assert.That(WaveManager.GetEmergencyRepairAmount(20, 20), Is.EqualTo(0));
		}

		[Test]
		public void TerminalStateBlocksWaveResolutionAndRewards()
		{
			Assert.That(WaveManager.ShouldBlockTerminalResolution(true, false), Is.True);
			Assert.That(WaveManager.ShouldBlockTerminalResolution(false, true), Is.True);
			Assert.That(WaveManager.ShouldBlockTerminalResolution(false, false), Is.False);
		}

		[Test]
		public void TerminalBaseRejectsRewardSelectionAndForceStopClearsOffer()
		{
			var waveObject = new GameObject("RewardOfferContractTests.TerminalWaveManager");
			var baseObject = new GameObject("RewardOfferContractTests.TerminalBase");
			var waveManager = waveObject.AddComponent<WaveManager>();
			baseObject.AddComponent<BoxCollider>();
			var playerBase = baseObject.AddComponent<PlayerBase>();
			SetPrivateField(waveManager, "rewardOfferPending", true);
			SetPrivateField(waveManager, "rewardOfferId", "terminal-offer");
			SetPrivateField(waveManager, "targetBase", playerBase);

			try
			{
				playerBase.Initialize(1);
				playerBase.TakeDamage(1);

				Assert.That(playerBase.IsDestroyed, Is.True);
				Assert.That(
					waveManager.SelectRewardOffer("terminal-offer", (int)RewardOfferChoice.ResourceCache),
					Is.False);
				Assert.That(waveManager.IsRewardOfferPending, Is.True);

				waveManager.ForceStopWave();
				Assert.That(waveManager.IsRewardOfferPending, Is.False);
				Assert.That(waveManager.HasSelectedReward, Is.False);
			}
			finally
			{
				Object.DestroyImmediate(waveObject);
				Object.DestroyImmediate(baseObject);
			}
		}

		[Test]
		public void InitializeBindsInterWaveOwnersFromBootstrap()
		{
			var waveObject = new GameObject("RewardOfferContractTests.WaveManager");
			var baseObject = new GameObject("RewardOfferContractTests.PlayerBase");
			var mapObject = new GameObject("RewardOfferContractTests.TileMapManager");
			var tileObject = new GameObject("RewardOfferContractTests.TilePlacementSystem");
			var waveManager = waveObject.AddComponent<WaveManager>();
			baseObject.AddComponent<BoxCollider>();
			var playerBase = baseObject.AddComponent<PlayerBase>();
			var tileMapManager = mapObject.AddComponent<TileMapManager>();
			var tilePlacementSystem = tileObject.AddComponent<TilePlacementSystem>();

			try
			{
				waveManager.Initialize(null, new Transform[0], playerBase, tileMapManager, tilePlacementSystem);

				Assert.That(GetPrivateField<TileMapManager>(waveManager, "tileMapManager"), Is.SameAs(tileMapManager));
				Assert.That(GetPrivateField<TilePlacementSystem>(waveManager, "tilePlacementSystem"), Is.SameAs(tilePlacementSystem));
			}
			finally
			{
				Object.DestroyImmediate(waveObject);
				Object.DestroyImmediate(baseObject);
				Object.DestroyImmediate(mapObject);
				Object.DestroyImmediate(tileObject);
			}
		}

		private static T GetPrivateField<T>(object target, string fieldName)
		{
			return (T)typeof(WaveManager)
				.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
				.GetValue(target);
		}

		private static void SetPrivateField(object target, string fieldName, object value)
		{
			typeof(WaveManager)
				.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
				.SetValue(target, value);
		}
	}
}
