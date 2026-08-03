using TD.GameLoop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TD.UI
{
	public class WaveUI : MonoBehaviour
	{
		[SerializeField] private Button startWaveButton;
		[SerializeField] private TMP_Text waveInfoText;

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
			bool canStart = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Preparation;
			startWaveButton.gameObject.SetActive(canStart);

			// Update wave info text
			if (waveInfoText != null)
			{
				if (WaveManager.Instance.IsSpawning)
				{
					waveInfoText.text = $"Wave {WaveManager.Instance.CurrentWaveNumber}/{WaveManager.Instance.TotalWaves} " +
					                    $"Spawning: {WaveManager.Instance.EnemiesSpawned}/{WaveManager.Instance.TotalEnemiesInWave}";
				}
				else if (WaveManager.Instance.IsWaveActive)
				{
					waveInfoText.text = $"Wave {WaveManager.Instance.CurrentWaveNumber}/{WaveManager.Instance.TotalWaves} " +
					                    $"Enemies Alive: {WaveManager.Instance.EnemiesAlive}";
				}
				else
				{
					string waveStatus = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Preparation
						? "Ready to start"
						: "Preparing";
					waveInfoText.text = $"{waveStatus} Wave {WaveManager.Instance.CurrentWaveNumber + 1}/{WaveManager.Instance.TotalWaves}";
				}
			}
		}

		private void OnStartWaveClicked()
		{
			GameManager.Instance?.StartNextWave();
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