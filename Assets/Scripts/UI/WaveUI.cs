using System.Collections.Generic;
using TD.GameLoop;
using TD.Monsters;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
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
			var waveManager = WaveManager.Instance;
			if (waveManager == null) return;

			var gameManager = GameManager.Instance;
			bool canStart = gameManager != null && gameManager.CurrentState == GameState.Preparation;
			if (startWaveButton != null)
			{
				startWaveButton.gameObject.SetActive(canStart);
			}

			if (waveInfoText == null) return;

			if (waveManager.IsSpawning)
			{
				waveInfoText.text = GetLocalizedText(
					"wave.info.spawning",
					waveManager.CurrentWaveNumber,
					waveManager.TotalWaves,
					waveManager.EnemiesSpawned,
					waveManager.TotalEnemiesInWave);
			}
			else if (waveManager.IsWaveActive)
			{
				waveInfoText.text = GetLocalizedText(
					"wave.info.active",
					waveManager.CurrentWaveNumber,
					waveManager.TotalWaves,
					waveManager.EnemiesAlive);
			}
			else if (canStart)
			{
				waveInfoText.text = BuildUpcomingWaveInfo(waveManager.UpcomingWave);
			}
			else
			{
				waveInfoText.text = GetLocalizedText(
					"wave.info.preparing",
					waveManager.CurrentWaveNumber + 1,
					waveManager.TotalWaves);
			}
		}

		private string BuildUpcomingWaveInfo(WaveConfig waveConfig)
		{
			if (waveConfig == null)
				return GetLocalizedText("wave.intel.none");

			var lines = new List<string>
			{
				GetLocalizedText("wave.intel.header", waveConfig.WaveNumber, waveConfig.GetTotalEnemyCount())
			};

			foreach (var enemySpawn in waveConfig.EnemySpawns)
			{
				if (enemySpawn == null || enemySpawn.enemyPrefab == null)
					continue;

				if (!enemySpawn.enemyPrefab.TryGetComponent<MonsterStats>(out var stats) || stats.statsSO == null)
					continue;

				lines.Add(GetLocalizedText(
					"wave.intel.entry",
					enemySpawn.count,
					stats.statsSO.Role.GetLocalizedString(),
					stats.statsSO.DefensiveIdentity.GetLocalizedString()));
			}

			return string.Join("\n", lines);
		}

		private string GetLocalizedText(string key, params object[] arguments)
		{
			return new LocalizedString("UI", key)
			{
				Arguments = arguments
			}.GetLocalizedString();
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