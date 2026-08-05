using System;
using TD.GameLoop;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace TD.MLAgents
{
	public class TowerDefenceEnemyLevelAgent : Agent
	{
		public const int ObservationSize = 30;
		public const int ActionBranchCount = 11;
		public const int ActionBranchSize = 5;
		public const int SeedBranchSize = WaveManager.GeneratedSeedActionSize;
		public const int ArchetypeBranchSize = WaveManager.GeneratedArchetypeActionSize;
		public const int CountBranchSize = WaveManager.GeneratedCountActionSize;
		public const int PacingBranchSize = WaveManager.GeneratedPacingActionSize;
		public const float DefaultEpisodeTimeLimitSeconds = TowerDefenceAgent.DefaultEpisodeTimeLimitSeconds;
		public const float MinimumEpisodeTimeLimitSeconds = TowerDefenceAgent.MinimumEpisodeTimeLimitSeconds;

		private const int MaxStateValues = 9;
		private const int NeutralAction = 2;

		[SerializeField] private GameManager _gameManager;
		[SerializeField] private WaveManager _waveManager;
		[SerializeField] private GameplayTelemetry _gameplayTelemetry;
		[SerializeField] private bool _trainingMode = true;
		[SerializeField, Min(MinimumEpisodeTimeLimitSeconds)] private float _episodeTimeLimitSeconds = DefaultEpisodeTimeLimitSeconds;

		private int _lastGeneratedWave = -1;
		private WaveConfig _lastEvaluatedGeneratedWave;
		private float _episodeStartTime;
		private bool _subscribed;
		private bool _episodeFinished;
		private bool _episodeStarted;

		public float EpisodeTimeLimitSeconds
		{
			get => _episodeTimeLimitSeconds;
			set => _episodeTimeLimitSeconds = Mathf.Max(MinimumEpisodeTimeLimitSeconds, value);
		}

		public bool TrainingMode
		{
			get => _trainingMode;
			set => _trainingMode = value;
		}

		private void Start()
		{
			_gameManager?.onVictory.AddListener(OnVictory);
			_gameManager?.onGameOver.AddListener(OnGameOver);
			_waveManager?.onWaveCompleted.AddListener(OnWaveCompleted);
			_subscribed = true;
		}

		private void OnDestroy()
		{
			if (!_subscribed)
				return;

			_gameManager?.onVictory.RemoveListener(OnVictory);
			_gameManager?.onGameOver.RemoveListener(OnGameOver);
			_waveManager?.onWaveCompleted.RemoveListener(OnWaveCompleted);
			_subscribed = false;
		}

		public override void OnEpisodeBegin()
		{
			_lastGeneratedWave = -1;
			_lastEvaluatedGeneratedWave = null;
			_episodeStartTime = Time.time;
			_episodeFinished = false;
			_episodeStarted = true;
		}

		public override void CollectObservations(VectorSensor sensor)
		{
			var snapshot = _gameplayTelemetry.CaptureSnapshot();
			sensor.AddOneHotObservation(GetStateIndex(snapshot.GameState), MaxStateValues);
			sensor.AddObservation(Mathf.Clamp01(snapshot.WaveNumber / 20f));
			sensor.AddObservation(Mathf.Clamp01(snapshot.TotalWaves / 20f));
			sensor.AddObservation(snapshot.IsSpawning ? 1f : 0f);
			sensor.AddObservation(Mathf.Clamp01(snapshot.EnemiesAlive / 20f));
			sensor.AddObservation(Mathf.Clamp01(snapshot.ActiveEnemyCount / 20f));
			sensor.AddObservation(snapshot.BaseMaxHealth > 0 ? Mathf.Clamp01((float)snapshot.BaseHealth / snapshot.BaseMaxHealth) : 0f);
			sensor.AddObservation(Mathf.Clamp01(snapshot.Currency / 200f));
			sensor.AddObservation(Mathf.Clamp01(snapshot.WaveProgress));
			sensor.AddObservation(snapshot.RewardOfferPending ? 1f : 0f);
			sensor.AddObservation(snapshot.IsTilePlacing ? 1f : 0f);
			sensor.AddObservation(snapshot.CanSelectChallengeModifier ? 1f : 0f);
			sensor.AddObservation(snapshot.AdaptiveDifficultyScore);
			sensor.AddObservation(Mathf.InverseLerp(WaveManager.EnemyLevelFactorMinimum, WaveManager.EnemyLevelFactorMaximum, snapshot.EnemyLevelHealthFactor));
			sensor.AddObservation(Mathf.InverseLerp(WaveManager.EnemyLevelFactorMinimum, WaveManager.EnemyLevelFactorMaximum, snapshot.EnemyLevelCountFactor));
			sensor.AddObservation(Mathf.InverseLerp(WaveManager.EnemyLevelSpeedMinimum, WaveManager.EnemyLevelSpeedMaximum, snapshot.EnemyLevelSpeedFactor));
			sensor.AddObservation(snapshot.EnemyLevelDifficultyScore);
			sensor.AddObservation(snapshot.HasGeneratedWave ? 1f : 0f);
			sensor.AddObservation(Mathf.Clamp01(snapshot.GeneratedWaveGroupCount / (float)WaveManager.GeneratedEnemySlotCount));
			sensor.AddObservation(Mathf.Clamp01(snapshot.GeneratedWavePredictedDamageFraction));
			sensor.AddObservation(snapshot.GeneratedWaveTensionScore);
			sensor.AddObservation(Mathf.Clamp01(snapshot.GeneratedWaveSeed / 1000000f));
		}

		public override void OnActionReceived(ActionBuffers actions)
		{
			if (!_trainingMode || actions.DiscreteActions.Length < ActionBranchCount)
				return;

			if (TryFinishTimedOutEpisode())
				return;

			var snapshot = _gameplayTelemetry.CaptureSnapshot();
			if (_gameManager.CurrentState != GameState.Preparation || !_waveManager.CanApplyEnemyLevel ||
				snapshot.RewardOfferPending || snapshot.IsTilePlacing || _lastGeneratedWave == snapshot.WaveNumber)
				return;

			var healthAction = Mathf.Clamp(actions.DiscreteActions[0], 0, ActionBranchSize - 1);
			var countAction = Mathf.Clamp(actions.DiscreteActions[1], 0, ActionBranchSize - 1);
			var speedAction = Mathf.Clamp(actions.DiscreteActions[2], 0, ActionBranchSize - 1);
			var seedAction = Mathf.Clamp(actions.DiscreteActions[3], 0, SeedBranchSize - 1);
			var archetypeActions = new[]
			{
				Mathf.Clamp(actions.DiscreteActions[4], 0, ArchetypeBranchSize - 1),
				Mathf.Clamp(actions.DiscreteActions[5], 0, ArchetypeBranchSize - 1),
				Mathf.Clamp(actions.DiscreteActions[6], 0, ArchetypeBranchSize - 1)
			};
			var countActions = new[]
			{
				Mathf.Clamp(actions.DiscreteActions[7], 0, CountBranchSize - 1),
				Mathf.Clamp(actions.DiscreteActions[8], 0, CountBranchSize - 1),
				Mathf.Clamp(actions.DiscreteActions[9], 0, CountBranchSize - 1)
			};
			var pacingAction = Mathf.Clamp(actions.DiscreteActions[10], 0, PacingBranchSize - 1);
			var seed = GetGenerationSeed(seedAction, snapshot.WaveNumber);
			if (_waveManager.ApplyEnemyLevelGeneration(
				GetEnemyFactor(healthAction),
				GetEnemyFactor(countAction),
				GetSpeedFactor(speedAction),
				seed,
				archetypeActions,
				countActions,
				pacingAction))
			{
				_lastGeneratedWave = snapshot.WaveNumber;
				AddReward(0.005f);
			}
		}

		private void OnWaveCompleted(int waveNumber)
		{
			if (!_trainingMode || _episodeFinished)
				return;

			var snapshot = _gameplayTelemetry.CaptureSnapshot();
			var evaluation = GameplayEvaluationMetrics.CreateForGeneratedWave(snapshot, true, false);
			if (!TryRecordGeneratedWaveEvaluation(evaluation))
				return;

			RecordEvaluationStats(evaluation, snapshot);
			var waveReward = evaluation.BalanceReward + snapshot.EnemyLevelDifficultyScore * 0.25f;
			Academy.Instance.StatsRecorder.Add("TD3D/EnemyLevel/WaveEvaluation", 1f);
			Academy.Instance.StatsRecorder.Add("TD3D/EnemyLevel/WaveReward", waveReward);
			AddReward(waveReward);
		}

		public override void Heuristic(in ActionBuffers actionsOut)
		{
			var actions = actionsOut.DiscreteActions;
			for (var i = 0; i < actions.Length; i++)
				actions[i] = NeutralAction;

			if (actions.Length >= ActionBranchCount)
			{
				actions[3] = 1;
				actions[4] = 0;
				actions[5] = 1;
				actions[6] = 2;
				actions[7] = 1;
				actions[8] = 2;
				actions[9] = 1;
				actions[10] = 2;
			}
		}

		private void RecordEvaluationStats(GameplayEvaluationMetrics evaluation, GameplayTelemetrySnapshot snapshot)
		{
			if (!Application.isPlaying)
				return;

			var statsRecorder = Academy.Instance.StatsRecorder;
			statsRecorder.Add("TD3D/EnemyLevel/Victory", evaluation.IsVictory ? 1f : 0f);
			statsRecorder.Add("TD3D/EnemyLevel/Defeat", evaluation.IsDefeat ? 1f : 0f);
			statsRecorder.Add("TD3D/EnemyLevel/Timeout", evaluation.IsTimedOut ? 1f : 0f);
			statsRecorder.Add("TD3D/EnemyLevel/CompletionRatio", evaluation.CompletionRatio);
			statsRecorder.Add("TD3D/EnemyLevel/BaseHealthFraction", evaluation.BaseHealthFraction);
			statsRecorder.Add("TD3D/EnemyLevel/DifficultyScore", snapshot.EnemyLevelDifficultyScore);
			statsRecorder.Add("TD3D/EnemyLevel/EnemyHealthFactor", snapshot.EnemyLevelHealthFactor);
			statsRecorder.Add("TD3D/EnemyLevel/EnemyCountFactor", snapshot.EnemyLevelCountFactor);
			statsRecorder.Add("TD3D/EnemyLevel/EnemySpeedFactor", snapshot.EnemyLevelSpeedFactor);
			statsRecorder.Add("TD3D/EnemyLevel/GeneratedWave", snapshot.HasGeneratedWave ? 1f : 0f);
			statsRecorder.Add("TD3D/EnemyLevel/GeneratedGroups", snapshot.GeneratedWaveGroupCount);
			statsRecorder.Add("TD3D/EnemyLevel/PredictedDamageFraction", snapshot.GeneratedWavePredictedDamageFraction);
			statsRecorder.Add("TD3D/EnemyLevel/PredictedCombatSeconds", _waveManager.GeneratedWavePredictedCombatSeconds);
			statsRecorder.Add("TD3D/EnemyLevel/AppliedAdaptiveHealthFactor", _waveManager.GeneratedWaveAppliedAdaptiveEnemyHealthFactor);
			statsRecorder.Add("TD3D/EnemyLevel/AppliedAdaptiveCountFactor", _waveManager.GeneratedWaveAppliedAdaptiveEnemyCountFactor);
			statsRecorder.Add("TD3D/EnemyLevel/AppliedAdaptiveSpeedFactor", _waveManager.GeneratedWaveAppliedAdaptiveEnemySpeedFactor);
			statsRecorder.Add("TD3D/EnemyLevel/AppliedAdaptiveRewardFactor", _waveManager.GeneratedWaveAppliedAdaptiveRewardFactor);
			statsRecorder.Add("TD3D/EnemyLevel/TensionScore", snapshot.GeneratedWaveTensionScore);
			statsRecorder.Add("TD3D/EnemyLevel/Reward", evaluation.BalanceReward);
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
			var snapshot = _gameplayTelemetry.CaptureSnapshot();
			var evaluation = timedOut
				? GameplayEvaluationMetrics.CreateGeneratedWaveTimeout(snapshot)
				: GameplayEvaluationMetrics.CreateForGeneratedWave(snapshot, victory, defeat);
			RecordEvaluationStats(evaluation, snapshot);
			TryRecordGeneratedWaveEvaluation(evaluation);
			AddReward(victory
				? evaluation.BalanceReward + snapshot.EnemyLevelDifficultyScore * 0.25f
				: evaluation.BalanceReward);
			EndEpisode();
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

		private bool TryRecordGeneratedWaveEvaluation(GameplayEvaluationMetrics evaluation)
		{
			var generatedWave = _waveManager.ActiveWaveConfig;
			if (generatedWave == null || !generatedWave.GeneratedByMl || generatedWave == _lastEvaluatedGeneratedWave)
				return false;

			_waveManager.RecordGeneratedWaveEvaluation(evaluation, evaluation.IsVictory, evaluation.IsDefeat);
			_lastEvaluatedGeneratedWave = generatedWave;
			return true;
		}

		private static float GetEnemyFactor(int action)
		{
			return Mathf.Lerp(WaveManager.EnemyLevelFactorMinimum, WaveManager.EnemyLevelFactorMaximum, action / (float)(ActionBranchSize - 1));
		}

		private static float GetSpeedFactor(int action)
		{
			return Mathf.Lerp(WaveManager.EnemyLevelSpeedMinimum, WaveManager.EnemyLevelSpeedMaximum, action / (float)(ActionBranchSize - 1));
		}

		private static int GetGenerationSeed(int action, int waveNumber)
		{
			var seed = unchecked(100003 + Mathf.Max(0, waveNumber) * 1009 + action * 7919);
			return seed == 0 ? 1 : seed;
		}

		private static int GetStateIndex(string state)
		{
			if (!Enum.TryParse(state, out GameState parsedState))
				return 0;

			return Mathf.Clamp((int)parsedState, 0, MaxStateValues - 1);
		}
	}
}
