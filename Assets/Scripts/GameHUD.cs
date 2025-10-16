using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

namespace TD
{
    public class GameHUD : MonoBehaviour
    {
        [Required]
        [SerializeField] private TextMeshProUGUI currencyText;
        [SerializeField] private string currencyPrefix = "Gold: ";

        [Required]
        [SerializeField] private TextMeshProUGUI waveText;
        [Required]
        [SerializeField] private TextMeshProUGUI enemiesText;
        [SerializeField] private Slider waveProgressBar;

        [Required]
        [SerializeField] private TextMeshProUGUI baseHealthText;
        [SerializeField] private Slider baseHealthBar;
        [SerializeField] private Image baseHealthFill;
        [SerializeField] private Gradient healthColorGradient;

        [Required]
        [SerializeField] private Button startWaveButton;
        [Required]
        [SerializeField] private TextMeshProUGUI startWaveButtonText;

        [Required]
        [SerializeField] private GameObject gameOverPanel;
        [Required]
        [SerializeField] private TextMeshProUGUI gameOverText;
        [Required]
        [SerializeField] private Button restartButton;
        [Required]
        [SerializeField] private Button quitButton;

        [SerializeField] private CanvasGroup mainHUDGroup;
        private TowerPlacementSystem placementSystem;

        private void Start()
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
            var playerBase = FindFirstObjectByType<Base>();
            if (playerBase != null)
            {
                playerBase.onHealthChanged.AddListener(OnBaseHealthChanged);
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
            }
            else if (!shouldShow && mainHUDGroup.alpha > 0f)
            {
                mainHUDGroup.alpha = Mathf.Lerp(mainHUDGroup.alpha, 0f, Time.deltaTime * 10f);
                mainHUDGroup.interactable = false;
                mainHUDGroup.blocksRaycasts = false;
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
            var playerBase = FindFirstObjectByType<Base>();
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

        private void OnCurrencyChanged(int newCurrency)
        {
            UpdateCurrency();
        }

        private void OnWaveStarted(int waveNumber)
        {
            UpdateWaveDisplay();
        }

        private void OnWaveCompleted(int waveNumber)
        {
            UpdateWaveDisplay();
        }

        private void OnEnemySpawned(int totalSpawned)
        {
            UpdateWaveDisplay();
        }

        private void OnEnemyKilled(int remaining)
        {
            UpdateWaveDisplay();
        }

        private void OnBaseHealthChanged(int newHealth)
        {
            UpdateBaseHealth();
        }

        private void OnGameOver()
        {
            ShowGameOverPanel("Game Over!", "Your base has been destroyed!");
        }

        private void OnVictory()
        {
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

        private void OnStartWaveButtonClicked()
        {
            WaveManager.Instance?.StartNextWave();
        }

        private void OnRestartButtonClicked()
        {
            GameManager.Instance?.RestartGame();
        }

        private void OnQuitButtonClicked()
        {
            GameManager.Instance?.QuitGame();
        }

        private void OnDestroy()
        {
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

            var playerBase = FindFirstObjectByType<Base>();
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
