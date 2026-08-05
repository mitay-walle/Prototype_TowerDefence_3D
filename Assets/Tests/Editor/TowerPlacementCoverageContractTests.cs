using System.Collections.Generic;
using NUnit.Framework;
using TD.Towers;
using UnityEditor;
using UnityEngine;

namespace TD.Tests
{
	public class TowerPlacementCoverageContractTests
	{
		[Test]
		public void CoveragePreviewCountsEntrancesWithinTowerRange()
		{
			var entrances = new List<Vector3>
			{
				new Vector3(0f, 0f, 0f),
				new Vector3(3.5f, 0f, 0f),
				new Vector3(3.51f, 0f, 0f)
			};

			var coveredEntrances = TowerPlacementSystem.CountCoveredEntrances(Vector3.zero, 3.5f, entrances);

			Assert.That(coveredEntrances, Is.EqualTo(2));
		}

		[Test]
		public void DisabledTowerDoesNotContributeToCombinedCoverage()
		{
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Towers/Tower_00 Novice.prefab");
			Assert.That(prefab, Is.Not.Null);

			var towerObject = Object.Instantiate(prefab);
			try
			{
				if (!towerObject.TryGetComponent<Tower>(out var tower))
				{
					Assert.Fail("Tower prefab must contain a Tower component.");
					return;
				}

				tower.enabled = false;
				var coveredEntrances = TowerPlacementSystem.CountCoveredEntrances(
					new List<Tower> { tower },
					new List<Vector3> { Vector3.zero });

				Assert.That(coveredEntrances, Is.EqualTo(0));
			}
			finally
			{
				Object.DestroyImmediate(towerObject);
			}
		}

		[Test]
		public void RouteCoverageCountsPathSamplesWithinTowerRange()
		{
			var routeSamples = new List<Vector3>
			{
				new Vector3(0f, 0f, 0f),
				new Vector3(3.5f, 0f, 0f),
				new Vector3(3.51f, 0f, 0f)
			};

			var coveredSamples = TowerPlacementSystem.CountCoveredRouteSamples(Vector3.zero, 3.5f, routeSamples);

			Assert.That(coveredSamples, Is.EqualTo(2));
		}

		[Test]
		public void CombinedCoverageAddsCandidateToExistingBuild()
		{
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Towers/Tower_00 Novice.prefab");
			Assert.That(prefab, Is.Not.Null);

			var towerObject = Object.Instantiate(prefab);
			try
			{
				if (!towerObject.TryGetComponent<Tower>(out var tower) || tower.Stats == null || tower.Stats.statsSO == null)
				{
					Assert.Fail("Tower prefab must contain initialized TowerStats.");
					return;
				}

				tower.transform.position = Vector3.zero;

				var entrances = new List<Vector3>
				{
					new Vector3(0f, 0f, 0f),
					new Vector3(3.5f, 0f, 0f),
					new Vector3(7f, 0f, 0f)
				};

				var combinedCoverage = TowerPlacementSystem.CountCoveredEntrances(
					new List<Tower> { tower },
					new Vector3(3.5f, 0f, 0f),
					3.5f,
					entrances);

				Assert.That(combinedCoverage, Is.EqualTo(3));
			}
			finally
			{
				Object.DestroyImmediate(towerObject);
			}
		}

		[Test]
		public void CombinedRouteCoverageCountsCandidateWhenBuildIsEmpty()
		{
			var routeSamples = new List<Vector3>
			{
				new Vector3(0f, 0f, 0f),
				new Vector3(3.5f, 0f, 0f),
				new Vector3(7f, 0f, 0f)
			};

			var combinedCoverage = TowerPlacementSystem.CountCoveredRouteSamples(
				new List<Tower>(),
				new Vector3(3.5f, 0f, 0f),
				3.5f,
				routeSamples);

			Assert.That(combinedCoverage, Is.EqualTo(3));
		}

		[Test]
		public void CommittedTowerPlacementPublishesDecisionDetails()
		{
			var placementObject = new GameObject("TowerPlacementContract");
			var placementSystem = placementObject.AddComponent<TowerPlacementSystem>();
			string details = null;
			placementSystem.onTowerPlaced.AddListener(value => details = value);

			placementSystem.onTowerPlaced.Invoke("tower=Tower_00 Novice;cost=25;coverage=2/4;position=(-7.00, 0.50, 3.00);currencyAfter=25");

			Assert.That(details, Does.Contain("tower=Tower_00 Novice"));
			Assert.That(details, Does.Contain("coverage=2/4"));
			Assert.That(details, Does.Contain("currencyAfter=25"));
			Object.DestroyImmediate(placementObject);
		}
	}
}
