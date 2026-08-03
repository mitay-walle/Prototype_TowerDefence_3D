using System.Collections.Generic;
using Sirenix.OdinInspector;
using TD.GameLoop;
using TD.Towers;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
		[SerializeField] private TextMeshProUGUI controlsText;
		[SerializeField] private InputActionAsset inputActions;
		[SerializeField] private GameObject pausePanel;
		[SerializeField] private GameObject settingsPanel;
		[SerializeField] private GameObject rebindPanel;
		[SerializeField] private Button resumeButton;
		[SerializeField] private Button settingsButton;
		[SerializeField] private Button rebindButton;
		[SerializeField] private Button backToPauseButton;
		[SerializeField] private Button backToSettingsButton;

		private TowerPlacementSystem placementSystem;
		private PlayerInput playerInput;
		private Component rebindActionUI;
		private Button triggerRebindButton;
		private Button resetRebindButton;
		private List<ConsoleMessage> consoleMessages = new List<ConsoleMessage>();

		public void Initialize()
		{
			placementSystem = FindFirstObjectByType<TowerPlacementSystem>();
			ResolvePauseControls();

			SetupEventListeners();
			UpdateUI();
			playerInput = FindFirstObjectByType<PlayerInput>();
			if (playerInput != null)
			{
				playerInput.onControlsChanged += OnControlsChanged;
			}

			InputSystem.onActionChange += OnActionChange;
			UpdateControlHints();

			if (gameOverPanel != null)
			{
				gameOverPanel.SetActive(false);
			}

			HidePausePanels();

			if (mainHUDGroup == null)
			{
				mainHUDGroup = GetComponent<CanvasGroup>();
			}

			Application.logMessageReceived += OnLogMessage;
			TowerShopUI.Initialize();
		}

		private void ResolvePauseControls()
		{
			var uiRoot = transform.parent == null ? null : transform.parent.Find("PauseMenuUI");
			if (uiRoot == null) return;

			if (pausePanel == null) pausePanel = uiRoot.Find("PausePanel")?.gameObject;
			if (settingsPanel == null) settingsPanel = uiRoot.Find("SettingsPanel")?.gameObject;
			if (rebindPanel == null) rebindPanel = uiRoot.Find("RebindPanel")?.gameObject;
			if (resumeButton == null) resumeButton = uiRoot.Find("PausePanel/ResumeButton")?.GetComponent<Button>();
			if (settingsButton == null) settingsButton = uiRoot.Find("PausePanel/SettingsButton")?.GetComponent<Button>();
			if (rebindButton == null) rebindButton = uiRoot.Find("SettingsPanel/RebindButton")?.GetComponent<Button>();
			if (backToPauseButton == null) backToPauseButton = uiRoot.Find("SettingsPanel/BackToPauseButton")?.GetComponent<Button>();
			if (backToSettingsButton == null) backToSettingsButton = uiRoot.Find("RebindPanel/BackToSettingsButton")?.GetComponent<Button>();

			var inputBindingSettings = uiRoot.Find("RebindPanel/InputBindingSettings");
			if (inputBindingSettings == null) return;

			if (rebindActionUI == null) rebindActionUI = inputBindingSettings.GetComponent("UnityEngine.InputSystem.Samples.RebindUI.RebindActionUI");
			if (triggerRebindButton == null) triggerRebindButton = inputBindingSettings.Find("TriggerRebindButton")?.GetComponent<Button>();
			if (resetRebindButton == null) resetRebindButton = inputBindingSettings.Find("ResetToDefaultButton")?.GetComponent<Button>();
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
				GameManager.Instance.onGamePaused.AddListener(OnGamePaused);
				GameManager.Instance.onGameUnpaused.AddListener(OnGameUnpaused);
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

			if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeButtonClicked);
			if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsButtonClicked);
			if (rebindButton != null) rebindButton.onClick.AddListener(OnRebindButtonClicked);
			if (backToPauseButton != null) backToPauseButton.onClick.AddListener(OnBackToPauseButtonClicked);
			if (backToSettingsButton != null) backToSettingsButton.onClick.AddListener(OnBackToSettingsButtonClicked);
			if (triggerRebindButton != null && rebindActionUI != null) triggerRebindButton.onClick.AddListener(OnStartRebindButtonClicked);
			if (resetRebindButton != null && rebindActionUI != null) resetRebindButton.onClick.AddListener(OnResetRebindButtonClicked);
		}

		private void Update()
		{
			UpdateWaveProgress();
			UpdateStartWaveButton();
			UpdateHUDVisibility();
			UpdateConsoleMessages();
		}

		private void UpdateControlHints()
		{
			if (controlsText == null || inputActions == null) return;

			var moveBinding = GetBindingDisplayString("Player/Move");
			var zoomBinding = GetBindingDisplayString("Player/Zoom");
			var rotateBinding = GetBindingDisplayString("UI/MiddleClick");
			var selectBinding = GetBindingDisplayString("UI/Click");
			controlsText.text =
				"Движение: " + moveBinding + "\n" +
				"Зум: " + zoomBinding + "\n" +
				"Поворот: " + rotateBinding + "\n" +
				"Выбор: " + selectBinding;
		}

		private string GetBindingDisplayString(string actionName)
		{
			var action = inputActions.FindAction(actionName, false);
			return action == null ? "—" : action.GetBindingDisplayString();
		}

		private void OnControlsChanged(PlayerInput changedPlayerInput) => UpdateControlHints();
		private void OnActionChange(object action, InputActionChange change)
		{
			if (change == InputActionChange.BoundControlsChanged)
			{
				UpdateControlHints();
			}
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
			if (startWaveButton == null || GameManager.Instance == null) return;

			bool canStart = GameManager.Instance.CurrentState == GameState.Preparation;
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
		private void OnGamePaused() => ShowPausePanel();
		private void OnGameUnpaused() => HidePausePanels();

		private void ShowPausePanel()
		{
			if (pausePanel != null) pausePanel.SetActive(true);
			if (settingsPanel != null) settingsPanel.SetActive(false);
			if (rebindPanel != null) rebindPanel.SetActive(false);
		}

		private void HidePausePanels()
		{
			if (pausePanel != null) pausePanel.SetActive(false);
			if (settingsPanel != null) settingsPanel.SetActive(false);
			if (rebindPanel != null) rebindPanel.SetActive(false);
		}

		private void ShowSettingsPanel()
		{
			if (pausePanel != null) pausePanel.SetActive(false);
			if (settingsPanel != null) settingsPanel.SetActive(true);
			if (rebindPanel != null) rebindPanel.SetActive(false);
		}

		private void ShowRebindPanel()
		{
			if (pausePanel != null) pausePanel.SetActive(false);
			if (settingsPanel != null) settingsPanel.SetActive(false);
			if (rebindPanel != null) rebindPanel.SetActive(true);
		}

		private void OnGameOver()
		{
			HidePausePanels();
			ShowGameOverPanel("Game Over!", "Your base has been destroyed!");
		}

		private void OnVictory()
		{
			HidePausePanels();
			ShowGameOverPanel("Victory!", "You have defended your base!");
		}

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

		private void OnStartWaveButtonClicked() => GameManager.Instance?.StartNextWave();
		private void OnRestartButtonClicked() => GameManager.Instance?.RestartGame();
		private void OnQuitButtonClicked() => GameManager.Instance?.QuitGame();
		private void OnResumeButtonClicked() => GameManager.Instance?.TogglePause();
		private void OnSettingsButtonClicked() => ShowSettingsPanel();
		private void OnRebindButtonClicked() => ShowRebindPanel();
		private void OnBackToPauseButtonClicked() => ShowPausePanel();
		private void OnBackToSettingsButtonClicked() => ShowSettingsPanel();
		private void OnStartRebindButtonClicked() => rebindActionUI?.SendMessage("StartInteractiveRebind", SendMessageOptions.DontRequireReceiver);
		private void OnResetRebindButtonClicked() => rebindActionUI?.SendMessage("ResetToDefault", SendMessageOptions.DontRequireReceiver);

		private void OnDestroy()
		{
			Application.logMessageReceived -= OnLogMessage;
			InputSystem.onActionChange -= OnActionChange;

			if (playerInput != null)
			{
				playerInput.onControlsChanged -= OnControlsChanged;
			}

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
				GameManager.Instance.onGamePaused.RemoveListener(OnGamePaused);
				GameManager.Instance.onGameUnpaused.RemoveListener(OnGameUnpaused);
			}

			if (resumeButton != null) resumeButton.onClick.RemoveListener(OnResumeButtonClicked);
			if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
			if (rebindButton != null) rebindButton.onClick.RemoveListener(OnRebindButtonClicked);
			if (backToPauseButton != null) backToPauseButton.onClick.RemoveListener(OnBackToPauseButtonClicked);
			if (backToSettingsButton != null) backToSettingsButton.onClick.RemoveListener(OnBackToSettingsButtonClicked);
			if (triggerRebindButton != null && rebindActionUI != null) triggerRebindButton.onClick.RemoveListener(OnStartRebindButtonClicked);
			if (resetRebindButton != null && rebindActionUI != null) resetRebindButton.onClick.RemoveListener(OnResetRebindButtonClicked);
		}
	}
}
