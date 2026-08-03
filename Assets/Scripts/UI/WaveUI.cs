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
		[SerializeField] private GameObject rewardOfferPanel;
		[SerializeField] private TMP_Text rewardOfferTitleText;
		[SerializeField] private Button resourceCacheButton;
		[SerializeField] private Button emergencyRepairsButton;
		[SerializeField] private Button bountyContractButton;
		[SerializeField] private GameObject challengeModifierPanel;
		[SerializeField] private TMP_Text challengeModifierTitleText;
		[SerializeField] private Button reinforcedHordeButton;

		private TMP_Text resourceCacheText;
		private TMP_Text emergencyRepairsText;
		private TMP_Text bountyContractText;
		private TMP_Text reinforcedHordeText;

		private void Start()
		{
			if (startWaveButton != null)
			{
				startWaveButton.onClick.AddListener(OnStartWaveClicked);
			}

			if (resourceCacheButton != null)
			{
				resourceCacheButton.onClick.AddListener(OnResourceCacheClicked);
				resourceCacheText = resourceCacheButton.GetComponentInChildren<TMP_Text>(true);
			}

			if (emergencyRepairsButton != null)
			{
				emergencyRepairsButton.onClick.AddListener(OnEmergencyRepairsClicked);
				emergencyRepairsText = emergencyRepairsButton.GetComponentInChildren<TMP_Text>(true);
			}

			if (bountyContractButton != null)
			{
				bountyContractButton.onClick.AddListener(OnBountyContractClicked);
				bountyContractText = bountyContractButton.GetComponentInChildren<TMP_Text>(true);
			}

			if (reinforcedHordeButton != null)
			{
				reinforcedHordeButton.onClick.AddListener(OnReinforcedHordeClicked);
				reinforcedHordeText = reinforcedHordeButton.GetComponentInChildren<TMP_Text>(true);
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

			UpdateRewardOffer(waveManager);
			UpdateChallengeModifier(waveManager);

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

		private void UpdateRewardOffer(WaveManager waveManager)
		{
			bool isPending = waveManager.IsRewardOfferPending;
			if (rewardOfferPanel != null)
			{
				rewardOfferPanel.SetActive(isPending);
			}

			if (!isPending)
			{
				return;
			}

			if (rewardOfferTitleText != null)
			{
				rewardOfferTitleText.text = GetLocalizedText("wave.reward.header");
			}

			if (resourceCacheText != null)
			{
				resourceCacheText.text = GetLocalizedText("wave.reward.resource_cache");
			}

			if (emergencyRepairsText != null)
			{
				emergencyRepairsText.text = GetLocalizedText("wave.reward.emergency_repairs");
			}

			if (bountyContractText != null)
			{
				bountyContractText.text = GetLocalizedText("wave.reward.bounty_contract");
			}

			if (resourceCacheButton != null)
			{
				resourceCacheButton.interactable = true;
			}

			if (emergencyRepairsButton != null)
			{
				emergencyRepairsButton.interactable = true;
			}

			if (bountyContractButton != null)
			{
				bountyContractButton.interactable = true;
			}
		}

		private void UpdateChallengeModifier(WaveManager waveManager)
		{
			bool isActive = waveManager.ActiveChallengeModifier == ChallengeModifier.ReinforcedHorde;
			bool showPanel = waveManager.CanSelectChallengeModifier || isActive;
			if (challengeModifierPanel != null)
			{
				challengeModifierPanel.SetActive(showPanel);
			}

			if (!showPanel)
			{
				return;
			}

			if (challengeModifierTitleText != null)
			{
				challengeModifierTitleText.text = GetLocalizedText(
					isActive ? "wave.challenge.active" : "wave.challenge.header");
			}

			if (reinforcedHordeText != null)
			{
				reinforcedHordeText.text = GetLocalizedText(
					isActive ? "wave.challenge.reinforced_horde_active" : "wave.challenge.reinforced_horde");
			}

			if (reinforcedHordeButton != null)
			{
				reinforcedHordeButton.interactable = waveManager.CanSelectChallengeModifier;
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

		private void OnResourceCacheClicked()
		{
			WaveManager.Instance?.SelectRewardOffer((int)RewardOfferChoice.ResourceCache);
		}

		private void OnEmergencyRepairsClicked()
		{
			WaveManager.Instance?.SelectRewardOffer((int)RewardOfferChoice.EmergencyRepairs);
		}

		private void OnBountyContractClicked()
		{
			WaveManager.Instance?.SelectRewardOffer((int)RewardOfferChoice.BountyContract);
		}

		private void OnReinforcedHordeClicked()
		{
			WaveManager.Instance?.SelectChallengeModifier(ChallengeModifier.ReinforcedHorde);
		}

		private void OnDestroy()
		{
			if (startWaveButton != null)
			{
				startWaveButton.onClick.RemoveListener(OnStartWaveClicked);
			}

			if (resourceCacheButton != null)
			{
				resourceCacheButton.onClick.RemoveListener(OnResourceCacheClicked);
			}

			if (emergencyRepairsButton != null)
			{
				emergencyRepairsButton.onClick.RemoveListener(OnEmergencyRepairsClicked);
			}

			if (bountyContractButton != null)
			{
				bountyContractButton.onClick.RemoveListener(OnBountyContractClicked);
			}

			if (reinforcedHordeButton != null)
			{
				reinforcedHordeButton.onClick.RemoveListener(OnReinforcedHordeClicked);
			}
		}
	}
}