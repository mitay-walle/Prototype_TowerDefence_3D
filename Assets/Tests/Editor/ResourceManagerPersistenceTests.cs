using NUnit.Framework;
using TD.GameLoop;
using UnityEngine;

namespace TD.Tests
{
	public class ResourceManagerPersistenceTests
	{
		private const string StartingReserveKey = "TD3D.StartingReserve";
		private GameObject resourceManagerObject;
		private ResourceManager resourceManager;

		[SetUp]
		public void SetUp()
		{
			PlayerPrefs.DeleteKey(StartingReserveKey);
			PlayerPrefs.Save();
			resourceManagerObject = new GameObject("ResourceManagerPersistenceTests");
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
		public void FreshProfileResetKeepsAuthoredStartingCurrency()
		{
			var startingCurrency = resourceManager.CurrentCurrency;

			resourceManager.Reset();

			Assert.AreEqual(startingCurrency, resourceManager.CurrentCurrency);
		}

		[Test]
		public void StartingReserveUnlockSurvivesOwnerReset()
		{
			var startingCurrency = resourceManager.CurrentCurrency;

			resourceManager.UnlockStartingReserve();
			resourceManager.Reset();

			Assert.AreEqual(startingCurrency + 25, resourceManager.CurrentCurrency);
			resourceManager.UnlockStartingReserve();
			resourceManager.Reset();
			Assert.AreEqual(startingCurrency + 25, resourceManager.CurrentCurrency);
		}
	}
}