using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TD
{
	public class GameManager : MonoBehaviour
	{
		private const string TOOLTIP_GAME_OVER_DELAY = "Delay in seconds before showing game over screen after base is destroyed";

		[SerializeField] private bool Logs = true;
		public static GameManager Instance { get; private set; }

		[SerializeField] private GameState currentState = GameState.Menu;
		private Base playerBase;
		[SerializeField] SerializedDictionary<GameState, GameObject> _stateGameObjecs = new();
		[Tooltip(TOOLTIP_GAME_OVER_DELAY)]
		[SerializeField] private float gameOverDelay = 2f;

		public UnityEvent<GameState> onGameStateChanged;
		public UnityEvent onGameStarted;
		public UnityEvent onGamePaused;
		public UnityEvent onGameResumed;
		public UnityEvent onGameOver;
		public UnityEvent onVictory;

		public GameState CurrentState => currentState;
		public bool IsPlaying => currentState == GameState.Playing;
		public bool IsPaused => currentState == GameState.Paused;
		public bool IsGameOver => currentState == GameState.GameOver || currentState == GameState.Victory;

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

			if (currentState == GameState.Menu)
			{
				StartGame();
			}
		}

		private void SetupEventListeners()
		{
			playerBase = FindAnyObjectByType<Base>();
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
			}
			else
			{
				Debug.LogError("WaveManager is null");
			}
		}

		public void StartGame()
		{
			ChangeState(GameState.Playing);
			onGameStarted?.Invoke();
			Time.timeScale = 1f;
		}

		public void PauseGame()
		{
			if (currentState != GameState.Playing) return;

			ChangeState(GameState.Paused);
			onGamePaused?.Invoke();
			Time.timeScale = 0f;
		}

		public void ResumeGame()
		{
			if (currentState != GameState.Paused) return;

			ChangeState(GameState.Playing);
			onGameResumed?.Invoke();
			Time.timeScale = 1f;
		}

		public void TogglePause()
		{
			if (IsPaused)
			{
				ResumeGame();
			}
			else if (IsPlaying)
			{
				PauseGame();
			}
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
			onGameResumed?.RemoveAllListeners();
			onGameOver?.RemoveAllListeners();
			onVictory?.RemoveAllListeners();
		}
	}

	public enum GameState
	{
		Menu,
		Playing,
		Paused,
		GameOver,
		Victory
	}
}