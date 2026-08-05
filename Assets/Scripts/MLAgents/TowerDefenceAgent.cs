using System;
using System.Collections.Generic;
using TD.GameLoop;
using TD.Interactions;
using TD.Levels;
using TD.Towers;
using TD.Weapons;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace TD.MLAgents
{
	public class TowerDefenceAgent : Agent
	{
		public const int ObservationSize = 72;
		public const int ActionBranchCount = 5;
		public const int ActionBranchSize = 9;
		public const int TowerBranchSize = 4;
		public const int PlacementBranchSize = 8;
		public const int TileOptionBranchSize = 3;
		public const int UpgradeTargetBranchSize = 8;
		public const int MaxTowerPrefabs = 4;
		public const int MaxPlacementSlots = 8;
		public const float DefaultMlTestTimeScale = 5f;
		public const float MinimumMlTestTimeScale = 0.1f;
		public const float MaximumMlTestTimeScale = 100f;
		public const float DefaultEpisodeTimeLimitSeconds = 180f;
		public const float MinimumEpisodeTimeLimitSeconds = 30f;
		public const float TargetAcquiredReward = 0.002f;
		public const float TowerFiredReward = 0.0005f;
		public const float EnemyKilledReward = 0.01f;
		public const float EnemyLeakedPenalty = -0.02f;
		public const float PlacementBaseReward = 0.002f;
		public const float PlacementCoverageRewardPerRatio = 0.25f;
		public const float PlacementConcentrationRewardPerRatio = 0.01f;
		public const float PlacementWithoutCoveragePenalty = -0.02f;
		private const int MaxStateValues = 9;
		private const int MaxTelemetryTowers = 2;
		private const int MaxTelemetryEventsPerDecision = 128;
		private const int ActionNoOp = 0;
		private const int ActionStartWave = 1;
		private const int ActionResourceCache = 2;
		private const int ActionEmergencyRepairs = 3;
		private const int ActionBountyContract = 4;
		private const int ActionPlaceTower = 5;
		private const int ActionSelectTile = 6;
		private const int ActionCommitTile = 7;
		private const int ActionUpgradeTower = 8;
		private const float OpeningReserveGuardPenalty = 1500f;
		private const int OpeningAreaCounterEnemyCountThreshold = 7;
		private const float CoverageScoreWeight = 600f;
		private const float CombatPowerWeight = 150f;

		[SerializeField] private GameManager _gameManager;
		[SerializeField] private WaveManager _waveManager;
		[SerializeField] private ResourceManager _resourceManager;
		[SerializeField] private GameplayTelemetry _gameplayTelemetry;
		[SerializeField] private PlayerBase _playerBase;
		[SerializeField] private TowerPlacementSystem _towerPlacementSystem;
		[SerializeField] private TilePlacementSystem _tilePlacementSystem;
		[SerializeField] private TileMapManager _tileMapManager;
		[SerializeField] private Camera _gameplayCamera;
		[SerializeField] private SyntheticMouse _syntheticMouse;
		[SerializeField] private List<Tower> _towerPrefabs = new List<Tower>();
		[SerializeField] private bool _trainingMode = true;
		[SerializeField] private bool _restartSceneOnEpisodeReset = true;
		[SerializeField] private bool _applyMlTestTimeScale = true;
		[SerializeField, Min(MinimumMlTestTimeScale)] private float _mlTestTimeScale = DefaultMlTestTimeScale;
		[SerializeField, Min(MinimumEpisodeTimeLimitSeconds)] private float _episodeTimeLimitSeconds = DefaultEpisodeTimeLimitSeconds;

		private readonly Vector3[] _placementSlots = new Vector3[MaxPlacementSlots];
		private readonly bool[] _placementSlotValid = new bool[MaxPlacementSlots];
		private readonly bool[] _reservedPlacementSlots = new bool[MaxPlacementSlots];
		private int _placementSlotsFrame = -1;
		private bool _placementOwnerRejected;
		private bool _coverageHoldLogged;
		private bool _coverageBypassLogged;
		private bool _episodeStarted;
		private bool _episodeFinished;
		private int _lastBaseHealth = -1;
		private int _lastTelemetrySequence;
		private float _episodeStartTime;
		private bool _subscribed;

		public float MlTestTimeScale
		{
			get => _mlTestTimeScale;
			set => SetMlTestTimeScale(value);
		}

		public bool ApplyMlTestTimeScale
		{
			get => _applyMlTestTimeScale;
			set
			{
				_applyMlTestTimeScale = value;
				ApplyConfiguredMlTestTimeScale();
			}
		}

		public float EpisodeTimeLimitSeconds
		{
			get => _episodeTimeLimitSeconds;
			set => _episodeTimeLimitSeconds = Mathf.Max(MinimumEpisodeTimeLimitSeconds, value);
		}

		public bool TrainingMode
		{
			get => _trainingMode;
			set
			{
				_trainingMode = value;
				if (_syntheticMouse != null)
					_syntheticMouse.gameObject.SetActive(value);
			}
		}

		public bool RestartSceneOnEpisodeReset
		{
			get => _restartSceneOnEpisodeReset;
			set => _restartSceneOnEpisodeReset = value;
		}

		private void Start()
		{
			ApplyConfiguredMlTestTimeScale();

			if (_trainingMode && _syntheticMouse != null && !_syntheticMouse.gameObject.activeSelf)
			{
				_syntheticMouse.gameObject.SetActive(true);
			}

			SubscribeToGameplayEvents();
			_lastTelemetrySequence = _gameplayTelemetry != null ? _gameplayTelemetry.LastSequence : 0;
		}

		private void OnDestroy()
		{
			UnsubscribeFromGameplayEvents();
		}

		public override void OnEpisodeBegin()
		{
			ApplyConfiguredMlTestTimeScale();
			ResetLocalEpisodeState();

			if (!_episodeStarted)
			{
				_episodeStarted = true;
				return;
			}

			if (_trainingMode && _restartSceneOnEpisodeReset && _gameManager != null)
			{
				_gameManager.RestartGame();
			}
		}

		public void SetMlTestTimeScale(float value)
		{
			_mlTestTimeScale = Mathf.Clamp(value, MinimumMlTestTimeScale, MaximumMlTestTimeScale);
			ApplyConfiguredMlTestTimeScale();
		}

		public override void CollectObservations(VectorSensor sensor)
		{
			var snapshot = CaptureSnapshot();
			var stateIndex = GetStateIndex(snapshot.GameState);
			sensor.AddOneHotObservation(stateIndex, MaxStateValues);

			sensor.AddObservation(Normalize(snapshot.WaveNumber, Mathf.Max(1, snapshot.TotalWaves)));
			sensor.AddObservation(Normalize(snapshot.TotalWaves, 20f));
			sensor.AddObservation(snapshot.IsSpawning ? 1f : 0f);
			sensor.AddObservation(Normalize(snapshot.EnemiesAlive, 20f));
			sensor.AddObservation(Normalize(snapshot.EnemiesSpawned, Mathf.Max(1, snapshot.TotalEnemiesInWave)));
			sensor.AddObservation(Mathf.Clamp01(snapshot.WaveProgress));
			sensor.AddObservation(Normalize(snapshot.Currency, 200f));
			var availableCurrency = Mathf.Max(1, snapshot.StartingCurrency + snapshot.CurrencyGained);
			sensor.AddObservation(Mathf.Clamp01(Mathf.Max(0, snapshot.Currency) / (float)availableCurrency));
			sensor.AddObservation(Normalize(snapshot.TowersUpgraded, 10f));
			sensor.AddObservation(snapshot.BaseMaxHealth > 0 ? Mathf.Clamp01((float)snapshot.BaseHealth / snapshot.BaseMaxHealth) : 0f);
			sensor.AddObservation(snapshot.RewardOfferPending ? 1f : 0f);
			sensor.AddObservation(snapshot.CanSelectChallengeModifier ? 1f : 0f);
			sensor.AddObservation(snapshot.IsTowerPlacing ? 1f : 0f);
			sensor.AddObservation(snapshot.IsTilePlacing ? 1f : 0f);
			sensor.AddObservation(Mathf.InverseLerp(WaveManager.AdaptiveEnemyFactorMinimum, WaveManager.AdaptiveEnemyFactorMaximum, snapshot.AdaptiveEnemyHealthFactor));
			sensor.AddObservation(Mathf.InverseLerp(WaveManager.AdaptiveEnemyFactorMinimum, WaveManager.AdaptiveEnemyFactorMaximum, snapshot.AdaptiveEnemyCountFactor));
			sensor.AddObservation(Mathf.InverseLerp(WaveManager.AdaptiveSpeedFactorMinimum, WaveManager.AdaptiveSpeedFactorMaximum, snapshot.AdaptiveEnemySpeedFactor));
			sensor.AddObservation(Mathf.InverseLerp(WaveManager.AdaptiveRewardFactorMinimum, WaveManager.AdaptiveRewardFactorMaximum, snapshot.AdaptiveRewardFactor));
			sensor.AddObservation(snapshot.AdaptiveDifficultyScore);
			sensor.AddObservation(snapshot.EntryCoverageRatio);
			sensor.AddObservation(Normalize(snapshot.CoveredEntrances, 8f));
			sensor.AddObservation(snapshot.TowerBaseConcentration);
			sensor.AddObservation(Normalize(snapshot.TotalEntrances, 8f));

			RefreshPlacementSlots();
			for (var i = 0; i < MaxPlacementSlots; i++)
			{
				var relative = _placementSlots[i] - _tileMapManager.BasePosition;
				sensor.AddObservation(Mathf.Clamp(relative.x / 25f, -1f, 1f));
				sensor.AddObservation(Mathf.Clamp(relative.z / 25f, -1f, 1f));
				sensor.AddObservation(_placementSlotValid[i] && !_reservedPlacementSlots[i] ? 1f : 0f);
			}

			for (var i = 0; i < MaxTowerPrefabs; i++)
			{
				var tower = GetTowerPrefab(i);
				sensor.AddObservation(tower != null && tower.Stats != null && tower.Stats.statsSO != null
					? Normalize(tower.Stats.statsSO.Cost, 200f)
					: 0f);
				sensor.AddObservation(tower != null ? 1f : 0f);
			}

			for (var i = 0; i < MaxTelemetryTowers; i++)
			{
				if (snapshot.Towers != null && i < snapshot.Towers.Count && snapshot.Towers[i] != null)
				{
					var tower = snapshot.Towers[i];
					sensor.AddObservation(Normalize(tower.Level, 10f));
					sensor.AddObservation(Normalize(tower.Damage, 100f));
					sensor.AddObservation(Normalize(tower.Range, 25f));
					sensor.AddObservation(tower.HasTarget ? 1f : 0f);
				}
				else
				{
					sensor.AddObservation(0f);
					sensor.AddObservation(0f);
					sensor.AddObservation(0f);
					sensor.AddObservation(0f);
				}
			}
		}

		public override void OnActionReceived(ActionBuffers actions)
		{
			if (!_trainingMode || actions.DiscreteActions.Length < ActionBranchCount)
			{
				return;
			}

			ProcessTelemetryEvents();

			if (TryFinishTimedOutEpisode())
			{
				return;
			}

			if (_waveManager != null && _waveManager.CanSelectChallengeModifier)
			{
				_waveManager.SelectChallengeModifier(GetAutomaticChallengeModifier());
				AddReward(0.01f);
				return;
			}

			var action = Mathf.Clamp(actions.DiscreteActions[0], 0, ActionBranchSize - 1);
			var towerIndex = Mathf.Clamp(actions.DiscreteActions[1], 0, TowerBranchSize - 1);
			var placementIndex = Mathf.Clamp(actions.DiscreteActions[2], 0, PlacementBranchSize - 1);
			var tileOptionIndex = Mathf.Clamp(actions.DiscreteActions[3], 0, TileOptionBranchSize - 1);
			var upgradeTargetIndex = Mathf.Clamp(actions.DiscreteActions[4], 0, UpgradeTargetBranchSize - 1);

			if (_towerPlacementSystem != null && _towerPlacementSystem.IsPlacing && action != ActionPlaceTower)
			{
				_towerPlacementSystem.CancelPlacement();
				AddReward(-0.01f);
				return;
			}

			if (_tilePlacementSystem != null && _tilePlacementSystem.IsPlacing &&
				action != ActionSelectTile && action != ActionCommitTile)
			{
				_tilePlacementSystem.CancelPlacement();
				AddReward(-0.01f);
				return;
			}

			switch (action)
			{
				case ActionNoOp:
					return;
				case ActionStartWave:
					TryStartWave();
					return;
				case ActionResourceCache:
				case ActionEmergencyRepairs:
				case ActionBountyContract:
					TrySelectReward(action - ActionResourceCache);
					return;
				case ActionPlaceTower:
					TryPlaceTower(towerIndex, placementIndex);
					return;
				case ActionSelectTile:
					TrySelectTile(tileOptionIndex);
					return;
				case ActionCommitTile:
					TryCommitTile();
					return;
				case ActionUpgradeTower:
					TryUpgradeTower(upgradeTargetIndex);
					return;
				default:
					AddReward(-0.005f);
					return;
			}
		}

		public override void Heuristic(in ActionBuffers actionsOut)
		{
			var actions = actionsOut.DiscreteActions;
			for (var i = 0; i < actions.Length; i++)
			{
				actions[i] = 0;
			}

			if (!_trainingMode || actions.Length < ActionBranchCount || _gameManager == null || _waveManager == null)
			{
				return;
			}

			if (_waveManager.CanSelectChallengeModifier)
			{
				actions[0] = ActionStartWave;
				return;
			}

			if (_waveManager.IsRewardOfferPending)
			{
				var rewardSnapshot = CaptureSnapshot();
				var shouldPrioritizeRepairs = ShouldPrioritizeEmergencyRepairs(
					rewardSnapshot.BaseHealth,
					rewardSnapshot.BaseMaxHealth,
					rewardSnapshot.Currency,
					rewardSnapshot.Towers != null ? rewardSnapshot.Towers.Count : 0,
					GetCheapestTowerCost());
				var shouldPrioritizeBounty = !shouldPrioritizeRepairs && ShouldPrioritizeBountyContract(
					_waveManager.CurrentWaveNumber,
					_waveManager.TotalWaves,
					rewardSnapshot.BaseHealth,
					rewardSnapshot.BaseMaxHealth,
					rewardSnapshot.Currency,
					rewardSnapshot.Towers != null ? rewardSnapshot.Towers.Count : 0,
					GetCheapestTowerCost(),
					rewardSnapshot.EntryCoverageRatio);
				actions[0] = shouldPrioritizeRepairs
					? ActionEmergencyRepairs
					: shouldPrioritizeBounty ? ActionBountyContract : ActionResourceCache;
				return;
			}

			if (_tilePlacementSystem != null && _tilePlacementSystem.IsPlacing)
			{
				var bestTileOption = FindBestTileOption();
				if (bestTileOption >= 0 && _tilePlacementSystem.SelectedChoiceIndex != bestTileOption)
				{
					actions[0] = ActionSelectTile;
					actions[3] = bestTileOption;
					return;
				}

				actions[0] = ActionCommitTile;
				return;
			}

			if (_gameManager.CurrentState != GameState.Preparation)
			{
				return;
			}

			var snapshot = CaptureSnapshot();
			var coverageTowerIndex = -1;
			var coveragePlacementIndex = -1;
			var hasCoveragePlacement = !_placementOwnerRejected &&
				TryFindCoveragePlacement(snapshot, out coverageTowerIndex, out coveragePlacementIndex);
			var shouldHoldForCoverage = ShouldHoldForCoverage(
				snapshot.TotalEntrances,
				snapshot.CoveredEntrances,
				snapshot.Currency >= GetCheapestTowerCost(),
				hasCoveragePlacement);
			var reinforcementTowerIndex = -1;
			var reinforcementPlacementIndex = -1;
			var hasReinforcementPlacement = !_placementOwnerRejected &&
				TryFindReinforcementPlacement(snapshot, out reinforcementTowerIndex, out reinforcementPlacementIndex);
			var prioritizedPlacement = ShouldPrioritizePlacement(
				_placementOwnerRejected,
				hasCoveragePlacement,
				hasReinforcementPlacement);
			var towerIndex = hasCoveragePlacement ? coverageTowerIndex : reinforcementTowerIndex;
			var placementIndex = hasCoveragePlacement ? coveragePlacementIndex : reinforcementPlacementIndex;
			if (prioritizedPlacement)
			{
				actions[0] = ActionPlaceTower;
				actions[1] = towerIndex;
				actions[2] = placementIndex;
				return;
			}

			if (_towerPlacementSystem != null && _towerPlacementSystem.IsPlacing)
			{
				return;
			}

			var hasAffordableUpgrade = TryFindAffordableUpgrade(snapshot.Currency, out var upgradeTargetIndex);
			if (ShouldPrioritizeUpgrade(prioritizedPlacement, hasAffordableUpgrade))
			{
				actions[0] = ActionUpgradeTower;
				actions[4] = upgradeTargetIndex;
				return;
			}

			if (shouldHoldForCoverage)
			{
				LogCoveragePreparationHold(snapshot);
				actions[0] = ActionNoOp;
				return;
			}

			if (ShouldForceCoveragePlacement(
				snapshot.TotalEntrances,
				snapshot.CoveredEntrances,
				snapshot.Currency >= GetCheapestTowerCost()))
				LogCoveragePreparationBypass(snapshot);

			_coverageHoldLogged = false;
			actions[0] = ActionStartWave;
		}

		public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
		{
			if (!_trainingMode || _waveManager == null || _gameManager == null)
				return;

			if (_waveManager.CanSelectChallengeModifier)
			{
				MaskOnlyPrimaryAction(actionMask, ActionStartWave);
				MaskOnlyActionBranch(actionMask, 4, 0, UpgradeTargetBranchSize);
				return;
			}

			if (_waveManager.IsRewardOfferPending)
			{
				for (var action = 0; action < ActionBranchSize; action++)
				{
					var isRewardChoice = action >= ActionResourceCache && action <= ActionBountyContract;
					actionMask.SetActionEnabled(0, action, isRewardChoice);
				}
				return;
			}

			if (_tilePlacementSystem != null && _tilePlacementSystem.IsPlacing)
			{
				for (var action = 0; action < ActionBranchSize; action++)
					actionMask.SetActionEnabled(0, action, action == ActionSelectTile || action == ActionCommitTile);

				var choices = _tilePlacementSystem.PlacementChoices;
				var hasTileChoice = choices != null && choices.Count > 0;
				for (var option = 0; option < TileOptionBranchSize; option++)
					actionMask.SetActionEnabled(3, option, hasTileChoice && option < choices.Count);
				if (!hasTileChoice)
					actionMask.SetActionEnabled(3, 0, true);
				MaskOnlyActionBranch(actionMask, 4, 0, UpgradeTargetBranchSize);

				actionMask.SetActionEnabled(0, ActionCommitTile, _tilePlacementSystem.HasSelectedChoice);
				return;
			}

			if (_gameManager.CurrentState != GameState.Preparation)
			{
				MaskOnlyPrimaryAction(actionMask, ActionNoOp);
				MaskOnlyActionBranch(actionMask, 4, 0, UpgradeTargetBranchSize);
				return;
			}

			RefreshPlacementSlots();
			var snapshot = CaptureSnapshot();
			var hasAffordableTower = false;
			for (var towerIndex = 0; towerIndex < TowerBranchSize; towerIndex++)
			{
				var tower = GetTowerPrefab(towerIndex);
				var affordable = tower != null && tower.Stats != null && tower.Stats.statsSO != null &&
					tower.Stats.statsSO.Cost <= snapshot.Currency;
				actionMask.SetActionEnabled(1, towerIndex, affordable);
				hasAffordableTower |= affordable;
			}
			if (!hasAffordableTower)
				actionMask.SetActionEnabled(1, 0, true);

			var hasUsablePlacementSlot = false;
			for (var placementIndex = 0; placementIndex < PlacementBranchSize; placementIndex++)
			{
				var usable = _placementSlotValid[placementIndex] && !_reservedPlacementSlots[placementIndex] &&
					TryGetPlacementScreenPosition(placementIndex, out _);
				actionMask.SetActionEnabled(2, placementIndex, usable);
				hasUsablePlacementSlot |= usable;
			}
			if (!hasUsablePlacementSlot)
				actionMask.SetActionEnabled(2, 0, true);

			var hasUpgradeableTower = false;
			var orderedTowers = GetOrderedTowers();
			for (var targetIndex = 0; targetIndex < UpgradeTargetBranchSize; targetIndex++)
			{
				var canUpgrade = targetIndex < orderedTowers.Length && CanAffordUpgrade(orderedTowers[targetIndex], snapshot.Currency);
				actionMask.SetActionEnabled(4, targetIndex, canUpgrade);
				hasUpgradeableTower |= canUpgrade;
			}
			if (!hasUpgradeableTower)
				actionMask.SetActionEnabled(4, 0, true);

			var hasAffordablePlacement = !_placementOwnerRejected && hasAffordableTower && hasUsablePlacementSlot;
			var coverageTowerIndex = -1;
			var coveragePlacementIndex = -1;
			var shouldPrioritizeCoverage = !_placementOwnerRejected &&
				TryFindCoveragePlacement(snapshot, out coverageTowerIndex, out coveragePlacementIndex);
			var reinforcementTowerIndex = -1;
			var reinforcementPlacementIndex = -1;
			var hasReinforcementPlacement = !_placementOwnerRejected &&
				TryFindReinforcementPlacement(snapshot, out reinforcementTowerIndex, out reinforcementPlacementIndex);
			var shouldPrioritizePlacement = ShouldPrioritizePlacement(
				_placementOwnerRejected,
				shouldPrioritizeCoverage,
				hasReinforcementPlacement);
			var prioritizedTowerIndex = shouldPrioritizeCoverage ? coverageTowerIndex : reinforcementTowerIndex;
			var prioritizedPlacementIndex = shouldPrioritizeCoverage ? coveragePlacementIndex : reinforcementPlacementIndex;

			for (var action = 0; action < ActionBranchSize; action++)
			{
				var isPreparationAction = action == ActionNoOp || action == ActionStartWave ||
					action == ActionPlaceTower || action == ActionUpgradeTower;
				actionMask.SetActionEnabled(0, action, isPreparationAction);
			}

			if (shouldPrioritizePlacement)
			{
				MaskOnlyPrimaryAction(actionMask, ActionPlaceTower);
				MaskOnlyActionBranch(actionMask, 1, prioritizedTowerIndex, TowerBranchSize);
				MaskOnlyActionBranch(actionMask, 2, prioritizedPlacementIndex, PlacementBranchSize);
			}
			else
			{
				actionMask.SetActionEnabled(0, ActionPlaceTower, hasAffordablePlacement);
				actionMask.SetActionEnabled(0, ActionStartWave, !shouldPrioritizeCoverage);
				actionMask.SetActionEnabled(0, ActionUpgradeTower, hasUpgradeableTower);
			}
		}

		private void MaskOnlyPrimaryAction(IDiscreteActionMask actionMask, int allowedAction)
		{
			for (var action = 0; action < ActionBranchSize; action++)
				actionMask.SetActionEnabled(0, action, action == allowedAction);
		}

		private static void MaskOnlyActionBranch(IDiscreteActionMask actionMask, int branch, int allowedAction, int branchSize)
		{
			for (var action = 0; action < branchSize; action++)
				actionMask.SetActionEnabled(branch, action, action == allowedAction);
		}

		private void TryStartWave()
		{
			if (_waveManager != null && _waveManager.CanSelectChallengeModifier)
			{
				_waveManager.SelectChallengeModifier(GetAutomaticChallengeModifier());
				AddReward(0.01f);
				return;
			}

			if (_gameManager != null && _gameManager.CurrentState == GameState.Preparation && _waveManager != null && !_waveManager.IsRewardOfferPending)
			{
				var snapshot = CaptureSnapshot();
				if (!_placementOwnerRejected)
				{
					var hasCoveragePlacement = TryFindCoveragePlacement(snapshot, out _, out _);
					var shouldHoldForCoverage = ShouldHoldForCoverage(
						snapshot.TotalEntrances,
						snapshot.CoveredEntrances,
						snapshot.Currency >= GetCheapestTowerCost(),
						hasCoveragePlacement);
					if (shouldHoldForCoverage || TryFindReinforcementPlacement(snapshot, out _, out _))
					{
						AddReward(-0.03f);
						return;
					}

					if (ShouldForceCoveragePlacement(
						snapshot.TotalEntrances,
						snapshot.CoveredEntrances,
						snapshot.Currency >= GetCheapestTowerCost()))
						LogCoveragePreparationBypass(snapshot);
				}

				_gameManager.StartNextWave();
				_coverageBypassLogged = false;
				AddReward(0.002f + snapshot.EntryCoverageRatio * 0.01f);
				return;
			}

			AddReward(-0.005f);
		}

		private void TrySelectReward(int choiceIndex)
		{
			if (_waveManager == null || !_waveManager.IsRewardOfferPending)
			{
				AddReward(-0.005f);
				return;
			}

			var before = CaptureSnapshot();
			if (!_waveManager.SelectRewardOffer(_waveManager.RewardOfferId, choiceIndex))
			{
				AddReward(-0.005f);
				return;
			}

			AddReward(0.003f);
			Debug.Log(
				$"[MLAgent] Reward decision={(RewardOfferChoice)choiceIndex};" +
				$"wave={_waveManager.CurrentWaveNumber}/{_waveManager.TotalWaves};" +
				$"base={before.BaseHealth}/{before.BaseMaxHealth};currency={before.Currency};" +
				$"towers={(before.Towers != null ? before.Towers.Count : 0)};" +
				$"coverage={before.CoveredEntrances}/{before.TotalEntrances}");
		}

		private void TryPlaceTower(int towerIndex, int placementIndex)
		{
			if (_gameManager == null || _gameManager.CurrentState != GameState.Preparation || _towerPlacementSystem == null || _gameplayCamera == null)
			{
				AddReward(-0.005f);
				return;
			}

			if (_placementOwnerRejected)
			{
				AddReward(-0.005f);
				return;
			}

			var tower = GetTowerPrefab(towerIndex);
			if (tower == null)
			{
				AddReward(-0.005f);
				return;
			}

			var before = CaptureSnapshot();
			var openingDefense = _waveManager.WavesCompleted == 0 && GetOrderedTowers().Length == 0;
			var requireCoverage = before.EntryCoverageRatio < 0.999f &&
				TryFindCoveragePlacement(before, out _, out _);
			var placementIntent = requireCoverage ? "coverage" : "reinforcement";
			var placementReason = GetPlacementReason(before, tower, requireCoverage);
			var attemptedPlacements = 0;

			for (var attempt = 0; attempt < MaxPlacementSlots; attempt++)
			{
				var screenPosition = default(Vector2);
				var hasScreenPosition = placementIndex >= 0 && placementIndex < MaxPlacementSlots &&
					TryGetPlacementScreenPosition(placementIndex, out screenPosition);
				if (!hasScreenPosition)
				{
					if (placementIndex >= 0 && placementIndex < MaxPlacementSlots)
						_reservedPlacementSlots[placementIndex] = true;
				}
				else
				{
					attemptedPlacements++;
					_towerPlacementSystem.BeginPlacement(tower.gameObject);
					if (!_towerPlacementSystem.IsPlacing)
					{
						AddReward(-0.005f);
						return;
					}

					if (_towerPlacementSystem.TryPlaceTowerAtScreenPosition(screenPosition))
					{
						InvalidatePlacementSlots();
						_reservedPlacementSlots[placementIndex] = true;
						var after = CaptureSnapshot();
						var coverageGain = after.EntryCoverageRatio - before.EntryCoverageRatio;
						var concentrationChange = after.TowerBaseConcentration - before.TowerBaseConcentration;
						var placementReward = GetPlacementReward(coverageGain, concentrationChange);
						AddReward(placementReward);
						var towerCost = tower.Stats != null && tower.Stats.statsSO != null ? tower.Stats.statsSO.Cost : 0;
						var isAreaRole = tower.GetComponent<AoEWeapon>() != null;
						var combatPower = GetPlanningTowerCombatPower(tower, isAreaRole);
						var upcomingEnemyCount = _waveManager != null ? _waveManager.GetUpcomingWaveTotalEnemyCount() : 0;
						var openingAreaCounterEligible = ShouldPreferAreaCounter(
							upcomingEnemyCount, openingDefense, isAreaRole, before.Currency, towerCost);
						Debug.Log(
							$"[MLAgent] Tower decision=index={towerIndex};name={tower.name};cost={towerCost};" +
							$"role={(isAreaRole ? "area" : "single")};" +
							$"currency={before.Currency}->{after.Currency};" +
							$"coverage={before.CoveredEntrances}/{before.TotalEntrances}->" +
							$"{after.CoveredEntrances}/{after.TotalEntrances};" +
							$"placementIntent={placementIntent};" +
							$"placementReason={placementReason};" +
							$"coverageScoreMode={(requireCoverage ? "entrance-first" : "route-first")};" +
							$"openingDefense={openingDefense};" +
							$"upcomingEnemyCount={upcomingEnemyCount};" +
							$"openingAreaCounterThreshold={OpeningAreaCounterEnemyCountThreshold};" +
					$"openingAreaRoleEligible={openingDefense && isAreaRole && after.Currency >= GetCheapestTowerCost()};" +
					$"openingAreaCounterEligible={openingAreaCounterEligible};" +
					$"combatPower={combatPower:F2};" +
							$"openingReserveGuard={(openingDefense ? (after.Currency >= GetCheapestTowerCost() ? "preserved" : "spent") : "n/a")};" +
							$"basicReserveAfter={after.Currency >= GetCheapestTowerCost()}");
						if (Application.isPlaying)
						{
							var statsRecorder = Academy.Instance.StatsRecorder;
							statsRecorder.Add("TD3D/Player/PlacementCoverageGain", coverageGain);
							statsRecorder.Add("TD3D/Player/PlacementBaseConcentration", after.TowerBaseConcentration);
							statsRecorder.Add("TD3D/Player/PlacementCoveredNewEntrance", coverageGain > 0f ? 1f : 0f);
							statsRecorder.Add("TD3D/Player/PlacementReward", placementReward);
						}
						return;
					}

					_reservedPlacementSlots[placementIndex] = true;
					_towerPlacementSystem.CancelPlacement();
				}

				if (!TryFindFreePlacementSlot(towerIndex, out placementIndex, requireCoverage))
					break;
			}

			_placementOwnerRejected = true;
			Debug.Log(
				$"[MLAgent] Placement gate=owner-rejected;covered={before.CoveredEntrances}/{before.TotalEntrances};" +
				$"currency={before.Currency};attempts={attemptedPlacements};" +
				$"requireCoverage={requireCoverage};action=next-preparation-policy");
			AddReward(-0.01f);
		}

		private void TryUpgradeTower(int targetIndex)
		{
			if (_gameManager == null || _gameManager.CurrentState != GameState.Preparation)
			{
				AddReward(-0.005f);
				return;
			}

			var before = CaptureSnapshot();
			var orderedTowers = GetOrderedTowers();
			if (targetIndex < 0 || targetIndex >= orderedTowers.Length ||
				!CanAffordUpgrade(orderedTowers[targetIndex], before.Currency))
			{
				AddReward(-0.01f);
				return;
			}

			var tower = orderedTowers[targetIndex];
			var previousGrade = tower.Stats.currentGrade;
			tower.UpgradeSpendingCost();
			if (tower.Stats.currentGrade <= previousGrade)
			{
				AddReward(-0.01f);
				return;
			}

			var after = CaptureSnapshot();
			var upgradeGain = Mathf.Max(1, after.TowersUpgraded - before.TowersUpgraded);
			AddReward(0.01f + upgradeGain * 0.02f);
			var isFinalWavePreparation = _waveManager != null && ShouldPrioritizeFinalWaveUpgrade(
				_waveManager.CurrentWaveNumber,
				_waveManager.TotalWaves,
				true,
				after.EntryCoverageRatio);
			Debug.Log(
				$"[MLAgent] Upgrade committed tower={tower.name};grade={previousGrade}->{tower.Stats.currentGrade};" +
				$"currency={after.Currency};totalUpgrades={after.TowersUpgraded};" +
				$"reason={(isFinalWavePreparation ? "final-wave-upgrade-reserve" : "upgrade")};" +
				$"wave={_waveManager?.CurrentWaveNumber ?? 0}/{_waveManager?.TotalWaves ?? 0}");
		}

		private void TrySelectTile(int tileOptionIndex)
		{
			if (_tilePlacementSystem == null || !_tilePlacementSystem.IsPlacing)
			{
				AddReward(-0.005f);
				return;
			}

			var currentIndex = _tilePlacementSystem.SelectedChoiceIndex;
			var steps = (tileOptionIndex - currentIndex + TileOptionBranchSize) % TileOptionBranchSize;
			for (var i = 0; i < steps; i++)
			{
				_tilePlacementSystem.SelectNextOption();
			}

			if (_tilePlacementSystem.HasSelectedChoice)
			{
				LogTileDecision(_tilePlacementSystem.SelectedChoice, "select");
			}

			AddReward(0.001f);
		}

		private int FindBestTileOption()
		{
			if (_tilePlacementSystem == null || _tileMapManager == null || !_tilePlacementSystem.IsPlacing)
				return -1;

			var choices = _tilePlacementSystem.PlacementChoices;
			if (choices == null || choices.Count == 0)
				return -1;

			var towers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
			var coveredEntrancesBefore = TowerPlacementSystem.CountCoveredEntrances(towers, _tileMapManager.SpawnPositions);
			var totalEntrancesBefore = _tileMapManager.SpawnPositions.Count;
			var coveredEntrancesAfter = new int[choices.Count];
			var totalEntrancesAfter = new int[choices.Count];
			var coveredRouteSamplesAfter = new int[choices.Count];
			var totalRouteSamplesAfter = new int[choices.Count];
			for (var choiceIndex = 0; choiceIndex < choices.Count; choiceIndex++)
			{
				var spawnPositionsAfter = _tileMapManager.GetSpawnPositionsAfter(choices[choiceIndex]);
				totalEntrancesAfter[choiceIndex] = spawnPositionsAfter != null ? spawnPositionsAfter.Count : 0;
				coveredEntrancesAfter[choiceIndex] = TowerPlacementSystem.CountCoveredEntrances(towers, spawnPositionsAfter);
				var routeSamples = TowerPlacementSystem.BuildRouteSamples(
					spawnPositionsAfter,
					_tileMapManager.BasePosition,
					Mathf.Max(0.5f, _tileMapManager.TileSize * 0.25f));
				totalRouteSamplesAfter[choiceIndex] = routeSamples.Count;
				coveredRouteSamplesAfter[choiceIndex] = TowerPlacementSystem.CountCoveredRouteSamples(towers, routeSamples);
			}

			return ChooseBestTileOption(
				choices,
				coveredEntrancesAfter,
				totalEntrancesAfter,
				coveredRouteSamplesAfter,
				totalRouteSamplesAfter,
				coveredEntrancesBefore,
				totalEntrancesBefore);
		}

		private void GetTileRouteCoverage(TilePlacementChoice choice, out int coveredRouteSamples, out int totalRouteSamples)
		{
			coveredRouteSamples = 0;
			totalRouteSamples = 0;
			if (_tileMapManager == null)
				return;

			var spawnPositionsAfter = _tileMapManager.GetSpawnPositionsAfter(choice);
			var routeSamples = TowerPlacementSystem.BuildRouteSamples(
				spawnPositionsAfter,
				_tileMapManager.BasePosition,
				Mathf.Max(0.5f, _tileMapManager.TileSize * 0.25f));
			var towers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
			totalRouteSamples = routeSamples.Count;
			coveredRouteSamples = TowerPlacementSystem.CountCoveredRouteSamples(towers, routeSamples);
		}

		public static int ChooseBestTileOption(
			IReadOnlyList<TilePlacementChoice> choices,
			IReadOnlyList<int> coveredEntrancesAfter,
			IReadOnlyList<int> totalEntrancesAfter,
			IReadOnlyList<int> coveredRouteSamplesAfter = null,
			IReadOnlyList<int> totalRouteSamplesAfter = null,
			int coveredEntrancesBefore = -1,
			int totalEntrancesBefore = -1)
		{
			if (choices == null || coveredEntrancesAfter == null || totalEntrancesAfter == null)
				return -1;

			var count = Mathf.Min(choices.Count, Mathf.Min(coveredEntrancesAfter.Count, totalEntrancesAfter.Count));
			if (coveredRouteSamplesAfter != null && totalRouteSamplesAfter != null)
				count = Mathf.Min(count, Mathf.Min(coveredRouteSamplesAfter.Count, totalRouteSamplesAfter.Count));
			var hasCoverageBaseline = totalEntrancesBefore > 0;
			var coverageBeforeRatio = hasCoverageBaseline
				? Mathf.Clamp01((float)coveredEntrancesBefore / totalEntrancesBefore)
				: 0f;
			var hasCoveragePreservingChoice = false;
			if (hasCoverageBaseline)
			{
				for (var choiceIndex = 0; choiceIndex < count; choiceIndex++)
				{
					if (!choices[choiceIndex].IsValid)
						continue;

					var choiceCoverageRatio = totalEntrancesAfter[choiceIndex] > 0
						? Mathf.Clamp01((float)coveredEntrancesAfter[choiceIndex] / totalEntrancesAfter[choiceIndex])
						: 0f;
					if (choiceCoverageRatio + 0.0001f >= coverageBeforeRatio)
					{
						hasCoveragePreservingChoice = true;
						break;
					}
				}
			}

			var bestIndex = -1;
			var bestScore = float.MinValue;
			for (var choiceIndex = 0; choiceIndex < count; choiceIndex++)
			{
				if (hasCoveragePreservingChoice &&
					GetCoverageRatio(coveredEntrancesAfter[choiceIndex], totalEntrancesAfter[choiceIndex]) + 0.0001f < coverageBeforeRatio)
					continue;

				var routeCovered = coveredRouteSamplesAfter != null ? coveredRouteSamplesAfter[choiceIndex] : 0;
				var routeTotal = totalRouteSamplesAfter != null ? totalRouteSamplesAfter[choiceIndex] : 0;
				var score = GetTileChoiceScore(
					choices[choiceIndex],
					coveredEntrancesAfter[choiceIndex],
					totalEntrancesAfter[choiceIndex],
					routeCovered,
					routeTotal);
				if (score <= bestScore)
					continue;

				bestScore = score;
				bestIndex = choiceIndex;
			}

			return bestIndex;
		}

		private static float GetCoverageRatio(int coveredEntrances, int totalEntrances)
		{
			return totalEntrances > 0 ? Mathf.Clamp01((float)coveredEntrances / totalEntrances) : 0f;
		}

		public static float GetTileChoiceScore(
			TilePlacementChoice choice,
			int coveredEntrancesAfter,
			int totalEntrancesAfter,
			int coveredRouteSamplesAfter = 0,
			int totalRouteSamplesAfter = 0)
		{
			if (!choice.IsValid)
				return float.MinValue;

			var coverageRatio = totalEntrancesAfter > 0
				? Mathf.Clamp01((float)coveredEntrancesAfter / totalEntrancesAfter)
				: 0f;
			var routeCoverageRatio = totalRouteSamplesAfter > 0
				? Mathf.Clamp01((float)coveredRouteSamplesAfter / totalRouteSamplesAfter)
				: coverageRatio;
			var coverageScore = totalRouteSamplesAfter > 0
				? routeCoverageRatio * 800f + coverageRatio * 200f
				: coverageRatio * 1000f;
		const float openRoadEndPenalty = 500f;
		return coverageScore - choice.OpenRoadEndCountAfter * openRoadEndPenalty + choice.ConnectedNeighborCount;
		}

		private void TryCommitTile()
		{
			if (_tilePlacementSystem == null || !_tilePlacementSystem.IsPlacing)
			{
				AddReward(-0.005f);
				return;
			}

			if (_tilePlacementSystem.HasSelectedChoice)
				LogTileDecision(_tilePlacementSystem.SelectedChoice, "commit");

			if (_tilePlacementSystem.TryPlaceSelectedTile())
			{
				InvalidatePlacementSlots();
				AddReward(0.005f);
				return;
			}

			AddReward(-0.01f);
		}

		private void LogTileDecision(TilePlacementChoice selectedChoice, string phase)
		{
			GetTileRouteCoverage(selectedChoice, out var routeCovered, out var routeTotal);
			Debug.Log(
				$"[MLAgent] Tile decision=phase={phase};index={_tilePlacementSystem.SelectedChoiceIndex};" +
				$"name={selectedChoice.TileName};" +
				$"openEnds={selectedChoice.OpenRoadEndCountBefore}->{selectedChoice.OpenRoadEndCountAfter};" +
				$"coverage={_tilePlacementSystem.SelectedChoiceCoveredEntrancesBefore}/" +
				$"{_tilePlacementSystem.SelectedChoiceTotalEntrancesBefore}->" +
				$"{_tilePlacementSystem.SelectedChoiceCoveredEntrancesAfter}/" +
				$"{_tilePlacementSystem.SelectedChoiceTotalEntrancesAfter};" +
				$"routeCoverage={routeCovered}/{routeTotal}");
		}

		private GameplayTelemetrySnapshot CaptureSnapshot()
		{
			return _gameplayTelemetry.CaptureSnapshot();
		}

		public static float GetTelemetryEventReward(string eventName)
		{
			return eventName switch
			{
				"TowerTargetAcquired" => TargetAcquiredReward,
				"TowerFired" => TowerFiredReward,
				"MonsterDeath" => EnemyKilledReward,
				"EnemyLeaked" => EnemyLeakedPenalty,
				_ => 0f
			};
		}

		public static ChallengeModifier GetAutomaticChallengeModifier()
		{
			return ChallengeModifier.ControlledPressure;
		}

		public static float GetPlacementReward(float coverageGain, float concentrationChange)
		{
			var reward = PlacementBaseReward +
				Mathf.Max(0f, coverageGain) * PlacementCoverageRewardPerRatio +
				Mathf.Max(0f, concentrationChange) * PlacementConcentrationRewardPerRatio;
			if (coverageGain <= 0f)
				reward += PlacementWithoutCoveragePenalty;

			return reward;
		}

		private void ProcessTelemetryEvents()
		{
			if (_gameplayTelemetry == null)
				return;

			if (_gameplayTelemetry.LastSequence < _lastTelemetrySequence)
				_lastTelemetrySequence = 0;

			var telemetryEvents = _gameplayTelemetry.GetEventsSince(_lastTelemetrySequence, MaxTelemetryEventsPerDecision);
			foreach (var telemetryEvent in telemetryEvents)
			{
				_lastTelemetrySequence = Mathf.Max(_lastTelemetrySequence, telemetryEvent.Sequence);
				var reward = GetTelemetryEventReward(telemetryEvent.Name);
				if (Mathf.Approximately(reward, 0f))
					continue;

				AddReward(reward);
				if (!Application.isPlaying)
					continue;

				var statsRecorder = Academy.Instance.StatsRecorder;
				statsRecorder.Add("TD3D/Player/CombatReward", reward);
				switch (telemetryEvent.Name)
				{
					case "TowerTargetAcquired":
						statsRecorder.Add("TD3D/Player/TowerTargetAcquired", 1f);
						break;
					case "TowerFired":
						statsRecorder.Add("TD3D/Player/TowerFired", 1f);
						break;
					case "MonsterDeath":
						statsRecorder.Add("TD3D/Player/EnemyKilled", 1f);
						break;
					case "EnemyLeaked":
						statsRecorder.Add("TD3D/Player/EnemyLeaked", 1f);
						break;
				}
			}
		}

		private void RefreshPlacementSlots()
		{
			if (_tileMapManager == null || _placementSlotsFrame == Time.frameCount)
				return;

			_placementSlotsFrame = Time.frameCount;

			var tiles = _tileMapManager.GetAllTiles();
			var candidates = new List<Vector3>();
			var tileOffset = Mathf.Max(1f, _tileMapManager.TileSize * 0.32f);
			foreach (var tile in tiles)
			{
				if (tile.Key == Vector2Int.zero)
					continue;

				var tilePosition = _tileMapManager.GridToWorld(tile.Key);
				AddPlacementCandidate(candidates, tilePosition + new Vector3(-tileOffset, 0f, -tileOffset));
				AddPlacementCandidate(candidates, tilePosition + new Vector3(-tileOffset, 0f, tileOffset));
				AddPlacementCandidate(candidates, tilePosition + new Vector3(tileOffset, 0f, -tileOffset));
				AddPlacementCandidate(candidates, tilePosition + new Vector3(tileOffset, 0f, tileOffset));
			}

			if (_tileMapManager.SpawnPositions != null)
			{
				foreach (var entrance in _tileMapManager.SpawnPositions)
				{
					var tilePosition = _tileMapManager.GridToWorld(_tileMapManager.WorldToGrid(entrance));
					var outward = entrance - tilePosition;
					outward.y = 0f;
					if (outward.sqrMagnitude < 0.01f)
						outward = entrance - _tileMapManager.BasePosition;

					if (outward.sqrMagnitude < 0.01f)
						continue;

					var lateral = Vector3.Cross(Vector3.up, outward.normalized) * tileOffset;
					AddPlacementCandidate(candidates, tilePosition + lateral);
					AddPlacementCandidate(candidates, tilePosition - lateral);
					var entranceInset = Mathf.Max(0.75f, _tileMapManager.TileSize * 0.18f);
					var inward = -outward.normalized * entranceInset;
					AddPlacementCandidate(candidates, entrance + inward + lateral);
					AddPlacementCandidate(candidates, entrance + inward - lateral);
				}
			}

			var selectedCandidates = new List<Vector3>();
			var minimumRange = GetMinimumTowerRange();
			var maximumRange = GetMaximumTowerRange();
			while (selectedCandidates.Count < MaxPlacementSlots && candidates.Count > 0)
			{
				var bestCandidateIndex = -1;
				var bestScore = float.MinValue;
				for (var candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
				{
					var candidate = candidates[candidateIndex];
					var minimumRangeCoverage = minimumRange > 0f
						? CountUncoveredEntrancesWithin(candidate, minimumRange)
						: 0;
					var maximumRangeCoverage = maximumRange > 0f
						? CountUncoveredEntrancesWithin(candidate, maximumRange)
						: 0;
					var entranceProximity = GetUncoveredEntranceProximity(candidate);
					var score = minimumRangeCoverage * 100f + maximumRangeCoverage * 20f +
						entranceProximity * 30f + GetBaseConcentration(candidate) * 10f;
					foreach (var selectedCandidate in selectedCandidates)
						score -= Mathf.Max(0f, 1f - Vector3.Distance(candidate, selectedCandidate)) * 5f;

					if (score <= bestScore)
						continue;

					bestScore = score;
					bestCandidateIndex = candidateIndex;
				}

				if (bestCandidateIndex < 0)
					break;

				selectedCandidates.Add(candidates[bestCandidateIndex]);
				candidates.RemoveAt(bestCandidateIndex);
			}

			for (var i = 0; i < MaxPlacementSlots; i++)
			{
				_reservedPlacementSlots[i] = false;
				if (i < selectedCandidates.Count)
				{
					_placementSlots[i] = selectedCandidates[i];
					_placementSlotValid[i] = true;
				}
				else
				{
					_placementSlots[i] = _tileMapManager.BasePosition;
					_placementSlotValid[i] = false;
				}
			}

			var towers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
			for (var slotIndex = 0; slotIndex < MaxPlacementSlots; slotIndex++)
			{
				if (!_placementSlotValid[slotIndex])
					continue;

				foreach (var tower in towers)
				{
					if (tower != null && Vector3.Distance(tower.transform.position, _placementSlots[slotIndex]) <= 1f)
					{
						_reservedPlacementSlots[slotIndex] = true;
						break;
					}
				}
			}
		}

		private static void AddPlacementCandidate(List<Vector3> candidates, Vector3 candidate)
		{
			foreach (var existingCandidate in candidates)
			{
				if (Vector3.Distance(existingCandidate, candidate) < 0.1f)
					return;
			}

			candidates.Add(candidate);
		}

		private void InvalidatePlacementSlots()
		{
			_placementSlotsFrame = -1;
			_placementOwnerRejected = false;
		}

		private float GetMaximumTowerRange()
		{
			var maximumRange = 0f;
			for (var i = 0; i < MaxTowerPrefabs; i++)
			{
				var tower = GetTowerPrefab(i);
				if (tower != null)
					maximumRange = Mathf.Max(maximumRange, GetPlanningTowerRange(tower));
			}

			return maximumRange;
		}

		private float GetMinimumTowerRange()
		{
			var minimumRange = float.MaxValue;
			for (var i = 0; i < MaxTowerPrefabs; i++)
			{
				var tower = GetTowerPrefab(i);
				if (tower == null)
					continue;

				minimumRange = Mathf.Min(minimumRange, GetPlanningTowerRange(tower));
			}

			return minimumRange == float.MaxValue ? 0f : minimumRange;
		}

		private bool TryGetPlacementScreenPosition(int placementIndex, out Vector2 screenPosition)
		{
			RefreshPlacementSlots();
			if (placementIndex < 0 || placementIndex >= MaxPlacementSlots || !_placementSlotValid[placementIndex] || _reservedPlacementSlots[placementIndex])
			{
				screenPosition = default;
				return false;
			}

			var projected = _gameplayCamera.WorldToScreenPoint(_placementSlots[placementIndex]);
			if (projected.z <= 0f || projected.x < 0f || projected.y < 0f || projected.x > Screen.width || projected.y > Screen.height)
			{
				screenPosition = default;
				return false;
			}

			screenPosition = projected;
			return true;
		}

		private bool TryFindFreePlacementSlot(int towerIndex, out int placementIndex, bool requireCoverage = false)
		{
			RefreshPlacementSlots();
			var tower = GetTowerPrefab(towerIndex);
			var range = GetPlanningTowerRange(tower);
			var routeSamples = _tileMapManager != null
				? TowerPlacementSystem.BuildRouteSamples(
					_tileMapManager.SpawnPositions,
					_tileMapManager.BasePosition,
					Mathf.Max(0.5f, _tileMapManager.TileSize * 0.25f))
				: new List<Vector3>();
			var bestScore = float.MinValue;
			var bestIndex = -1;
			var towers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
			for (var i = 0; i < MaxPlacementSlots; i++)
			{
				if (!_placementSlotValid[i] || _reservedPlacementSlots[i] || !TryGetPlacementScreenPosition(i, out _))
					continue;

				var uncoveredEntrances = CountUncoveredEntrancesWithin(_placementSlots[i], range);
				if (requireCoverage && uncoveredEntrances <= 0)
					continue;

				var baseConcentration = GetBaseConcentration(_placementSlots[i]);
				var coveredRouteSamples = TowerPlacementSystem.CountCoveredRouteSamples(
					towers,
					_placementSlots[i],
					range,
					routeSamples);
				var score = GetPlacementSlotScore(
					uncoveredEntrances,
					coveredRouteSamples,
					routeSamples.Count,
					GetUncoveredEntranceProximity(_placementSlots[i]),
					baseConcentration,
					requireCoverage);
				if (score <= bestScore)
					continue;

				bestScore = score;
				bestIndex = i;
			}

			placementIndex = bestIndex;
			return bestIndex >= 0;
		}

		private static float GetPlanningTowerRange(Tower tower)
		{
			if (tower == null || tower.Stats == null)
				return 0f;

			var configuredRange = tower.Stats.statsSO != null ? tower.Stats.statsSO.Range.BaseValue : 0f;
			return Mathf.Max(tower.EffectiveRange, configuredRange);
		}

		public static float GetPlacementSlotScore(
			int uncoveredEntrances,
			int coveredRouteSamples,
			int totalRouteSamples,
			float uncoveredEntranceProximity,
			float baseConcentration,
			bool prioritizeEntranceCoverage = false)
		{
			var routeScore = totalRouteSamples > 0 ? coveredRouteSamples * 10f : 0f;
			var entranceScoreWeight = prioritizeEntranceCoverage
				? Mathf.Max(1, totalRouteSamples) * 10f + 1f
				: 10f;
			return routeScore + Mathf.Max(0, uncoveredEntrances) * entranceScoreWeight +
				uncoveredEntranceProximity * 3f + baseConcentration;
		}

		private static Tower[] GetOrderedTowers()
		{
			var towers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
			Array.Sort(towers, CompareTowers);
			return towers;
		}

		private static int CompareTowers(Tower left, Tower right)
		{
			if (left == right)
				return 0;
			if (left == null)
				return 1;
			if (right == null)
				return -1;

			var leftPosition = left.transform.position;
			var rightPosition = right.transform.position;
			var comparison = leftPosition.x.CompareTo(rightPosition.x);
			if (comparison != 0)
				return comparison;

			comparison = leftPosition.z.CompareTo(rightPosition.z);
			if (comparison != 0)
				return comparison;

			comparison = string.CompareOrdinal(left.name, right.name);
			return comparison != 0 ? comparison : left.GetInstanceID().CompareTo(right.GetInstanceID());
		}

		private static bool CanAffordUpgrade(Tower tower, int currency)
		{
			return tower != null && tower.Stats != null && tower.CanUpgrade() && tower.Stats.UpgradeCost <= currency;
		}

		private int CountUncoveredEntrancesWithin(Vector3 candidate, float range)
		{
			if (_tileMapManager == null || _tileMapManager.SpawnPositions == null)
				return 0;

			var towers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
			var count = 0;
			foreach (var entrance in _tileMapManager.SpawnPositions)
			{
				if (!IsEntranceCovered(entrance, towers) && Vector3.Distance(candidate, entrance) <= range)
					count++;
			}

			return count;
		}

		private void LogCoveragePreparationHold(GameplayTelemetrySnapshot snapshot)
		{
			if (_coverageHoldLogged || snapshot == null)
				return;

			_coverageHoldLogged = true;
			RefreshPlacementSlots();
			var validSlotCount = 0;
			var visibleSlotCount = 0;
			for (var slotIndex = 0; slotIndex < MaxPlacementSlots; slotIndex++)
			{
				if (!_placementSlotValid[slotIndex] || _reservedPlacementSlots[slotIndex])
					continue;

				validSlotCount++;
				if (TryGetPlacementScreenPosition(slotIndex, out _))
					visibleSlotCount++;
			}

			var towerOptions = string.Empty;
			for (var towerIndex = 0; towerIndex < MaxTowerPrefabs; towerIndex++)
			{
				var tower = GetTowerPrefab(towerIndex);
				if (tower == null || tower.Stats == null || tower.Stats.statsSO == null || tower.Stats.statsSO.Cost > snapshot.Currency)
					continue;

				var maxCoverage = 0;
				for (var slotIndex = 0; slotIndex < MaxPlacementSlots; slotIndex++)
				{
					if (!_placementSlotValid[slotIndex] || _reservedPlacementSlots[slotIndex])
						continue;

					maxCoverage = Mathf.Max(
						maxCoverage,
						TowerPlacementSystem.CountCoveredEntrances(
							_placementSlots[slotIndex],
							GetPlanningTowerRange(tower),
							_tileMapManager.SpawnPositions));
				}

				var configuredRange = tower.Stats.statsSO.Range.BaseValue;
				towerOptions += (towerOptions.Length > 0 ? "|" : string.Empty) +
					$"{towerIndex}:{tower.name};cost={tower.Stats.statsSO.Cost};range={GetPlanningTowerRange(tower):F1};" +
					$"configuredRange={configuredRange:F1};runtimeRange={tower.EffectiveRange:F1};maxCoverage={maxCoverage}";
			}

			Debug.Log(
				$"[MLAgent] Preparation hold=coverage;covered={snapshot.CoveredEntrances}/{snapshot.TotalEntrances};" +
				$"currency={snapshot.Currency};placementSlot=unavailable;" +
				$"slots={validSlotCount};visibleSlots={visibleSlotCount};towerOptions={towerOptions}");
		}

		private void LogCoveragePreparationBypass(GameplayTelemetrySnapshot snapshot)
		{
			if (_coverageBypassLogged || snapshot == null)
				return;

			_coverageBypassLogged = true;
			Debug.Log(
				$"[MLAgent] Coverage gate=unreachable;covered={snapshot.CoveredEntrances}/{snapshot.TotalEntrances};" +
				$"currency={snapshot.Currency};action=StartWave");
		}

		private bool TryFindCoveragePlacement(GameplayTelemetrySnapshot snapshot, out int towerIndex, out int placementIndex)
		{
			towerIndex = -1;
			placementIndex = -1;
			if (snapshot == null || !ShouldForceCoveragePlacement(snapshot.TotalEntrances, snapshot.CoveredEntrances, true) ||
				!TryFindAffordableTower(snapshot, out towerIndex))
				return false;

			return TryFindFreePlacementSlot(towerIndex, out placementIndex, true);
		}

		private bool TryFindReinforcementPlacement(GameplayTelemetrySnapshot snapshot, out int towerIndex, out int placementIndex)
		{
			towerIndex = -1;
			placementIndex = -1;
			if (snapshot == null)
				return false;

			var towerCount = snapshot.Towers != null ? snapshot.Towers.Count : 0;
			var cheapestTowerCost = GetCheapestTowerCost();
			var hasAffordableUpgrade = TryFindAffordableUpgrade(snapshot.Currency, out var upgradeTargetIndex);
			var hasCoveragePlacement = TryFindCoveragePlacement(snapshot, out _, out _);
			var prioritizeCoveredBuild = ShouldPrioritizeReinforcementPlacement(
				snapshot.Currency,
				cheapestTowerCost,
				towerCount,
				snapshot.EntryCoverageRatio,
				hasAffordableUpgrade);
			var prioritizeRouteReinforcement = ShouldPrioritizeRouteReinforcementPlacement(
				snapshot.Currency,
				cheapestTowerCost,
				towerCount,
				snapshot.EntryCoverageRatio,
				!hasCoveragePlacement);
			if (!TryFindAffordableTower(snapshot, out towerIndex))
				return false;

			var prioritizeFinalWaveUpgrade = ShouldPrioritizeFinalWaveUpgrade(
				_waveManager != null ? _waveManager.CurrentWaveNumber : 0,
				_waveManager != null ? _waveManager.TotalWaves : 0,
				hasAffordableUpgrade,
				snapshot.EntryCoverageRatio);
			var prioritizeCombatPower = false;
			if (!prioritizeFinalWaveUpgrade && !prioritizeCoveredBuild && !prioritizeRouteReinforcement && hasAffordableUpgrade)
			{
				var orderedTowers = GetOrderedTowers();
				var upgradeTarget = upgradeTargetIndex >= 0 && upgradeTargetIndex < orderedTowers.Length
					? orderedTowers[upgradeTargetIndex]
					: null;
				var candidateTower = GetTowerPrefab(towerIndex);
				var candidateIsAreaRole = candidateTower != null && candidateTower.GetComponent<AoEWeapon>() != null;
				var candidateCost = candidateTower != null && candidateTower.Stats != null && candidateTower.Stats.statsSO != null
					? candidateTower.Stats.statsSO.Cost
					: 0;
				var upgradeCost = upgradeTarget != null && upgradeTarget.Stats != null
					? upgradeTarget.Stats.UpgradeCost.ValueInt
					: 0;
				prioritizeCombatPower = ShouldPreferNewTowerOverUpgrade(
					snapshot.Currency,
					candidateCost,
					GetPlanningTowerCombatPower(candidateTower, candidateIsAreaRole),
					upgradeCost,
					GetUpgradeCombatPowerGain(upgradeTarget),
					towerCount,
					snapshot.EntryCoverageRatio);
			}

			if (prioritizeFinalWaveUpgrade || (!prioritizeCoveredBuild && !prioritizeRouteReinforcement && !prioritizeCombatPower))
				return false;

			if (prioritizeCoveredBuild)
				return TryFindFreePlacementSlot(towerIndex, out placementIndex, true) ||
					TryFindFreePlacementSlot(towerIndex, out placementIndex);

			if (!TryFindFreePlacementSlot(towerIndex, out placementIndex))
				return false;

			if (prioritizeCombatPower)
				return true;

			return AddsRouteCoverage(towerIndex, placementIndex);
		}

		private bool AddsRouteCoverage(int towerIndex, int placementIndex)
		{
			if (_tileMapManager == null || placementIndex < 0 || placementIndex >= MaxPlacementSlots ||
				!_placementSlotValid[placementIndex])
				return false;

			var routeSamples = TowerPlacementSystem.BuildRouteSamples(
				_tileMapManager.SpawnPositions,
				_tileMapManager.BasePosition,
				Mathf.Max(0.5f, _tileMapManager.TileSize * 0.25f));
			if (routeSamples.Count == 0)
				return false;

			var tower = GetTowerPrefab(towerIndex);
			var towers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
			var coveredBefore = TowerPlacementSystem.CountCoveredRouteSamples(towers, routeSamples);
			var coveredAfter = TowerPlacementSystem.CountCoveredRouteSamples(
				towers,
				_placementSlots[placementIndex],
				GetPlanningTowerRange(tower),
				routeSamples);
			return coveredAfter > coveredBefore;
		}

		private float GetUncoveredEntranceProximity(Vector3 candidate)
		{
			if (_tileMapManager == null || _tileMapManager.SpawnPositions == null || _tileMapManager.SpawnPositions.Count == 0)
				return 0f;

			var towers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
			var nearestDistance = float.MaxValue;
			foreach (var entrance in _tileMapManager.SpawnPositions)
			{
				if (!IsEntranceCovered(entrance, towers))
					nearestDistance = Mathf.Min(nearestDistance, Vector3.Distance(candidate, entrance));
			}

			return nearestDistance == float.MaxValue ? 0f : 1f / (1f + nearestDistance);
		}

		private static bool IsEntranceCovered(Vector3 entrance, Tower[] towers)
		{
			foreach (var tower in towers)
			{
				if (tower != null && Vector3.Distance(tower.transform.position, entrance) <= tower.EffectiveRange)
					return true;
			}

			return false;
		}

		public static bool ShouldForceCoveragePlacement(int totalEntrances, int coveredEntrances, bool hasAffordablePlacement)
		{
			return hasAffordablePlacement && totalEntrances > 0 && coveredEntrances < totalEntrances;
		}

		public static bool ShouldHoldForCoverage(
			int totalEntrances,
			int coveredEntrances,
			bool hasAffordablePlacement,
			bool hasReachablePlacement)
		{
			return hasReachablePlacement && ShouldForceCoveragePlacement(totalEntrances, coveredEntrances, hasAffordablePlacement);
		}

		public static int ChooseBestAffordableTower(
			IReadOnlyList<int> costs,
			IReadOnlyList<int> coverageGains,
			IReadOnlyList<bool> placementAvailable,
			int currency,
			int cheapestTowerCost,
			IReadOnlyList<bool> areaRoles = null,
			bool openingDefense = false,
			int upcomingEnemyCount = 0,
			IReadOnlyList<float> combatPowers = null)
		{
			if (costs == null || coverageGains == null || placementAvailable == null)
				return -1;

			var count = Mathf.Min(costs.Count, Mathf.Min(coverageGains.Count, placementAvailable.Count));
			var bestIndex = -1;
			var bestScore = float.MinValue;
			var hasOpeningReserveOption = false;
			if (openingDefense && cheapestTowerCost > 0)
			{
				for (var towerIndex = 0; towerIndex < count; towerIndex++)
				{
					if (placementAvailable[towerIndex] && costs[towerIndex] > 0 && costs[towerIndex] <= currency &&
						currency - costs[towerIndex] >= cheapestTowerCost)
					{
						hasOpeningReserveOption = true;
						break;
					}
				}
			}

			for (var towerIndex = 0; towerIndex < count; towerIndex++)
			{
				if (!placementAvailable[towerIndex] || costs[towerIndex] <= 0 || costs[towerIndex] > currency)
					continue;

				var preservesBasicPurchase = cheapestTowerCost > 0 && currency - costs[towerIndex] >= cheapestTowerCost;
				var isAreaRole = areaRoles != null && towerIndex < areaRoles.Count && areaRoles[towerIndex];
				var areaCounterEligible = ShouldPreferAreaCounter(
					upcomingEnemyCount, openingDefense, isAreaRole, currency, costs[towerIndex]);
				var openingAreaBonus = openingDefense && isAreaRole && (preservesBasicPurchase || areaCounterEligible) ? 125f : 0f;
				var openingReservePenalty = openingDefense && hasOpeningReserveOption && !preservesBasicPurchase && !areaCounterEligible
					? OpeningReserveGuardPenalty
					: 0f;
				var combatPowerScore = combatPowers != null && towerIndex < combatPowers.Count
					? Mathf.Max(0f, combatPowers[towerIndex]) * CombatPowerWeight
					: 0f;
				var coverageScoreWeight = combatPowers != null ? CoverageScoreWeight : 1000f;
				var score = Mathf.Max(0, coverageGains[towerIndex]) * coverageScoreWeight + combatPowerScore +
					openingAreaBonus + (preservesBasicPurchase ? 100f : 0f) - openingReservePenalty - costs[towerIndex] * 0.01f;
				if (score <= bestScore)
					continue;

				bestScore = score;
				bestIndex = towerIndex;
			}

			return bestIndex;
		}

		public static bool ShouldPreferAreaCounter(
			int upcomingEnemyCount,
			bool openingDefense,
			bool areaRole,
			int currency,
			int towerCost)
		{
			return openingDefense && areaRole && upcomingEnemyCount >= OpeningAreaCounterEnemyCountThreshold &&
				currency >= towerCost && towerCost > 0;
		}

		private float GetBaseConcentration(Vector3 position)
		{
			if (_tileMapManager == null || _tileMapManager.SpawnPositions == null || _tileMapManager.SpawnPositions.Count == 0)
				return 0f;

			var maxDistance = 1f;
			foreach (var entrance in _tileMapManager.SpawnPositions)
				maxDistance = Mathf.Max(maxDistance, Vector3.Distance(_tileMapManager.BasePosition, entrance));

			return 1f - Mathf.Clamp01(Vector3.Distance(_tileMapManager.BasePosition, position) / maxDistance);
		}

		private bool TryFindAffordableTower(GameplayTelemetrySnapshot snapshot, out int towerIndex)
		{
			if (snapshot == null)
			{
				towerIndex = -1;
				return false;
			}

			var costs = new int[MaxTowerPrefabs];
			var coverageGains = new int[MaxTowerPrefabs];
			var placementAvailable = new bool[MaxTowerPrefabs];
			var areaRoles = new bool[MaxTowerPrefabs];
			var combatPowers = new float[MaxTowerPrefabs];
			var cheapestTowerCost = GetCheapestTowerCost();
			var towerCount = snapshot.Towers != null ? snapshot.Towers.Count : 0;
			var openingDefense = snapshot.WavesCompleted == 0 && towerCount == 0;
			var upcomingEnemyCount = _waveManager != null ? _waveManager.GetUpcomingWaveTotalEnemyCount() : 0;
			for (var i = 0; i < MaxTowerPrefabs; i++)
			{
				var tower = GetTowerPrefab(i);
				if (tower == null || tower.Stats == null || tower.Stats.statsSO == null || tower.Stats.statsSO.Cost > snapshot.Currency)
					continue;

				if (!TryFindFreePlacementSlot(i, out var placementIndex, true) &&
					!TryFindFreePlacementSlot(i, out placementIndex))
					continue;

				costs[i] = tower.Stats.statsSO.Cost;
				coverageGains[i] = CountUncoveredEntrancesWithin(_placementSlots[placementIndex], GetPlanningTowerRange(tower));
				placementAvailable[i] = true;
				areaRoles[i] = tower.GetComponent<AoEWeapon>() != null;
				combatPowers[i] = GetPlanningTowerCombatPower(tower, areaRoles[i]);
			}

			towerIndex = ChooseBestAffordableTower(
				costs,
				coverageGains,
				placementAvailable,
				snapshot.Currency,
				cheapestTowerCost,
				areaRoles,
				openingDefense,
				upcomingEnemyCount,
				combatPowers);
			return towerIndex >= 0;
		}

		private static float GetPlanningTowerCombatPower(Tower tower, bool areaRole)
		{
			if (tower == null || tower.Stats == null || tower.Stats.statsSO == null)
				return 0f;

			var baseDps = tower.Stats.statsSO.Damage.BaseValue * tower.Stats.statsSO.FireRate.BaseValue;
			return Mathf.Max(0f, baseDps) * (areaRole ? 3f : 1f);
		}

		private static float GetUpgradeCombatPowerGain(Tower tower)
		{
			if (tower == null || tower.Stats == null || tower.Stats.statsSO == null || !tower.CanUpgrade())
				return 0f;

			var currentDps = tower.Stats.statsSO.CalculateDPS(tower.Stats.currentGrade);
			var nextDps = tower.Stats.statsSO.CalculateDPS(tower.Stats.currentGrade + 1);
			var areaRole = tower.GetComponent<AoEWeapon>() != null;
			return Mathf.Max(0f, nextDps - currentDps) * (areaRole ? 3f : 1f);
		}

		private string GetPlacementReason(GameplayTelemetrySnapshot snapshot, Tower tower, bool requireCoverage)
		{
			if (requireCoverage)
				return "coverage";

			if (snapshot == null || snapshot.EntryCoverageRatio < 0.999f)
				return "route-reinforcement";

			if (!TryFindAffordableUpgrade(snapshot.Currency, out var upgradeTargetIndex))
				return "reinforcement";

			var orderedTowers = GetOrderedTowers();
			var upgradeTarget = upgradeTargetIndex >= 0 && upgradeTargetIndex < orderedTowers.Length
				? orderedTowers[upgradeTargetIndex]
				: null;
			var newTowerCost = tower != null && tower.Stats != null && tower.Stats.statsSO != null
				? tower.Stats.statsSO.Cost
				: 0;
			var upgradeCost = upgradeTarget != null && upgradeTarget.Stats != null
				? upgradeTarget.Stats.UpgradeCost.ValueInt
				: 0;
			var areaRole = tower != null && tower.GetComponent<AoEWeapon>() != null;
			return ShouldPreferNewTowerOverUpgrade(
				snapshot.Currency,
				newTowerCost,
				GetPlanningTowerCombatPower(tower, areaRole),
				upgradeCost,
				GetUpgradeCombatPowerGain(upgradeTarget),
				snapshot.Towers != null ? snapshot.Towers.Count : 0,
				snapshot.EntryCoverageRatio)
				? "combat-power-over-upgrade"
				: "reinforcement";
		}

		private int GetCheapestTowerCost()
		{
			var cheapestCost = int.MaxValue;
			for (var i = 0; i < MaxTowerPrefabs; i++)
			{
				var tower = GetTowerPrefab(i);
				if (tower != null && tower.Stats != null && tower.Stats.statsSO != null && tower.Stats.statsSO.Cost > 0)
					cheapestCost = Mathf.Min(cheapestCost, tower.Stats.statsSO.Cost);
			}

			return cheapestCost == int.MaxValue ? 0 : cheapestCost;
		}

		public static bool ShouldPrioritizeEmergencyRepairs(
			int baseHealth,
			int baseMaxHealth,
			int currency,
			int towerCount,
			int cheapestTowerCost)
		{
			if (baseMaxHealth <= 0 || baseHealth <= 0 || baseHealth >= baseMaxHealth ||
				baseHealth > Mathf.CeilToInt(baseMaxHealth * 0.75f))
				return false;

			var canStillBuyBasicTower = cheapestTowerCost > 0 && currency >= cheapestTowerCost;
			var criticalBaseHealth = baseHealth <= Mathf.CeilToInt(baseMaxHealth * 0.5f);
			return canStillBuyBasicTower || criticalBaseHealth;
		}

		public static bool ShouldPrioritizeBountyContract(
			int currentWaveNumber,
			int totalWaves,
			int baseHealth,
			int baseMaxHealth,
			int currency,
			int towerCount,
			int cheapestTowerCost,
			float entryCoverageRatio)
		{
			if (currentWaveNumber <= 0 || currentWaveNumber >= totalWaves || baseMaxHealth <= 0 ||
				baseHealth <= Mathf.CeilToInt(baseMaxHealth * 0.75f) || towerCount < 2 ||
				cheapestTowerCost <= 0 || currency < cheapestTowerCost || entryCoverageRatio < 0.999f)
				return false;

			return true;
		}

		public static bool ShouldPrioritizeUpgrade(bool hasPrioritizedPlacement, bool hasAffordableUpgrade)
		{
			return !hasPrioritizedPlacement && hasAffordableUpgrade;
		}

		public static bool ShouldPrioritizePlacement(
			bool placementOwnerRejected,
			bool hasCoveragePlacement,
			bool hasReinforcementPlacement)
		{
			return !placementOwnerRejected && (hasCoveragePlacement || hasReinforcementPlacement);
		}

		public static bool ShouldPrioritizeReinforcementPlacement(
			int currency,
			int cheapestTowerCost,
			int towerCount,
			float entryCoverageRatio,
			bool hasAffordableUpgrade)
		{
			return !hasAffordableUpgrade && towerCount > 0 && entryCoverageRatio >= 0.999f &&
				cheapestTowerCost > 0 && currency >= cheapestTowerCost;
		}

		public static bool ShouldPrioritizeFinalWaveUpgrade(
			int currentWaveNumber,
			int totalWaves,
			bool hasAffordableUpgrade,
			float entryCoverageRatio)
		{
			return hasAffordableUpgrade && totalWaves > 0 && currentWaveNumber + 1 >= totalWaves &&
				entryCoverageRatio >= 0.999f;
		}

		public static bool ShouldPreferNewTowerOverUpgrade(
			int currency,
			int newTowerCost,
			float newTowerCombatPower,
			int upgradeCost,
			float upgradeCombatPowerGain,
			int towerCount,
			float entryCoverageRatio)
		{
			if (towerCount <= 0 || entryCoverageRatio < 0.999f || newTowerCost <= 0 || upgradeCost <= 0 ||
				currency < newTowerCost || currency < upgradeCost)
				return false;

			return Mathf.Max(0f, newTowerCombatPower) > Mathf.Max(0f, upgradeCombatPowerGain) * 2f;
		}

		public static bool ShouldPrioritizeRouteReinforcementPlacement(
			int currency,
			int cheapestTowerCost,
			int towerCount,
			float entryCoverageRatio,
			bool coveragePlacementUnavailable)
		{
			return coveragePlacementUnavailable && towerCount > 0 &&
				entryCoverageRatio < 0.999f && cheapestTowerCost > 0 && currency >= cheapestTowerCost;
		}

		private static bool TryFindAffordableUpgrade(int currency, out int targetIndex)
		{
			var orderedTowers = GetOrderedTowers();
			for (var i = 0; i < orderedTowers.Length; i++)
			{
				if (!CanAffordUpgrade(orderedTowers[i], currency))
					continue;

				targetIndex = i;
				return true;
			}

			targetIndex = -1;
			return false;
		}

		private Tower GetTowerPrefab(int index)
		{
			return _towerPrefabs != null && index >= 0 && index < _towerPrefabs.Count ? _towerPrefabs[index] : null;
		}

		private void SubscribeToGameplayEvents()
		{
			if (_subscribed)
			{
				return;
			}

			_gameManager?.onVictory.AddListener(OnVictory);
			_gameManager?.onGameOver.AddListener(OnGameOver);
			_waveManager?.onWaveCompleted.AddListener(OnWaveCompleted);
			_playerBase?.onHealthChanged.AddListener(OnBaseHealthChanged);
			_subscribed = true;
		}

		private void UnsubscribeFromGameplayEvents()
		{
			if (!_subscribed)
			{
				return;
			}

			_gameManager?.onVictory.RemoveListener(OnVictory);
			_gameManager?.onGameOver.RemoveListener(OnGameOver);
			_waveManager?.onWaveCompleted.RemoveListener(OnWaveCompleted);
			_playerBase?.onHealthChanged.RemoveListener(OnBaseHealthChanged);
			_subscribed = false;
		}

		private void OnWaveCompleted(int waveIndex)
		{
			InvalidatePlacementSlots();
			var snapshot = CaptureSnapshot();
			var waveReward = 0.05f + snapshot.EntryCoverageRatio * 0.25f + snapshot.TowerBaseConcentration * 0.05f;
			AddReward(waveReward);

			if (!Application.isPlaying)
				return;

			var statsRecorder = Academy.Instance.StatsRecorder;
			statsRecorder.Add("TD3D/Player/WaveEvaluation", 1f);
			statsRecorder.Add("TD3D/Player/WaveEntryCoverageRatio", snapshot.EntryCoverageRatio);
			statsRecorder.Add("TD3D/Player/WaveCoveredEntrances", snapshot.CoveredEntrances);
			statsRecorder.Add("TD3D/Player/WaveTotalEntrances", snapshot.TotalEntrances);
			statsRecorder.Add("TD3D/Player/WaveTowerBaseConcentration", snapshot.TowerBaseConcentration);
			statsRecorder.Add("TD3D/Player/WaveReward", waveReward);
		}

		private void OnBaseHealthChanged(int currentHealth)
		{
			if (_lastBaseHealth >= 0 && currentHealth < _lastBaseHealth && _playerBase != null && _playerBase.MaxHealth > 0)
			{
				AddReward(-0.05f * (_lastBaseHealth - currentHealth) / _playerBase.MaxHealth);
			}

			_lastBaseHealth = currentHealth;
		}

		private void RecordEvaluationStats(GameplayEvaluationMetrics evaluation)
		{
			if (!Application.isPlaying)
				return;

			var statsRecorder = Academy.Instance.StatsRecorder;
			statsRecorder.Add("TD3D/Player/Victory", evaluation.IsVictory ? 1f : 0f);
			statsRecorder.Add("TD3D/Player/Defeat", evaluation.IsDefeat ? 1f : 0f);
			statsRecorder.Add("TD3D/Player/Timeout", evaluation.IsTimedOut ? 1f : 0f);
			statsRecorder.Add("TD3D/Player/Success", evaluation.SuccessScore);
			statsRecorder.Add("TD3D/Player/CompletionRatio", evaluation.CompletionRatio);
			statsRecorder.Add("TD3D/Player/WavesCompleted", evaluation.WavesCompleted);
			statsRecorder.Add("TD3D/Player/BaseHealthFraction", evaluation.BaseHealthFraction);
			statsRecorder.Add("TD3D/Player/BaseHealthLossFraction", evaluation.BaseHealthLossFraction);
			statsRecorder.Add("TD3D/Player/CurrencySavingsRatio", evaluation.CurrencySavingsRatio);
			statsRecorder.Add("TD3D/Player/UpgradeScore", evaluation.UpgradeScore);
			statsRecorder.Add("TD3D/Player/TotalEntrances", evaluation.TotalEntrances);
			statsRecorder.Add("TD3D/Player/CoveredEntrances", evaluation.CoveredEntrances);
			statsRecorder.Add("TD3D/Player/EntryCoverageRatio", evaluation.EntryCoverageRatio);
			statsRecorder.Add("TD3D/Player/TowerBaseConcentration", evaluation.TowerBaseConcentration);
			statsRecorder.Add("TD3D/Player/Reward", evaluation.PlayerReward);
		}

		private void OnVictory()
		{
			if (!_trainingMode)
				return;

			FinishEpisode(true, false);
		}

		private void OnGameOver()
		{
			if (!_trainingMode)
				return;

			FinishEpisode(false, true);
		}

		private void FinishEpisode(bool victory, bool defeat, bool timedOut = false)
		{
			if (_episodeFinished)
				return;

			_episodeFinished = true;
			var evaluation = timedOut
				? GameplayEvaluationMetrics.CreateTimeout(CaptureSnapshot())
				: GameplayEvaluationMetrics.Create(CaptureSnapshot(), victory, defeat);
			RecordEvaluationStats(evaluation);
			AddReward(evaluation.PlayerReward);
			EndEpisode();
		}

		private void ResetLocalEpisodeState()
		{
			InvalidatePlacementSlots();
			_coverageHoldLogged = false;
			_coverageBypassLogged = false;
			for (var i = 0; i < MaxPlacementSlots; i++)
			{
				_reservedPlacementSlots[i] = false;
			}

			_lastBaseHealth = -1;
			_lastTelemetrySequence = _gameplayTelemetry != null ? _gameplayTelemetry.LastSequence : 0;
			_episodeStartTime = Time.time;
			_episodeFinished = false;
		}

		private bool TryFinishTimedOutEpisode()
		{
			if (!_episodeStarted || _episodeFinished || _gameManager == null || !_gameManager.IsPlaying ||
				_episodeTimeLimitSeconds <= 0f || Time.time - _episodeStartTime < _episodeTimeLimitSeconds)
			{
				return false;
			}

			FinishEpisode(false, true, true);
			return true;
		}

		private void ApplyConfiguredMlTestTimeScale()
		{
			if (Application.isPlaying && _trainingMode && _applyMlTestTimeScale)
			{
				Time.timeScale = _mlTestTimeScale;
			}
		}

		private void OnValidate()
		{
			_mlTestTimeScale = Mathf.Clamp(_mlTestTimeScale, MinimumMlTestTimeScale, MaximumMlTestTimeScale);
		}

		private static int GetStateIndex(string state)
		{
			if (!Enum.TryParse(state, out GameState parsedState))
			{
				return 0;
			}

			return Mathf.Clamp((int)parsedState, 0, MaxStateValues - 1);
		}

		private static float Normalize(float value, float maximum)
		{
			return maximum > 0f ? Mathf.Clamp01(value / maximum) : 0f;
		}
	}
}
