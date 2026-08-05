using System;
using TD.GameLoop;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace TD.MLAgents
{
	public class TowerDefenceBalancerAgent : Agent
	{
		public const int ObservationSize = 24;
		public const int ActionBranchCount = 4;
		public const int ActionBranchSize = 5;
		public const float DefaultEpisodeTimeLimitSeconds = TowerDefenceAgent.DefaultEpisodeTimeLimitSeconds;
		public const float MinimumEpisodeTimeLimitSeconds = TowerDefenceAgent.MinimumEpisodeTimeLimitSeconds;

		private const int MaxStateValues = 9;
		private const int NeutralAction = 2;

		[SerializeField] private GameManager _gameManager;
		[SerializeField] private WaveManager _waveManager;
		[SerializeField] private GameplayTelemetry _gameplayTelemetry;
		[SerializeField] private bool _trainingMode = true;
		[SerializeField, Min(MinimumEpisodeTimeLimitSeconds)] private float _episodeTimeLimitSeconds = DefaultEpisodeTimeLimitSeconds;

		private int _lastAppliedWave = -1;
		private int _lastEvaluatedWave = -1;
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
			_lastAppliedWave = -1;
			_lastEvaluatedWave = -1;
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
			sensor.AddObservation(Mathf.Clamp01(snapshot.EnemiesAlive / 20f));
			sensor.AddObservation(Mathf.Clamp01(snapshot.ActiveEnemyCount / 20f));
			sensor.AddObservation(snapshot.BaseMaxHealth > 0 ? Mathf.Clamp01((float)snapshot.BaseHealth / snapshot.BaseMaxHealth) : 0f);
			sensor.AddObservation(Mathf.Clamp01(snapshot.Currency / 200f));
			sensor.AddObservation(snapshot.RewardOfferPending ? 1f : 0f);
			sensor.AddObservation(snapshot.IsTilePlacing ? 1f : 0f);
			sensor.AddObservation(Mathf.Clamp01(snapshot.WaveProgress));
			sensor.AddObservation(snapshot.CanSelectChallengeModifier ? 1f : 0f);
			sensor.AddObservation(Mathf.InverseLerp(WaveManager.AdaptiveEnemyFactorMinimum, WaveManager.AdaptiveEnemyFactorMaximum, snapshot.AdaptiveEnemyHealthFactor));
			sensor.AddObservation(Mathf.InverseLerp(WaveManager.AdaptiveEnemyFactorMinimum, WaveManager.AdaptiveEnemyFactorMaximum, snapshot.AdaptiveEnemyCountFactor));
			sensor.AddObservation(Mathf.InverseLerp(WaveManager.AdaptiveSpeedFactorMinimum, WaveManager.AdaptiveSpeedFactorMaximum, snapshot.AdaptiveEnemySpeedFactor));
			sensor.AddObservation(Mathf.InverseLerp(WaveManager.AdaptiveRewardFactorMinimum, WaveManager.AdaptiveRewardFactorMaximum, snapshot.AdaptiveRewardFactor));
			sensor.AddObservation(snapshot.AdaptiveDifficultyScore);
		}

		public override void OnActionReceived(ActionBuffers actions)
		{
			if (!_trainingMode || actions.DiscreteActions.Length < ActionBranchCount)
				return;

			if (TryFinishTimedOutEpisode())
				return;

			var snapshot = _gameplayTelemetry.CaptureSnapshot();
			if (_gameManager.CurrentState != GameState.Preparation ||
				!_waveManager.CanApplyAdaptiveBalance || snapshot.RewardOfferPending || snapshot.IsTilePlacing ||
				_lastAppliedWave == snapshot.WaveNumber)
				return;

			var healthAction = Mathf.Clamp(actions.DiscreteActions[0], 0, ActionBranchSize - 1);
			var countAction = Mathf.Clamp(actions.DiscreteActions[1], 0, ActionBranchSize - 1);
			var speedAction = Mathf.Clamp(actions.DiscreteActions[2], 0, ActionBranchSize - 1);
			var rewardAction = Mathf.Clamp(actions.DiscreteActions[3], 0, ActionBranchSize - 1);
			var applied = _waveManager.ApplyAdaptiveBalance(
				GetEnemyFactor(healthAction),
				GetEnemyFactor(countAction),
				GetSpeedFactor(speedAction),
				GetRewardFactor(rewardAction));

			if (applied)
			{
				_lastAppliedWave = snapshot.WaveNumber;
				AddReward(0.005f);
			}
		}

		public override void Heuristic(in ActionBuffers actionsOut)
		{
			var actions = actionsOut.DiscreteActions;
			for (var i = 0; i < actions.Length; i++)
				actions[i] = NeutralAction;
		}

		private void RecordEvaluationStats(GameplayEvaluationMetrics evaluation, GameplayTelemetrySnapshot snapshot)
		{
			if (!Application.isPlaying)
				return;

			var statsRecorder = Academy.Instance.StatsRecorder;
			statsRecorder.Add("TD3D/Balance/Victory", evaluation.IsVictory ? 1f : 0f);
			statsRecorder.Add("TD3D/Balance/Defeat", evaluation.IsDefeat ? 1f : 0f);
			statsRecorder.Add("TD3D/Balance/Timeout", evaluation.IsTimedOut ? 1f : 0f);
			statsRecorder.Add("TD3D/Balance/CompletionRatio", evaluation.CompletionRatio);
			statsRecorder.Add("TD3D/Balance/BaseHealthFraction", evaluation.BaseHealthFraction);
			statsRecorder.Add("TD3D/Balance/BaseHealthLossFraction", evaluation.BaseHealthLossFraction);
			statsRecorder.Add("TD3D/Balance/DifficultyScore", evaluation.DifficultyScore);
			statsRecorder.Add("TD3D/Balance/AdaptiveEnemyHealthFactor", snapshot.AdaptiveEnemyHealthFactor);
			statsRecorder.Add("TD3D/Balance/AdaptiveEnemyCountFactor", snapshot.AdaptiveEnemyCountFactor);
			statsRecorder.Add("TD3D/Balance/AdaptiveEnemySpeedFactor", snapshot.AdaptiveEnemySpeedFactor);
			statsRecorder.Add("TD3D/Balance/AdaptiveRewardFactor", snapshot.AdaptiveRewardFactor);
			statsRecorder.Add("TD3D/Balance/Reward", evaluation.BalanceReward);
		}

		private void RecordWaveEvaluationStats(GameplayEvaluationMetrics evaluation, GameplayTelemetrySnapshot snapshot)
		{
			if (!Application.isPlaying)
				return;

			var statsRecorder = Academy.Instance.StatsRecorder;
			statsRecorder.Add("TD3D/Balance/WaveEvaluation", 1f);
			statsRecorder.Add("TD3D/Balance/WaveBaseHealthFraction", evaluation.BaseHealthFraction);
			statsRecorder.Add("TD3D/Balance/WaveDifficultyScore", evaluation.DifficultyScore);
			statsRecorder.Add("TD3D/Balance/WaveAdaptiveEnemyHealthFactor", snapshot.AdaptiveEnemyHealthFactor);
			statsRecorder.Add("TD3D/Balance/WaveAdaptiveEnemyCountFactor", snapshot.AdaptiveEnemyCountFactor);
			statsRecorder.Add("TD3D/Balance/WaveAdaptiveEnemySpeedFactor", snapshot.AdaptiveEnemySpeedFactor);
			statsRecorder.Add("TD3D/Balance/WaveAdaptiveRewardFactor", snapshot.AdaptiveRewardFactor);
			statsRecorder.Add("TD3D/Balance/WaveReward", evaluation.BalanceReward);
		}

		private void OnVictory()
		{
			if (!_trainingMode)
				return;

			FinishEpisode(true, false);
		}

		private void OnWaveCompleted(int waveNumber)
		{
			if (!_trainingMode || _episodeFinished || waveNumber <= _lastEvaluatedWave)
				return;

			_lastEvaluatedWave = waveNumber;
			var snapshot = _gameplayTelemetry.CaptureSnapshot();
			var evaluation = GameplayEvaluationMetrics.Create(snapshot, true, false);
			RecordWaveEvaluationStats(evaluation, snapshot);
			AddReward(evaluation.BalanceReward);
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
				? GameplayEvaluationMetrics.CreateTimeout(snapshot)
				: GameplayEvaluationMetrics.Create(snapshot, victory, defeat);
			RecordEvaluationStats(evaluation, snapshot);
			AddReward(evaluation.BalanceReward);
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

		private static float GetEnemyFactor(int action)
		{
			return Mathf.Lerp(WaveManager.AdaptiveEnemyFactorMinimum, WaveManager.AdaptiveEnemyFactorMaximum, action / (float)(ActionBranchSize - 1));
		}

		private static float GetSpeedFactor(int action)
		{
			return Mathf.Lerp(WaveManager.AdaptiveSpeedFactorMinimum, WaveManager.AdaptiveSpeedFactorMaximum, action / (float)(ActionBranchSize - 1));
		}

		private static float GetRewardFactor(int action)
		{
			return Mathf.Lerp(WaveManager.AdaptiveRewardFactorMinimum, WaveManager.AdaptiveRewardFactorMaximum, action / (float)(ActionBranchSize - 1));
		}

		private static int GetStateIndex(string state)
		{
			if (!Enum.TryParse(state, out GameState parsedState))
				return 0;

			return Mathf.Clamp((int)parsedState, 0, MaxStateValues - 1);
		}
	}
}
