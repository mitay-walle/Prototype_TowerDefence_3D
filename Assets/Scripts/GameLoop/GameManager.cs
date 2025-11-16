using Sirenix.OdinInspector;
using TD.Plugins.Timing;
using TD.Towers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TD.GameLoop
{
	public class GameManager : MonoBehaviour
	{
		public static GameManager Instance { get; private set; }

		[SerializeField] private bool Logs = true;
		[SerializeField] private float gameOverDelay = 2f;
		[SerializeField] private GameState currentState = GameState.Initial;
		[SerializeField] private SerializedDictionary<GameState, GameObject> _stateGameObjecs = new();

		public UnityEvent<GameState> onGameStateChanged;
		public UnityEvent onGameStarted;
		public UnityEvent onGamePaused;
		public UnityEvent onGameUnpaused;
		public UnityEvent onGameOver;
		public UnityEvent onVictory;

		[ShowInInspector] private TimeControl TimeControl => TimeControl.Instance;

		public GameState CurrentState => currentState;
		public bool IsPlaying => currentState == PlayingState;
		public bool IsPaused => TimeControl.Instance.IsPaused;
		public bool IsGameOver => currentState == GameState.GameOver || currentState == GameState.Victory;

		private GameState PlayingState => WaveManager.Instance.IsWaveActive ? GameState.WaveActive : GameState.WavePreparing;
		private PlayerBase playerBase;

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
		}

		public void Initialize()
		{
			SetupEventListeners();

			if (currentState == GameState.Initial)
			{
				StartGame();
			}
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
				WaveManager.Instance.onWaveCompleted.AddListener(OnWaveCompleted);
				WaveManager.Instance.onWaveStarted.AddListener(OnWaveStarted);
			}
			else
			{
				Debug.LogError("WaveManager is null");
			}
		}

		void OnWaveStarted(int waveIndex)
		{
			ChangeState(GameState.WaveActive);
		}

		void OnWaveCompleted(int waveIndex)
		{
			ChangeState(GameState.WavePreparing);
		}

		void StartGame()
		{
			ChangeState(GameState.WavePreparing);
			onGameStarted?.Invoke();
			TimeControl.Instance.Pause.Remove(this);
		}

		void PauseGame()
		{
			if (IsPaused) return;

			ChangeState(GameState.Paused);

			onGamePaused?.Invoke();
			TimeControl.Instance.Pause.Add(this);
		}

		void UnpauseGame()
		{
			if (!IsPaused) return;

			TimeControl.Instance.Pause.Remove(this);
			ChangeState(PlayingState);
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
			if (Logs) Debug.Log("On Base Destroyed");
			Invoke(nameof(GameOver), gameOverDelay);
		}

		private void OnAllWavesCompleted()
		{
			Victory();
		}

		private void GameOver()
		{
			if (Logs) Debug.Log("GameOver");
			ChangeState(GameState.GameOver);
			onGameOver?.Invoke();
			Time.timeScale = 0f;
		}

		private void Victory()
		{
			ChangeState(GameState.Victory);
			onVictory?.Invoke();
		}

		public void RestartGame()
		{
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

			onGameStateChanged?.RemoveAllListeners();
			onGameStarted?.RemoveAllListeners();
			onGamePaused?.RemoveAllListeners();
			onGameUnpaused?.RemoveAllListeners();
			onGameOver?.RemoveAllListeners();
			onVictory?.RemoveAllListeners();
		}
	}
}