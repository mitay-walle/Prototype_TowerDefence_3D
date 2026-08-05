using TD.Interactions;
using System.Collections.Generic;
using TD.MLAgents;
using TD.Levels;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using NUnit.Framework;

	public class TowerDefenceMlAgentsContractTests
	{
		private const string GameplayScenePath = "Assets/Scenes/Gameplay.unity";

		[Test]
		public void PlayerCombatTelemetryProvidesDenseShapingRewards()
		{
			Assert.That(TowerDefenceAgent.GetTelemetryEventReward("TowerTargetAcquired"), Is.EqualTo(TowerDefenceAgent.TargetAcquiredReward));
			Assert.That(TowerDefenceAgent.GetTelemetryEventReward("TowerFired"), Is.EqualTo(TowerDefenceAgent.TowerFiredReward));
			Assert.That(TowerDefenceAgent.GetTelemetryEventReward("MonsterDeath"), Is.EqualTo(TowerDefenceAgent.EnemyKilledReward));
			Assert.That(TowerDefenceAgent.GetTelemetryEventReward("EnemyLeaked"), Is.EqualTo(TowerDefenceAgent.EnemyLeakedPenalty));
			Assert.That(TowerDefenceAgent.GetTelemetryEventReward("WaveStarted"), Is.EqualTo(0f));
		}

		[Test]
		public void PlayerUsesDocumentedChallengeBaseline()
		{
			Assert.That(TowerDefenceAgent.GetAutomaticChallengeModifier(), Is.EqualTo(TD.GameLoop.ChallengeModifier.ControlledPressure));
			Assert.That(TD.GameLoop.ChallengeModifierCatalog.IsSelectable(TowerDefenceAgent.GetAutomaticChallengeModifier()), Is.True);
		}

		[Test]
		public void PlayerPrioritizesUpgradeOnlyWhenCoveragePlacementIsNotRequired()
		{
			Assert.That(TowerDefenceAgent.ShouldPrioritizeUpgrade(false, true), Is.True);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeUpgrade(true, true), Is.False);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeUpgrade(false, false), Is.False);
		}

		[Test]
		public void PlayerStopsPrioritizingRejectedPlacementOwner()
		{
			Assert.That(TowerDefenceAgent.ShouldPrioritizePlacement(false, true, false), Is.True);
			Assert.That(TowerDefenceAgent.ShouldPrioritizePlacement(true, true, true), Is.False);
			Assert.That(TowerDefenceAgent.ShouldPrioritizePlacement(false, false, true), Is.True);
		}

		[Test]
		public void PlayerReinforcesCoveredBuildBeforeStartingNextWave()
		{
			Assert.That(TowerDefenceAgent.ShouldPrioritizeReinforcementPlacement(27, 25, 2, 1f, false), Is.True);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeReinforcementPlacement(24, 25, 2, 1f, false), Is.False);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeReinforcementPlacement(27, 25, 2, 1f, true), Is.False);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeReinforcementPlacement(27, 25, 2, 0.8f, false), Is.False);
		}

		[Test]
		public void PlayerReservesFinalWavePreparationForAffordableUpgrade()
		{
			Assert.That(TowerDefenceAgent.ShouldPrioritizeFinalWaveUpgrade(2, 3, true, 1f), Is.True);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeFinalWaveUpgrade(2, 3, true, 0.75f), Is.False);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeFinalWaveUpgrade(1, 3, true, 1f), Is.False);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeFinalWaveUpgrade(2, 3, false, 1f), Is.False);
		}

		[Test]
		public void PlayerPrefersNewTowerWhenItsCombatPowerBeatsUpgradeGain()
		{
			Assert.That(
				TowerDefenceAgent.ShouldPreferNewTowerOverUpgrade(54, 40, 15f, 40, 0.5f, 3, 1f),
				Is.True);
			Assert.That(
				TowerDefenceAgent.ShouldPreferNewTowerOverUpgrade(54, 40, 9f, 40, 5f, 3, 1f),
				Is.False);
			Assert.That(
				TowerDefenceAgent.ShouldPreferNewTowerOverUpgrade(54, 40, 15f, 40, 0.5f, 3, 0.75f),
				Is.False);
		}

		[Test]
		public void PlayerUsesRouteReinforcementBeforeUpgradeWhenCoverageCannotBeReached()
		{
			Assert.That(TowerDefenceAgent.ShouldPrioritizeRouteReinforcementPlacement(25, 25, 2, 0.5f, true), Is.True);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeRouteReinforcementPlacement(25, 25, 2, 0.5f, false), Is.False);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeRouteReinforcementPlacement(24, 25, 2, 0.5f, true), Is.False);
		}

		[Test]
		public void PlayerUsesRepairsOnlyWhenHealthIsLowAndBuildCanSurviveWithoutCatchUpCash()
		{
			Assert.That(TowerDefenceAgent.ShouldPrioritizeEmergencyRepairs(14, 20, 4, 1, 25), Is.False);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeEmergencyRepairs(14, 20, 4, 2, 25), Is.False);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeEmergencyRepairs(10, 20, 4, 2, 25), Is.True);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeEmergencyRepairs(14, 20, 25, 1, 25), Is.True);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeEmergencyRepairs(20, 20, 4, 2, 25), Is.False);
		}

		[Test]
		public void PlayerUsesBountyOnlyWhenFutureWaveAndBuildCanCarryDelayedReward()
		{
			Assert.That(TowerDefenceAgent.ShouldPrioritizeBountyContract(1, 3, 17, 20, 28, 2, 25, 1f), Is.True);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeBountyContract(3, 3, 20, 20, 28, 2, 25, 1f), Is.False);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeBountyContract(1, 3, 15, 20, 28, 2, 25, 1f), Is.False);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeBountyContract(1, 3, 17, 20, 24, 2, 25, 1f), Is.False);
			Assert.That(TowerDefenceAgent.ShouldPrioritizeBountyContract(1, 3, 17, 20, 28, 2, 25, 0.75f), Is.False);
		}

		[Test]
		public void GameplaySmokeIsolationHasTrainingModeSeam()
		{
			Assert.That(typeof(TowerDefenceAgent).GetProperty(nameof(TowerDefenceAgent.TrainingMode)), Is.Not.Null);
			Assert.That(typeof(TowerDefenceBalancerAgent).GetProperty(nameof(TowerDefenceBalancerAgent.TrainingMode)), Is.Not.Null);
			Assert.That(typeof(TowerDefenceEnemyLevelAgent).GetProperty(nameof(TowerDefenceEnemyLevelAgent.TrainingMode)), Is.Not.Null);
		}

		[Test]
		public void PlayerPlacementRewardPrioritizesNewEntranceCoverage()
		{
			var uncoveredPlacement = TowerDefenceAgent.GetPlacementReward(0f, 1f);
			var coveredPlacement = TowerDefenceAgent.GetPlacementReward(0.1f, -1f);
			var moreCoveredPlacement = TowerDefenceAgent.GetPlacementReward(0.2f, -1f);

			Assert.That(uncoveredPlacement, Is.LessThan(0f));
			Assert.That(coveredPlacement, Is.GreaterThan(uncoveredPlacement));
			Assert.That(moreCoveredPlacement, Is.GreaterThan(coveredPlacement));
		}

		[Test]
		public void PlayerForcesCoveragePlacementWhenAffordable()
		{
			Assert.That(TowerDefenceAgent.ShouldForceCoveragePlacement(13, 0, true), Is.True);
			Assert.That(TowerDefenceAgent.ShouldForceCoveragePlacement(13, 13, true), Is.False);
			Assert.That(TowerDefenceAgent.ShouldForceCoveragePlacement(13, 0, false), Is.False);
		}

		[Test]
		public void PlayerHoldsCoverageOnlyWhenReachablePlacementExists()
		{
			Assert.That(TowerDefenceAgent.ShouldHoldForCoverage(4, 2, true, true), Is.True);
			Assert.That(TowerDefenceAgent.ShouldHoldForCoverage(4, 2, true, false), Is.False);
		}

		[Test]
		public void PlayerSelectsTileWithHigherPostPlacementCoverage()
		{
			var choices = new List<TilePlacementChoice>
			{
				CreateTileChoice("low-coverage", 3),
				CreateTileChoice("high-coverage", 1)
			};

			var coveredEntrancesAfter = new[] { 1, 2 };
			var totalEntrancesAfter = new[] { 4, 4 };

			Assert.That(
				TowerDefenceAgent.ChooseBestTileOption(choices, coveredEntrancesAfter, totalEntrancesAfter),
				Is.EqualTo(1));
		}

		[Test]
		public void PlayerSelectsTileWithHigherRouteCoverageWhenAnchorCoverageTies()
		{
			var choices = new List<TilePlacementChoice>
			{
				CreateTileChoice("low-route", 2),
				CreateTileChoice("high-route", 2)
			};

			var coveredEntrancesAfter = new[] { 2, 2 };
			var totalEntrancesAfter = new[] { 4, 4 };
			var coveredRouteSamplesAfter = new[] { 8, 20 };
			var totalRouteSamplesAfter = new[] { 20, 20 };

			Assert.That(
				TowerDefenceAgent.ChooseBestTileOption(
					choices,
					coveredEntrancesAfter,
					totalEntrancesAfter,
					coveredRouteSamplesAfter,
					totalRouteSamplesAfter),
				Is.EqualTo(1));
		}

		[Test]
		public void PlayerPreservesExistingCoverageWhenAValidTileAlternativeExists()
		{
			var choices = new List<TilePlacementChoice>
			{
				CreateTileChoice("coverage-loss", 1),
				CreateTileChoice("coverage-preserving", 3)
			};

			var coveredEntrancesAfter = new[] { 1, 3 };
			var totalEntrancesAfter = new[] { 2, 4 };
			var coveredRouteSamplesAfter = new[] { 20, 8 };
			var totalRouteSamplesAfter = new[] { 20, 20 };

			Assert.That(
				TowerDefenceAgent.ChooseBestTileOption(
					choices,
					coveredEntrancesAfter,
					totalEntrancesAfter,
					coveredRouteSamplesAfter,
					totalRouteSamplesAfter,
					3,
					4),
				Is.EqualTo(1));
		}

		[Test]
		public void PlayerAvoidsOpenEndExpansionWhenRouteGainIsMinor()
		{
			var choices = new List<TilePlacementChoice>
			{
				CreateTileChoice("closed-route", 4),
				CreateTileChoice("expanded-route", 6)
			};

			var coveredEntrancesAfter = new[] { 2, 1 };
			var totalEntrancesAfter = new[] { 4, 6 };
			var coveredRouteSamplesAfter = new[] { 15, 16 };
			var totalRouteSamplesAfter = new[] { 20, 20 };

			Assert.That(
				TowerDefenceAgent.ChooseBestTileOption(
					choices,
					coveredEntrancesAfter,
					totalEntrancesAfter,
					coveredRouteSamplesAfter,
					totalRouteSamplesAfter),
				Is.EqualTo(0));
		}

		[Test]
		public void PlayerKeepsBudgetForNextTowerWhenCoverageTies()
		{
			var costs = new[] { 40, 25, 33 };
			var coverageGains = new[] { 2, 2, 1 };
			var placementAvailable = new[] { true, true, true };

			Assert.That(
				TowerDefenceAgent.ChooseBestAffordableTower(costs, coverageGains, placementAvailable, 50, 25),
				Is.EqualTo(1));
		}

		[Test]
		public void PlayerChoosesCoverageOverCheaperTower()
		{
			var costs = new[] { 25, 40 };
			var coverageGains = new[] { 1, 2 };
			var placementAvailable = new[] { true, true };

			Assert.That(
				TowerDefenceAgent.ChooseBestAffordableTower(costs, coverageGains, placementAvailable, 50, 25),
				Is.EqualTo(1));
		}

		[Test]
		public void PlayerUsesAreaCounterOnOpeningCoverageTie()
		{
			var costs = new[] { 40, 25 };
			var coverageGains = new[] { 1, 1 };
			var placementAvailable = new[] { true, true };
			var areaRoles = new[] { true, false };

			Assert.That(
				TowerDefenceAgent.ChooseBestAffordableTower(
					costs,
					coverageGains,
					placementAvailable,
					75,
					25,
					areaRoles,
					true),
				Is.EqualTo(0));
		}

		[Test]
		public void PlayerPreservesBasicReserveWhenOpeningAreaRoleConsumesIt()
		{
			var costs = new[] { 40, 25 };
			var coverageGains = new[] { 1, 1 };
			var placementAvailable = new[] { true, true };
			var areaRoles = new[] { true, false };

			Assert.That(
				TowerDefenceAgent.ChooseBestAffordableTower(
					costs,
					coverageGains,
					placementAvailable,
					50,
					25,
					areaRoles,
					true),
				Is.EqualTo(1));
		}

		[Test]
		public void PlayerSpendsOpeningReserveForAreaCounterAgainstSwarmIntel()
		{
			var costs = new[] { 40, 25 };
			var coverageGains = new[] { 1, 1 };
			var placementAvailable = new[] { true, true };
			var areaRoles = new[] { true, false };

			Assert.That(
				TowerDefenceAgent.ShouldPreferAreaCounter(7, true, true, 50, 40),
				Is.True);
			Assert.That(
				TowerDefenceAgent.ShouldPreferAreaCounter(8, true, true, 50, 40),
				Is.True);
			Assert.That(
				TowerDefenceAgent.ChooseBestAffordableTower(
					costs,
					coverageGains,
					placementAvailable,
					50,
					25,
					areaRoles,
					true,
					7),
				Is.EqualTo(0));
			Assert.That(
				TowerDefenceAgent.ChooseBestAffordableTower(
					costs,
					coverageGains,
					placementAvailable,
					50,
					25,
					areaRoles,
					true,
					6),
				Is.EqualTo(1));
		}

		[Test]
		public void PlayerRejectsLowPowerTowerForMinorCoverageGain()
		{
			var costs = new[] { 25, 33 };
			var coverageGains = new[] { 1, 2 };
			var placementAvailable = new[] { true, true };
			var combatPowers = new[] { 9f, 1f };

			Assert.That(
				TowerDefenceAgent.ChooseBestAffordableTower(
					costs,
					coverageGains,
					placementAvailable,
					33,
					25,
					null,
					false,
					0,
					combatPowers),
				Is.EqualTo(0));
		}

		[Test]
		public void PlayerPreservesOpeningReserveAgainstOneEntranceCoverageAdvantage()
		{
			var costs = new[] { 33, 25 };
			var coverageGains = new[] { 2, 1 };
			var placementAvailable = new[] { true, true };

			Assert.That(
				TowerDefenceAgent.ChooseBestAffordableTower(
					costs,
					coverageGains,
					placementAvailable,
					50,
					25,
					null,
					true),
				Is.EqualTo(1));
		}

		[Test]
		public void PlayerCanSpendOpeningReserveForTwoEntranceCoverageAdvantage()
		{
			var costs = new[] { 33, 25 };
			var coverageGains = new[] { 3, 1 };
			var placementAvailable = new[] { true, true };

			Assert.That(
				TowerDefenceAgent.ChooseBestAffordableTower(
					costs,
					coverageGains,
					placementAvailable,
					50,
					25,
					null,
					true),
				Is.EqualTo(0));
		}

		[Test]
		public void PlayerKeepsCoveragePriorityOverOpeningAreaCounter()
		{
			var costs = new[] { 40, 25 };
			var coverageGains = new[] { 1, 2 };
			var placementAvailable = new[] { true, true };
			var areaRoles = new[] { true, false };

			Assert.That(
				TowerDefenceAgent.ChooseBestAffordableTower(
					costs,
					coverageGains,
					placementAvailable,
					50,
					25,
					areaRoles,
					true),
				Is.EqualTo(1));
		}

		[Test]
		public void PlayerPlacementScorePrioritizesRouteExposure()
		{
			var weakRouteScore = TowerDefenceAgent.GetPlacementSlotScore(1, 10, 38, 0.8f, 0.5f);
			var strongRouteScore = TowerDefenceAgent.GetPlacementSlotScore(1, 20, 38, 0.1f, 0.1f);

			Assert.That(strongRouteScore, Is.GreaterThan(weakRouteScore));
		}

		[Test]
		public void PlayerPrioritizesNewEntranceCoverageWhenCoverageIsRequired()
		{
			var routeHeavyChoice = TowerDefenceAgent.GetPlacementSlotScore(1, 38, 38, 0f, 0f, true);
			var coverageHeavyChoice = TowerDefenceAgent.GetPlacementSlotScore(2, 0, 38, 0f, 0f, true);

			Assert.That(coverageHeavyChoice, Is.GreaterThan(routeHeavyChoice));
		}

		[Test]
	public void GameplaySceneContainsConfiguredTd3dAgent()
	{
		EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
		var agentObject = GameObject.Find("TD ML Agent");
		Assert.That(agentObject, Is.Not.Null, "Run TD/ML-Agents/Setup Gameplay Agent first.");
		Assert.That(agentObject.GetComponent<TowerDefenceAgent>(), Is.Not.Null);
		Assert.That(agentObject.GetComponent<BehaviorParameters>(), Is.Not.Null);
		Assert.That(agentObject.GetComponent<DecisionRequester>(), Is.Not.Null);
		var serializedAgent = new SerializedObject(agentObject.GetComponent<TowerDefenceAgent>());
		serializedAgent.Update();
		Assert.That(serializedAgent.FindProperty("_applyMlTestTimeScale").boolValue, Is.True);
		Assert.That(serializedAgent.FindProperty("_mlTestTimeScale").floatValue, Is.EqualTo(TowerDefenceAgent.DefaultMlTestTimeScale).Within(0.001f));
		Assert.That(agentObject.GetComponent<TowerDefenceAgent>().MaxStep, Is.EqualTo(0));
		Assert.That(agentObject.GetComponentsInChildren<SyntheticMouse>(true), Has.Length.EqualTo(1));

		var behavior = agentObject.GetComponent<BehaviorParameters>();
		var serializedBehavior = new SerializedObject(behavior);
		serializedBehavior.Update();
		var brain = serializedBehavior.FindProperty("m_BrainParameters");
		Assert.That(brain.FindPropertyRelative("VectorObservationSize").intValue, Is.EqualTo(TowerDefenceAgent.ObservationSize));
		var branchSizes = brain.FindPropertyRelative("m_ActionSpec").FindPropertyRelative("BranchSizes");
		Assert.That(branchSizes.arraySize, Is.EqualTo(TowerDefenceAgent.ActionBranchCount));
		Assert.That(branchSizes.GetArrayElementAtIndex(0).intValue, Is.EqualTo(TowerDefenceAgent.ActionBranchSize));
		Assert.That(branchSizes.GetArrayElementAtIndex(1).intValue, Is.EqualTo(TowerDefenceAgent.TowerBranchSize));
		Assert.That(branchSizes.GetArrayElementAtIndex(2).intValue, Is.EqualTo(TowerDefenceAgent.PlacementBranchSize));
		Assert.That(branchSizes.GetArrayElementAtIndex(3).intValue, Is.EqualTo(TowerDefenceAgent.TileOptionBranchSize));
		Assert.That(branchSizes.GetArrayElementAtIndex(4).intValue, Is.EqualTo(TowerDefenceAgent.UpgradeTargetBranchSize));
		var playerAgents = Object.FindObjectsByType<TowerDefenceAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		Assert.That(playerAgents, Is.Not.Empty);
		for (var i = 0; i < playerAgents.Length; i++)
		{
			var playerBehavior = playerAgents[i].GetComponent<BehaviorParameters>();
			Assert.That(playerBehavior, Is.Not.Null);
			var playerSerializedBehavior = new SerializedObject(playerBehavior);
			playerSerializedBehavior.Update();
			Assert.That(playerSerializedBehavior.FindProperty("m_BrainParameters").FindPropertyRelative("VectorObservationSize").intValue,
				Is.EqualTo(TowerDefenceAgent.ObservationSize));
			var playerBranchSizes = playerSerializedBehavior.FindProperty("m_BrainParameters").FindPropertyRelative("m_ActionSpec").FindPropertyRelative("BranchSizes");
			Assert.That(playerBranchSizes.arraySize, Is.EqualTo(TowerDefenceAgent.ActionBranchCount));
			for (var branchIndex = 0; branchIndex < TowerDefenceAgent.ActionBranchCount; branchIndex++)
				Assert.That(playerBranchSizes.GetArrayElementAtIndex(branchIndex).intValue,
					Is.EqualTo(branchIndex switch
					{
						0 => TowerDefenceAgent.ActionBranchSize,
						1 => TowerDefenceAgent.TowerBranchSize,
						2 => TowerDefenceAgent.PlacementBranchSize,
						3 => TowerDefenceAgent.TileOptionBranchSize,
						_ => TowerDefenceAgent.UpgradeTargetBranchSize
					}));
		}

		var balanceObject = GameObject.Find("TD ML Balance Agent");
		Assert.That(balanceObject, Is.Not.Null, "Run TD/ML-Agents/Setup Gameplay Agent first.");
		Assert.That(balanceObject.GetComponent<TowerDefenceBalancerAgent>(), Is.Not.Null);
		Assert.That(balanceObject.GetComponent<BehaviorParameters>(), Is.Not.Null);
		Assert.That(balanceObject.GetComponent<DecisionRequester>(), Is.Not.Null);
		var serializedBalanceAgent = new SerializedObject(balanceObject.GetComponent<TowerDefenceBalancerAgent>());
		serializedBalanceAgent.Update();
		Assert.That(serializedBalanceAgent.FindProperty("_trainingMode").boolValue, Is.False);

		var balanceBehavior = balanceObject.GetComponent<BehaviorParameters>();
		var serializedBalanceBehavior = new SerializedObject(balanceBehavior);
		serializedBalanceBehavior.Update();
		var balanceBrain = serializedBalanceBehavior.FindProperty("m_BrainParameters");
		Assert.That(balanceBrain.FindPropertyRelative("VectorObservationSize").intValue, Is.EqualTo(TowerDefenceBalancerAgent.ObservationSize));
		var balanceBranchSizes = balanceBrain.FindPropertyRelative("m_ActionSpec").FindPropertyRelative("BranchSizes");
		Assert.That(balanceBranchSizes.arraySize, Is.EqualTo(TowerDefenceBalancerAgent.ActionBranchCount));
		for (var i = 0; i < TowerDefenceBalancerAgent.ActionBranchCount; i++)
			Assert.That(balanceBranchSizes.GetArrayElementAtIndex(i).intValue, Is.EqualTo(TowerDefenceBalancerAgent.ActionBranchSize));

		var enemyLevelObject = GameObject.Find("TD ML Enemy Level Agent");
		Assert.That(enemyLevelObject, Is.Not.Null, "Run TD/ML-Agents/Setup Gameplay Agent first.");
		Assert.That(enemyLevelObject.GetComponent<TowerDefenceEnemyLevelAgent>(), Is.Not.Null);
		Assert.That(enemyLevelObject.GetComponent<BehaviorParameters>(), Is.Not.Null);
		Assert.That(enemyLevelObject.GetComponent<DecisionRequester>(), Is.Not.Null);
		var serializedEnemyLevelAgent = new SerializedObject(enemyLevelObject.GetComponent<TowerDefenceEnemyLevelAgent>());
		serializedEnemyLevelAgent.Update();
		Assert.That(serializedEnemyLevelAgent.FindProperty("_trainingMode").boolValue, Is.False);
		var enemyLevelBehavior = enemyLevelObject.GetComponent<BehaviorParameters>();
		var serializedEnemyLevelBehavior = new SerializedObject(enemyLevelBehavior);
		serializedEnemyLevelBehavior.Update();
		var enemyLevelBrain = serializedEnemyLevelBehavior.FindProperty("m_BrainParameters");
		Assert.That(enemyLevelBrain.FindPropertyRelative("VectorObservationSize").intValue, Is.EqualTo(TowerDefenceEnemyLevelAgent.ObservationSize));
		var enemyLevelBranchSizes = enemyLevelBrain.FindPropertyRelative("m_ActionSpec").FindPropertyRelative("BranchSizes");
		Assert.That(enemyLevelBranchSizes.arraySize, Is.EqualTo(TowerDefenceEnemyLevelAgent.ActionBranchCount));
		for (var i = 0; i < 3; i++)
			Assert.That(enemyLevelBranchSizes.GetArrayElementAtIndex(i).intValue, Is.EqualTo(TowerDefenceEnemyLevelAgent.ActionBranchSize));
		Assert.That(enemyLevelBranchSizes.GetArrayElementAtIndex(3).intValue, Is.EqualTo(TowerDefenceEnemyLevelAgent.SeedBranchSize));
		for (var i = 4; i <= 6; i++)
			Assert.That(enemyLevelBranchSizes.GetArrayElementAtIndex(i).intValue, Is.EqualTo(TowerDefenceEnemyLevelAgent.ArchetypeBranchSize));
		for (var i = 7; i <= 9; i++)
			Assert.That(enemyLevelBranchSizes.GetArrayElementAtIndex(i).intValue, Is.EqualTo(TowerDefenceEnemyLevelAgent.CountBranchSize));
		Assert.That(enemyLevelBranchSizes.GetArrayElementAtIndex(10).intValue, Is.EqualTo(TowerDefenceEnemyLevelAgent.PacingBranchSize));
		}

		private static TilePlacementChoice CreateTileChoice(string name, int openRoadEndCountAfter)
		{
			var openRoadEndsAfter = new List<Vector2Int>();
			for (var i = 0; i < openRoadEndCountAfter; i++)
				openRoadEndsAfter.Add(new Vector2Int(i, 0));

			return new TilePlacementChoice(
				true,
				string.Empty,
				new RoadTileDef
				{
					name = name,
					connections = RoadConnections.North | RoadConnections.South
				},
				null,
				Vector2Int.zero,
				0,
				RoadConnections.North | RoadConnections.South,
				1,
				new List<Vector2Int>(),
				openRoadEndsAfter,
				new List<Vector2Int>());
		}
	}
