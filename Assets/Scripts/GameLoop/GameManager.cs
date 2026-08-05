using Sirenix.OdinInspector;
using TD.Plugins.Timing;
using TD.Levels;
using TD.Towers;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TD.GameLoop
{
	public class GameManager : MonoBehaviour
	{
		public static GameManager Instance { get; private set; }

		[SerializeField] private bool Logs = true;
		[SerializeField] private float gameOverDelay = 2f;
		[SerializeField] private GameState currentState = GameState.Boot;
		[SerializeField] private SerializedDictionary<GameState, GameObject> _stateGameObjecs = new();
		[SerializeField] private InputActionAsset inputActions;

		public UnityEvent<GameState> onGameStateChanged;
		public UnityEvent onGameStarted;
		public UnityEvent onGamePaused;
		public UnityEvent onGameUnpaused;
		public UnityEvent onGameOver;
		public UnityEvent onVictory;
		public UnityEvent onRestartRequested;
		public RunResultEvent onRunFinished = new RunResultEvent();

		[ShowInInspector] private TimeControl TimeControl => TimeControl.Instance;

		public GameState CurrentState => currentState;
		public string CurrentRunId => currentRunId;
		public RunResult LastRunResult { get; private set; }
		public bool IsPlaying => currentState == GameState.ChallengeSelection || currentState == GameState.Preparation || currentState == GameState.WaveActive || currentState == GameState.WaveResolve;
		public bool IsPaused => TimeControl.Instance.IsPaused;
		public bool IsGameOver => currentState == GameState.Defeat || currentState == GameState.Victory;
		private PlayerBase playerBase;
		private InputActionMap playerActionMap;
		private InputAction restartAction;
		private InputAction pauseAction;
		private string currentRunId = string.Empty;
		private float runStartedAt;
		private bool runResultPublished;

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				if (Application.isPlaying)
					Destroy(gameObject);
				else
					DestroyImmediate(gameObject);
				return;
			}

			Instance = this;

			if (inputActions != null)
			{
				playerActionMap = inputActions.FindActionMap("Player", true);
				restartAction = inputActions.FindAction("Player/Restart", true);
				pauseAction = inputActions.FindAction("UI/Cancel", true);
			}
		}

		private void OnEnable()
		{
			if (restartAction != null)
			{
				restartAction.Enable();
				restartAction.performed += OnRestartInput;
			}

			if (pauseAction != null)
			{
				pauseAction.Enable();
				pauseAction.performed += OnPauseInput;
			}
		}

		private void OnDisable()
		{
			if (restartAction != null) restartAction.performed -= OnRestartInput;
			if (pauseAction != null) pauseAction.performed -= OnPauseInput;
		}

		private void OnRestartInput(InputAction.CallbackContext context)
		{
			if (context.performed && IsGameOver)
			{
				RestartGame();
			}
		}

		private void OnPauseInput(InputAction.CallbackContext context)
		{
			if (context.performed && !IsGameOver)
			{
				TogglePause();
			}
		}

		public void Initialize()
		{
			SetupEventListeners();
		}

		public void BeginBoot()
		{
			if (currentState != GameState.Boot)
			{
				ChangeState(GameState.Boot);
			}
			else if (Logs)
			{
				Debug.Log("[GameManager] State: Boot");
			}
		}

		public void BeginMapBuild()
		{
			ChangeState(GameState.MapBuild);
		}

		public void CompleteMapBuild()
		{
			if (currentState != GameState.MapBuild)
			{
				return;
			}

			BeginRun();
			ChangeState(GameState.ChallengeSelection);
			onGameStarted?.Invoke();
			TimeControl.Instance.Pause.Remove(this);
		}

		public void StartNextWave()
		{
			if (currentState != GameState.Preparation || WaveManager.Instance == null || WaveManager.Instance.TotalWaves == 0)
			{
				return;
			}

			WaveManager.Instance.StartNextWave();
		}

		public bool TryRepairBase(int amount)
		{
			if (amount <= 0 || playerBase == null || playerBase.IsDestroyed)
			{
				return false;
			}

			playerBase.Repair(amount);
			return true;
		}

		private void SetupEventListeners()
		{
			playerBase = FindAnyObjectByType<PlayerBase>();
			if (playerBase != null)
			{
				playerBase.onBaseDestroyed.AddListener(OnBaseDestroyed);
			}
			else
			{
				Debug.LogError("Base is null");
			}

			if (WaveManager.Instance != null)
			{
				WaveManager.Instance.onAllWavesCompleted.AddListener(OnAllWavesCompleted);
				WaveManager.Instance.onChallengeModifierSelected.AddListener(OnChallengeModifierSelected);
				WaveManager.Instance.onPreparationReady.AddListener(OnPreparationReady);
				WaveManager.Instance.onWaveCompleted.AddListener(OnWaveCompleted);
				WaveManager.Instance.onWaveStarted.AddListener(OnWaveStarted);
			}
			else
			{
				Debug.LogError("WaveManager is null");
			}
		}

		private void OnWaveStarted(int waveIndex)
		{
			ChangeState(GameState.WaveActive);
		}

		private void OnWaveCompleted(int waveIndex)
		{
			ChangeState(GameState.WaveResolve);
		}

		private void OnChallengeModifierSelected()
		{
			ChangeState(GameState.Preparation);
			playerActionMap?.Enable();

			if (WaveManager.Instance != null && WaveManager.Instance.AutoStartNextWave)
			{
				StartNextWave();
			}
		}

		private void OnPreparationReady()
		{
			playerActionMap?.Enable();
			ChangeState(GameState.Preparation);
		}

		private void PauseGame()
		{
			if (IsPaused) return;

			playerActionMap?.Disable();
			onGamePaused?.Invoke();
			TimeControl.Instance.Pause.Add(this);
		}

		private void UnpauseGame()
		{
			if (!IsPaused) return;

			if (currentState != GameState.ChallengeSelection)
			{
				playerActionMap?.Enable();
			}
			TimeControl.Instance.Pause.Remove(this);
			onGameUnpaused?.Invoke();
		}

		[Button]
		public void TogglePause()
		{
			if (IsPaused)
			{
				UnpauseGame();
			}
			else if (IsPlaying)
			{
				PauseGame();
			}
		}

		public void ToggleFullscreen()
		{
			Screen.SetResolution(1280, 800, FullScreenMode.Windowed);
		}

		private void OnBaseDestroyed()
		{
			if (IsGameOver) return;

			if (Logs) Debug.Log("On Base Destroyed");
			Invoke(nameof(Defeat), gameOverDelay);
		}

		private void OnAllWavesCompleted()
		{
			if (!IsGameOver)
			{
				Victory();
			}
		}

		private void Defeat()
		{
			if (IsGameOver) return;

			if (Logs) Debug.Log("Defeat");
			WaveManager.Instance?.ForceStopWave();
			ChangeState(GameState.Defeat);
			FinishRun(RunOutcome.Defeat);
			onGameOver?.Invoke();
			Time.timeScale = 0f;
		}

		private void Victory()
		{
			if (IsGameOver) return;

			ResourceManager.Instance?.UnlockStartingReserve();
			ChangeState(GameState.Victory);
			FinishRun(RunOutcome.Victory);
			onVictory?.Invoke();
		}

		private void BeginRun()
		{
			currentRunId = Guid.NewGuid().ToString("N");
			runStartedAt = Time.unscaledTime;
			LastRunResult = null;
			runResultPublished = false;
		}

		private void FinishRun(RunOutcome outcome)
		{
			if (runResultPublished)
				return;

			var currentWaveManager = WaveManager.Instance;
			var currentPlayerBase = playerBase != null ? playerBase : FindAnyObjectByType<PlayerBase>();
			var levelGenerator = FindAnyObjectByType<LevelGenerator>();
			if (string.IsNullOrEmpty(currentRunId))
				currentRunId = Guid.NewGuid().ToString("N");

			LastRunResult = new RunResult(
				currentRunId,
				1,
				outcome,
				levelGenerator != null ? levelGenerator.GeneratedSeed : 0,
				currentWaveManager != null ? currentWaveManager.ActiveChallengeModifier.ToString() : string.Empty,
				currentWaveManager != null ? currentWaveManager.WavesCompleted : 0,
				currentWaveManager != null ? currentWaveManager.CurrentWaveNumber : 0,
				currentPlayerBase != null ? currentPlayerBase.CurrentHealth : 0,
				currentPlayerBase != null ? currentPlayerBase.MaxHealth : 0,
				ResourceManager.Instance != null ? ResourceManager.Instance.CurrentCurrency : 0,
				currentWaveManager != null ? currentWaveManager.EnemiesKilled : 0,
				currentWaveManager != null ? currentWaveManager.EnemiesLeaked : 0,
				Time.unscaledTime - runStartedAt,
				Application.version);
			runResultPublished = true;
			onRunFinished?.Invoke(LastRunResult);
		}

		public void RestartGame()
		{
			CancelInvoke(nameof(Defeat));
			Time.timeScale = 1f;
			onRestartRequested?.Invoke();
			WaveManager.Instance?.ForceStopWave();
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}

		public void QuitToMenu()
		{
			Time.timeScale = 1f;

			// Load main menu scene (implement when menu scene exists)
			Debug.Log("GameManager: Quit to menu (not implemented)");
		}

		public void QuitGame()
		{
            #if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
		}

		private void ChangeState(GameState newState)
		{
			if (currentState == newState) return;

			if (Logs) Debug.Log($"[GameManager] State changed: {currentState} -> {newState}");

			currentState = newState;
			foreach (var kvp in _stateGameObjecs)
			{
				kvp.Value.SetActive(kvp.Key == currentState);
			}

			onGameStateChanged?.Invoke(newState);
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}

			if (playerBase != null)
			{
				playerBase.onBaseDestroyed.RemoveListener(OnBaseDestroyed);
			}

			if (WaveManager.Instance != null)
			{
				WaveManager.Instance.onAllWavesCompleted.RemoveListener(OnAllWavesCompleted);
				WaveManager.Instance.onChallengeModifierSelected.RemoveListener(OnChallengeModifierSelected);
				WaveManager.Instance.onPreparationReady.RemoveListener(OnPreparationReady);
				WaveManager.Instance.onWaveCompleted.RemoveListener(OnWaveCompleted);
				WaveManager.Instance.onWaveStarted.RemoveListener(OnWaveStarted);
			}

			onGameStateChanged?.RemoveAllListeners();
			onGameStarted?.RemoveAllListeners();
			onGamePaused?.RemoveAllListeners();
			onGameUnpaused?.RemoveAllListeners();
			onGameOver?.RemoveAllListeners();
			onVictory?.RemoveAllListeners();
			onRestartRequested?.RemoveAllListeners();
			onRunFinished?.RemoveAllListeners();
		}
	}
}
