using UnityEngine;
using UnityEngine.UI;

namespace TD
{
    public class WaveUI : MonoBehaviour
    {
        [SerializeField] private Button startWaveButton;
        [SerializeField] private Text waveInfoText;

        private void Start()
        {
            if (startWaveButton != null)
            {
                startWaveButton.onClick.AddListener(OnStartWaveClicked);
            }

            UpdateUI();
        }

        private void Update()
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (WaveManager.Instance == null) return;

            // Update button state
            if (startWaveButton != null)
            {
                startWaveButton.interactable = !WaveManager.Instance.IsWaveActive;
            }

            // Update wave info text
            if (waveInfoText != null)
            {
                if (WaveManager.Instance.IsSpawning)
                {
                    waveInfoText.text = $"Wave {WaveManager.Instance.CurrentWaveNumber}/{WaveManager.Instance.TotalWaves}\n" +
                                       $"Spawning: {WaveManager.Instance.EnemiesSpawned}/{WaveManager.Instance.TotalEnemiesInWave}";
                }
                else if (WaveManager.Instance.IsWaveActive)
                {
                    waveInfoText.text = $"Wave {WaveManager.Instance.CurrentWaveNumber}/{WaveManager.Instance.TotalWaves}\n" +
                                       $"Enemies Alive: {WaveManager.Instance.EnemiesAlive}";
                }
                else
                {
                    waveInfoText.text = $"Ready to start Wave {WaveManager.Instance.CurrentWaveNumber + 1}/{WaveManager.Instance.TotalWaves}";
                }
            }
        }

        private void OnStartWaveClicked()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.StartNextWave();
            }
        }

        private void OnDestroy()
        {
            if (startWaveButton != null)
            {
                startWaveButton.onClick.RemoveListener(OnStartWaveClicked);
            }
        }
    }
}
