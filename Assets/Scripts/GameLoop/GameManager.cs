using Sirenix.OdinInspector;
using TD.Plugins.Timing;
using TD.Towers;
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

		[ShowInInspector] private TimeControl TimeControl => TimeControl.Instance;

		public GameState CurrentState => currentState;
		public bool IsPlaying => currentState == GameState.Preparation || currentState == GameState.WaveActive || currentState == GameState.WaveResolve;
		public bool IsPaused => TimeControl.Instance.IsPaused;
		public bool IsGameOver => currentState == GameState.Defeat || currentState == GameState.Victory;
		private PlayerBase playerBase;
		private InputActionMap playerActionMap;
		private InputAction restartAction;
		private InputAction pauseAction;

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
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

			playerActionMap?.Enable();
			ChangeState(GameState.Preparation);
			onGameStarted?.Invoke();
			TimeControl.Instance.Pause.Remove(this);

			if (WaveManager.Instance != null && WaveManager.Instance.AutoStartNextWave)
			{
				StartNextWave();
			}
		}

		public void StartNextWave()
		{
			if (currentState != GameState.Preparation || WaveManager.Instance == null || WaveManager.Instance.TotalWaves == 0)
			{
				return;
			}

			WaveManager.Instance.StartNextWave();
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

		private void OnPreparationReady()
		{
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

			playerActionMap?.Enable();
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
			ChangeState(GameState.Defeat);
			onGameOver?.Invoke();
			Time.timeScale = 0f;
		}

		private void Victory()
		{
			if (IsGameOver) return;

			ChangeState(GameState.Victory);
			onVictory?.Invoke();
		}

		public void RestartGame()
		{
			CancelInvoke(nameof(Defeat));
			Time.timeScale = 1f;
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
		}
	}
}
