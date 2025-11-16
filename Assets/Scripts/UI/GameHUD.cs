using System.Collections.Generic;
using Sirenix.OdinInspector;
using TD.GameLoop;
using TD.Towers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TD.UI
{
	public class GameHUD : MonoBehaviour
	{
		[Title("Screens")]
		[SerializeField, Required] private GameObject gameOverPanel;
		[SerializeField, Required] private TowerShopUI TowerShopUI;
		[SerializeField, Required] private GameObject BuildPanel;

		[Title("Other")]
		[SerializeField, Required] private TextMeshProUGUI currencyText;
		[SerializeField] private string currencyPrefix = "Gold: ";
		[SerializeField, Required] private TextMeshProUGUI waveText;
		[SerializeField, Required] private TextMeshProUGUI enemiesText;
		[SerializeField, Required] private Slider waveProgressBar;
		[SerializeField, Required] private TextMeshProUGUI baseHealthText;
		[SerializeField, Required] private Slider baseHealthBar;
		[SerializeField, Required] private Image baseHealthFill;
		[SerializeField, Required] private Gradient healthColorGradient;
		[SerializeField, Required] private Button startWaveButton;
		[SerializeField, Required] private TextMeshProUGUI startWaveButtonText;
		[SerializeField, Required] private TextMeshProUGUI gameOverText;
		[SerializeField, Required] private Button restartButton;
		[SerializeField, Required] private Button quitButton;
		[SerializeField, Required] private CanvasGroup mainHUDGroup;
		[SerializeField, Required] private TextMeshProUGUI consoleMessagesText;
		[SerializeField, Required] private int maxConsoleLines = 5;
		[SerializeField, Required] private float messageDisplayDuration = 5f;

		private TowerPlacementSystem placementSystem;
		private List<ConsoleMessage> consoleMessages = new List<ConsoleMessage>();

		public void Initialize()
		{
			placementSystem = FindFirstObjectByType<TowerPlacementSystem>();

			SetupEventListeners();
			UpdateUI();

			if (gameOverPanel != null)
			{
				gameOverPanel.SetActive(false);
			}

			if (mainHUDGroup == null)
			{
				mainHUDGroup = GetComponent<CanvasGroup>();
			}

			Application.logMessageReceived += OnLogMessage;
			TowerShopUI.Initialize();
		}

		private void SetupEventListeners()
		{
			// Resource Manager
			if (ResourceManager.Instance != null)
			{
				ResourceManager.Instance.onCurrencyChanged.AddListener(OnCurrencyChanged);
			}

			// Wave Manager
			if (WaveManager.Instance != null)
			{
				WaveManager.Instance.onWaveStarted.AddListener(OnWaveStarted);
				WaveManager.Instance.onWaveCompleted.AddListener(OnWaveCompleted);
				WaveManager.Instance.onEnemySpawned.AddListener(OnEnemySpawned);
				WaveManager.Instance.onEnemyKilled.AddListener(OnEnemyKilled);
			}

			// Base
			var playerBase = FindFirstObjectByType<PlayerBase>();
			if (playerBase != null)
			{
				playerBase.onHealthChanged.AddListener(OnBaseHealthChanged);
				OnBaseHealthChanged(playerBase.CurrentHealth);
			}
			else
			{
				Debug.LogError("Base not found");
			}

			// Game Manager
			if (GameManager.Instance != null)
			{
				GameManager.Instance.onGameOver.AddListener(OnGameOver);
				GameManager.Instance.onVictory.AddListener(OnVictory);
			}

			// Buttons
			if (startWaveButton != null)
			{
				startWaveButton.onClick.AddListener(OnStartWaveButtonClicked);
			}

			if (restartButton != null)
			{
				restartButton.onClick.AddListener(OnRestartButtonClicked);
			}

			if (quitButton != null)
			{
				quitButton.onClick.AddListener(OnQuitButtonClicked);
			}
		}

		private void Update()
		{
			UpdateWaveProgress();
			UpdateStartWaveButton();
			UpdateHUDVisibility();
			UpdateConsoleMessages();
		}

		private void UpdateConsoleMessages()
		{
			if (consoleMessages.Count == 0)
			{
				if (consoleMessagesText != null)
					consoleMessagesText.text = "";

				return;
			}

			for (int i = consoleMessages.Count - 1; i >= 0; i--)
			{
				consoleMessages[i].timeRemaining -= Time.deltaTime;
				if (consoleMessages[i].timeRemaining <= 0)
				{
					consoleMessages.RemoveAt(i);
				}
			}

			UpdateConsoleDisplay();
		}

		private void UpdateConsoleDisplay()
		{
			if (consoleMessagesText == null) return;

			int linesToShow = Mathf.Min(consoleMessages.Count, maxConsoleLines);
			int startIndex = consoleMessages.Count - linesToShow;

			var displayText = new System.Text.StringBuilder();
			for (int i = startIndex; i < consoleMessages.Count; i++)
			{
				displayText.AppendLine(consoleMessages[i].text);
			}

			consoleMessagesText.text = displayText.ToString().TrimEnd();
		}

		private void OnLogMessage(string condition, string stackTrace, LogType type)
		{
			if (type == LogType.Log)
			{
				AddConsoleMessage(condition);
			}
		}

		private void AddConsoleMessage(string message)
		{
			consoleMessages.Add(new ConsoleMessage(message, messageDisplayDuration));
		}

		private void UpdateHUDVisibility()
		{
			if (placementSystem == null || mainHUDGroup == null) return;

			bool shouldShow = !placementSystem.IsPlacing;

			if (shouldShow && mainHUDGroup.alpha < 1f)
			{
				mainHUDGroup.alpha = Mathf.Lerp(mainHUDGroup.alpha, 1f, Time.deltaTime * 10f);
				mainHUDGroup.interactable = true;
				mainHUDGroup.blocksRaycasts = true;
				BuildPanel.SetActive(false);
			}
			else if (!shouldShow && mainHUDGroup.alpha > 0f)
			{
				mainHUDGroup.alpha = Mathf.Lerp(mainHUDGroup.alpha, 0f, Time.deltaTime * 10f);
				mainHUDGroup.interactable = false;
				mainHUDGroup.blocksRaycasts = false;
				BuildPanel.SetActive(true);
			}
		}

		private void UpdateUI()
		{
			UpdateCurrency();
			UpdateWaveDisplay();
			UpdateBaseHealth();
		}

		private void UpdateCurrency()
		{
			if (currencyText != null && ResourceManager.Instance != null)
			{
				currencyText.text = $"{currencyPrefix}{ResourceManager.Instance.CurrentCurrency}";
			}
		}

		private void UpdateWaveDisplay()
		{
			if (WaveManager.Instance != null)
			{
				if (waveText != null)
				{
					waveText.text = $"Wave: {WaveManager.Instance.CurrentWaveNumber}/{WaveManager.Instance.TotalWaves}";
				}

				if (enemiesText != null)
				{
					enemiesText.text = $"Enemies: {WaveManager.Instance.EnemiesAlive}";
				}
			}
		}

		private void UpdateWaveProgress()
		{
			if (waveProgressBar != null && WaveManager.Instance != null)
			{
				waveProgressBar.value = WaveManager.Instance.WaveProgress;
			}
		}

		private void UpdateBaseHealth()
		{
			var playerBase = FindFirstObjectByType<PlayerBase>();
			if (playerBase == null) return;

			if (baseHealthText != null)
			{
				baseHealthText.text = $"HP: {playerBase.CurrentHealth}/{playerBase.MaxHealth}";
			}

			if (baseHealthBar != null)
			{
				baseHealthBar.value = playerBase.HealthPercent;
			}

			if (baseHealthFill != null && healthColorGradient != null)
			{
				baseHealthFill.color = healthColorGradient.Evaluate(playerBase.HealthPercent);
			}
		}

		private void UpdateStartWaveButton()
		{
			if (startWaveButton == null || WaveManager.Instance == null) return;

			bool canStart = !WaveManager.Instance.IsWaveActive;
			startWaveButton.interactable = canStart;

			if (startWaveButtonText != null)
			{
				startWaveButtonText.text = canStart ? "Start Wave" : "Wave Active";
			}
		}

		private void OnCurrencyChanged(int newCurrency) => UpdateCurrency();
		private void OnWaveStarted(int waveNumber) => UpdateWaveDisplay();
		private void OnWaveCompleted(int waveNumber) => UpdateWaveDisplay();
		private void OnEnemySpawned(int totalSpawned) => UpdateWaveDisplay();
		private void OnEnemyKilled(int remaining) => UpdateWaveDisplay();
		private void OnBaseHealthChanged(int newHealth) => UpdateBaseHealth();
		private void OnGameOver() => ShowGameOverPanel("Game Over!", "Your base has been destroyed!");
		private void OnVictory() => ShowGameOverPanel("Victory!", "You have defended your base!");

		private void ShowGameOverPanel(string title, string message)
		{
			if (gameOverPanel != null)
			{
				gameOverPanel.SetActive(true);
			}

			if (gameOverText != null)
			{
				gameOverText.text = $"{title}\n{message}";
			}
		}

		private void OnStartWaveButtonClicked() => WaveManager.Instance?.StartNextWave();
		private void OnRestartButtonClicked() => GameManager.Instance?.RestartGame();
		private void OnQuitButtonClicked() => GameManager.Instance?.QuitGame();

		private void OnDestroy()
		{
			Application.logMessageReceived -= OnLogMessage;

			if (ResourceManager.Instance != null)
			{
				ResourceManager.Instance.onCurrencyChanged.RemoveListener(OnCurrencyChanged);
			}

			if (WaveManager.Instance != null)
			{
				WaveManager.Instance.onWaveStarted.RemoveListener(OnWaveStarted);
				WaveManager.Instance.onWaveCompleted.RemoveListener(OnWaveCompleted);
				WaveManager.Instance.onEnemySpawned.RemoveListener(OnEnemySpawned);
				WaveManager.Instance.onEnemyKilled.RemoveListener(OnEnemyKilled);
			}

			var playerBase = FindFirstObjectByType<PlayerBase>();
			if (playerBase != null)
			{
				playerBase.onHealthChanged.RemoveListener(OnBaseHealthChanged);
			}

			if (GameManager.Instance != null)
			{
				GameManager.Instance.onGameOver.RemoveListener(OnGameOver);
				GameManager.Instance.onVictory.RemoveListener(OnVictory);
			}
		}
	}
}