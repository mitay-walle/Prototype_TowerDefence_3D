using System.Collections.Generic;
using TD.GameLoop;
using TD.Levels;
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
		[SerializeField] private GameObject tilePlacementPanel;
		[SerializeField] private TMP_Text tilePlacementTitleText;
		[SerializeField] private TMP_Text tilePlacementSummaryText;
		[SerializeField] private TilePlacementSystem tilePlacementSystem;
		[SerializeField] private GameplayTelemetry gameplayTelemetry;
		[SerializeField] private Button tilePreviousButton;
		[SerializeField] private Button tileNextButton;
		[SerializeField] private Button tileSubmitButton;
		[SerializeField] private Button tileCancelButton;

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

			if (tilePreviousButton != null)
				tilePreviousButton.onClick.AddListener(OnTilePreviousClicked);
			if (tileNextButton != null)
				tileNextButton.onClick.AddListener(OnTileNextClicked);
			if (tileSubmitButton != null)
				tileSubmitButton.onClick.AddListener(OnTileSubmitClicked);
			if (tileCancelButton != null)
				tileCancelButton.onClick.AddListener(OnTileCancelClicked);

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
			UpdateTilePlacement();

			var gameManager = GameManager.Instance;
			bool canStart = gameManager != null && gameManager.CurrentState == GameState.Preparation;
			bool isChallengeSelection = gameManager != null && gameManager.CurrentState == GameState.ChallengeSelection;
			bool canResolveChallenge = isChallengeSelection && waveManager.CanSelectChallengeModifier;
			if (startWaveButton != null)
			{
				startWaveButton.gameObject.SetActive(canStart || canResolveChallenge);
				startWaveButton.interactable = canStart || canResolveChallenge;
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
			else if (canStart || isChallengeSelection)
			{
				waveInfoText.text = BuildPreparationWaveInfo(waveManager);
			}
			else
			{
				var lastWaveCombatSummary = BuildLastWaveCombatSummary();
				waveInfoText.text = string.IsNullOrEmpty(lastWaveCombatSummary)
					? GetLocalizedText(
						"wave.info.preparing",
						waveManager.CurrentWaveNumber + 1,
						waveManager.TotalWaves)
					: lastWaveCombatSummary;
			}
		}

		private void UpdateTilePlacement()
		{
			if (tilePlacementSystem == null)
				tilePlacementSystem = FindAnyObjectByType<TilePlacementSystem>();

			bool showPanel = tilePlacementSystem != null && tilePlacementSystem.HasSelectedChoice;
			if (tilePlacementPanel != null)
				tilePlacementPanel.SetActive(showPanel);

			if (!showPanel)
				return;

			var choice = tilePlacementSystem.SelectedChoice;
			if (tilePlacementTitleText != null)
				tilePlacementTitleText.text = GetLocalizedText("wave.tile_choice.header");

			if (tilePlacementSummaryText != null)
			{
				tilePlacementSummaryText.text = GetLocalizedText(
					"wave.tile_choice.summary",
					tilePlacementSystem.SelectedChoiceIndex + 1,
					tilePlacementSystem.PlacementChoices.Count,
					choice.TileName,
					choice.Rotation * 90,
					choice.ConnectedNeighborCount,
					choice.OpenRoadEndCountBefore,
					choice.OpenRoadEndCountAfter,
					choice.AffectedOpenRoadEnds.Count,
					tilePlacementSystem.SelectedChoiceCoveredEntrancesBefore,
					tilePlacementSystem.SelectedChoiceTotalEntrancesBefore,
					tilePlacementSystem.SelectedChoiceCoveredEntrancesAfter,
					tilePlacementSystem.SelectedChoiceTotalEntrancesAfter);
			}

			if (tilePreviousButton != null)
			{
				UpdateTilePlacementButton(tilePreviousButton, "wave.tile_choice.previous");
				tilePreviousButton.interactable = tilePlacementSystem.PlacementChoices.Count > 1;
			}
			if (tileNextButton != null)
			{
				UpdateTilePlacementButton(tileNextButton, "wave.tile_choice.next");
				tileNextButton.interactable = tilePlacementSystem.PlacementChoices.Count > 1;
			}
			if (tileSubmitButton != null)
			{
				UpdateTilePlacementButton(tileSubmitButton, "wave.tile_choice.submit");
				tileSubmitButton.interactable = true;
			}
			if (tileCancelButton != null)
			{
				UpdateTilePlacementButton(tileCancelButton, "wave.tile_choice.cancel");
				tileCancelButton.interactable = true;
			}
		}

		private void UpdateTilePlacementButton(Button button, string key)
		{
			var buttonText = button.GetComponentInChildren<TMP_Text>(true);
			if (buttonText != null)
				buttonText.text = GetLocalizedText(key);
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
			var gameManager = GameManager.Instance;
			bool showPanel = gameManager != null &&
				gameManager.CurrentState == GameState.ChallengeSelection &&
				waveManager.CanSelectChallengeModifier;
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
				challengeModifierTitleText.text = GetLocalizedText("wave.challenge.header");
			}

			if (reinforcedHordeText != null)
				reinforcedHordeText.text = BuildChallengeModifierSummary(waveManager);

			if (reinforcedHordeButton != null)
			{
				reinforcedHordeButton.interactable = waveManager.CanSelectChallengeModifier;
			}
		}

		private string BuildChallengeModifierSummary(WaveManager waveManager)
		{
			var modifier = waveManager.PreviewChallengeModifier;
			return string.Concat(
				ChallengeModifierCatalog.GetDisplayName(modifier),
				"\nCount x", ChallengeModifierCatalog.GetEnemyCountFactor(modifier).ToString("F2"),
				"  HP x", ChallengeModifierCatalog.GetEnemyHealthFactor(modifier).ToString("F2"),
				"  Speed x", ChallengeModifierCatalog.GetEnemySpeedFactor(modifier).ToString("F2"),
				"  Reward x", ChallengeModifierCatalog.GetCompletionRewardFactor(modifier).ToString("F2"));
		}

		private string BuildUpcomingWaveInfo(WaveManager waveManager)
		{
			var waveConfig = waveManager.UpcomingWave;
			if (waveConfig == null)
				return GetLocalizedText("wave.intel.none");

			var lines = new List<string>
			{
				GetLocalizedText("wave.intel.header", waveConfig.WaveNumber, waveManager.GetUpcomingWaveTotalEnemyCount())
			};

			foreach (var enemySpawn in waveConfig.EnemySpawns)
			{
				if (enemySpawn == null || enemySpawn.enemyPrefab == null)
					continue;

				if (!enemySpawn.enemyPrefab.TryGetComponent<MonsterStats>(out var stats) || stats.statsSO == null)
					continue;

				lines.Add(GetLocalizedText(
					"wave.intel.entry",
					waveManager.GetUpcomingWaveEnemyCount(enemySpawn),
					stats.statsSO.Role.GetLocalizedString(),
					stats.statsSO.DefensiveIdentity.GetLocalizedString()));
			}

			return string.Join("\n", lines);
		}

		private string BuildLastWaveCombatSummary()
		{
			if (gameplayTelemetry == null || !gameplayTelemetry.TryGetLatestCompletedWaveCombat(
				out var wave,
				out var targetAcquisitions,
				out var towerFires,
				out var damageApplications,
				out var kills,
				out var leaks))
			{
				return string.Empty;
			}

			return GetLocalizedText(
				"wave.info.last_result",
				wave,
				kills,
				leaks,
				targetAcquisitions,
				towerFires,
				damageApplications);
		}

		private string BuildPreparationWaveInfo(WaveManager waveManager)
		{
			var upcomingWaveInfo = BuildUpcomingWaveInfo(waveManager);
			var lastWaveCombatSummary = BuildLastWaveCombatSummary();
			return string.IsNullOrEmpty(lastWaveCombatSummary)
				? upcomingWaveInfo
				: string.Concat(lastWaveCombatSummary, "\n", upcomingWaveInfo);
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
			var gameManager = GameManager.Instance;
			var waveManager = WaveManager.Instance;
			if (gameManager != null && gameManager.CurrentState == GameState.ChallengeSelection &&
				waveManager != null && waveManager.CanSelectChallengeModifier)
			{
				waveManager.ConfirmChallengeModifierPreview();
				return;
			}

			gameManager?.StartNextWave();
		}

		private void OnResourceCacheClicked()
		{
			var waveManager = WaveManager.Instance;
			waveManager?.SelectRewardOffer(waveManager.RewardOfferId, (int)RewardOfferChoice.ResourceCache);
		}

		private void OnEmergencyRepairsClicked()
		{
			var waveManager = WaveManager.Instance;
			waveManager?.SelectRewardOffer(waveManager.RewardOfferId, (int)RewardOfferChoice.EmergencyRepairs);
		}

		private void OnBountyContractClicked()
		{
			var waveManager = WaveManager.Instance;
			waveManager?.SelectRewardOffer(waveManager.RewardOfferId, (int)RewardOfferChoice.BountyContract);
		}

		private void OnReinforcedHordeClicked()
		{
			WaveManager.Instance?.CycleChallengeModifier();
		}

		private void OnTilePreviousClicked()
		{
			tilePlacementSystem?.SelectPreviousOption();
		}

		private void OnTileNextClicked()
		{
			tilePlacementSystem?.SelectNextOption();
		}

		private void OnTileSubmitClicked()
		{
			tilePlacementSystem?.PlaceTile();
		}

		private void OnTileCancelClicked()
		{
			tilePlacementSystem?.CancelPlacement();
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

			if (tilePreviousButton != null)
				tilePreviousButton.onClick.RemoveListener(OnTilePreviousClicked);
			if (tileNextButton != null)
				tileNextButton.onClick.RemoveListener(OnTileNextClicked);
			if (tileSubmitButton != null)
				tileSubmitButton.onClick.RemoveListener(OnTileSubmitClicked);
			if (tileCancelButton != null)
				tileCancelButton.onClick.RemoveListener(OnTileCancelClicked);
		}
	}
}
