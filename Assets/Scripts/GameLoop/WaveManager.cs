using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TD.MLAgents;
using TD.Monsters;
using TD.Levels;
using TD.Towers;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace TD.GameLoop
{
	public class WaveManager : MonoBehaviour
	{
		private const string TOOLTIP_LOOP_WAVES = "Restart from wave 1 after completing all waves with increased difficulty";
		private const string TOOLTIP_DIFFICULTY_SCALING = "Multiplier applied to enemy stats each loop (1.5 = 150% health/count per loop)";
		private const string TOOLTIP_RANDOMIZE_SPAWN = "Pick random spawn point for each enemy, otherwise use first spawn point";
		private const string TOOLTIP_AUTO_START = "Automatically start next wave after completion";
		private const string TOOLTIP_AUTO_DELAY = "Delay in seconds before auto-starting next wave";
		private const string TOOLTIP_DETAILED_LOGS = "Show detailed spawn logs for each enemy";

		public const int ResourceCacheAmount = 5;
		public const float EmergencyRepairTargetHealthFraction = 0.75f;
		public const int BountyContractBonus = 15;
		public const float ReinforcedHordeEnemyCountFactor = 1.25f;
		public const float ReinforcedHordeEnemyHealthFactor = 1.25f;
		public const float ReinforcedHordeCompletionRewardFactor = 1.5f;
		public const float AdaptiveEnemyFactorMinimum = 0.8f;
		public const float AdaptiveEnemyFactorMaximum = 1.2f;
		public const float AdaptiveSpeedFactorMinimum = 0.85f;
		public const float AdaptiveSpeedFactorMaximum = 1.15f;
		public const float AdaptiveRewardFactorMinimum = 0.8f;
		public const float AdaptiveRewardFactorMaximum = 1.2f;
		public const float EnemyLevelFactorMinimum = 0.75f;
		public const float EnemyLevelFactorMaximum = 1.35f;
		public const float EnemyLevelSpeedMinimum = 0.85f;
		public const float EnemyLevelSpeedMaximum = 1.25f;
		public const int GeneratedEnemySlotCount = 3;
		public const int GeneratedArchetypeActionSize = 5;
		public const int GeneratedCountActionSize = 6;
		public const int GeneratedPacingActionSize = 5;
		public const int GeneratedSeedActionSize = 8;
		public const float GeneratedWaveMaximumDamageFraction = 0.95f;
		public const float GeneratedWaveMaximumCombatSeconds = 75f;
		private const float MinimumSpawnSeparation = 1f;

		[SerializeField] private bool Logs = true;
		[Tooltip(TOOLTIP_DETAILED_LOGS)]
		[SerializeField] private bool detailedLogs = false;
		public static WaveManager Instance { get; private set; }

		[SerializeField] private List<WaveConfig> waves = new List<WaveConfig>();
		[Tooltip(TOOLTIP_LOOP_WAVES)]
		[SerializeField] private bool loopWaves = true;
		[Tooltip(TOOLTIP_DIFFICULTY_SCALING)]
		[SerializeField] private float difficultyScalingPerLoop = 1.5f;

		[SerializeField] private Transform[] spawnPoints;
		[SerializeField] private List<GameObject> enemyArchetypes = new List<GameObject>();
		[SerializeField] private GameObject enemyVisualGenerationBase;
		[Tooltip(TOOLTIP_RANDOMIZE_SPAWN)]
		[SerializeField] private bool randomizeSpawnPoint = true;

		[Tooltip(TOOLTIP_AUTO_START)]
		[SerializeField] private bool autoStartNextWave = false;
		[Tooltip(TOOLTIP_AUTO_DELAY)]
		[SerializeField] private float autoStartDelay = 3f;
		[SerializeField] private InputActionAsset inputActions;
		private PlayerBase targetBase;
		private TileMapManager tileMapManager;
		private TilePlacementSystem tilePlacementSystem;

		[SerializeField] private int currentWaveIndex = -1;
		[SerializeField] private int currentLoopCount = 0;
		[SerializeField] private int enemiesAlive = 0;
		[SerializeField] private int enemiesSpawned = 0;
		[SerializeField] private int totalEnemiesInWave = 0;
		private int enemiesKilled;
		private int enemiesLeaked;
		private readonly HashSet<int> registeredEnemyIds = new HashSet<int>();
		private readonly HashSet<int> terminalEnemyIds = new HashSet<int>();
		private int wavesCompleted;
		private float adaptiveEnemyHealthFactor = 1f;
		private float adaptiveEnemyCountFactor = 1f;
		private float adaptiveEnemySpeedFactor = 1f;
		private float adaptiveRewardFactor = 1f;
		private float enemyLevelHealthFactor = 1f;
		private float enemyLevelCountFactor = 1f;
		private float enemyLevelSpeedFactor = 1f;
		private WaveConfig pendingGeneratedWave;
		private WaveConfig activeWaveConfig;

		[ProgressBar(0, 1, ColorGetter = "GetSpawnProgressColor")]
		[ShowInInspector, ReadOnly]
		private float spawnProgress = 0f;

		[ShowInInspector, ReadOnly]
		private float timeUntilNextWave = 0f;

		[ShowInInspector, ReadOnly]
		private string currentWaveStatus = "Idle";
		private string interWaveFailureReason = string.Empty;

		public UnityEvent<int> onWaveStarted;
		public UnityEvent<int> onWaveCompleted;
		public UnityEvent<string> onRewardOfferCreated;
		public UnityEvent<int> onRewardSelected;
		public UnityEvent<int> onEnemySpawned;
		public UnityEvent<int> onEnemyKilled;
		public UnityEvent<int> onEnemyLeaked;
		public UnityEvent onAllWavesCompleted;
		public UnityEvent onChallengeModifierSelected;
		public UnityEvent onPreparationReady;

		private UniTask spawnTask;
		private CancellationTokenSource waveCancellationSource;
		private CancellationTokenSource interWaveCancellationSource;
		private bool isSpawning = false;
		private bool rewardOfferPending;
		private bool rewardOfferResolved;
		private string rewardOfferId = string.Empty;
		private string selectedRewardId = string.Empty;
		private int rewardOfferCreatedForWave = -1;
		private RewardOfferChoice selectedReward;
		private int nextCompletionRewardBonus;
		private int lastRewardCurrencyAmount;
		private int lastRewardBaseRepairAmount;
		private string lastSpawnedEnemyArchetype = "Unknown";
		private float lastSpawnedEnemyHealth;
		private float lastSpawnedEnemySpeed;
		private float lastSpawnedEnemyDelay;
		private int lastSpawnedEnemyGroupIndex;
		private int lastSpawnedEnemyIndex;
		private int lastSpawnedEnemyGroupTotal;
		private bool terminalRewardSuppressionLogged;
		private ChallengeModifier activeChallengeModifier;
		private ChallengeModifier previewChallengeModifier = ChallengeModifier.ReinforcedHorde;
		private bool challengeModifierResolved;
		private InputAction startWaveAction;

		public int CurrentWaveNumber => currentWaveIndex + 1;
		public int TotalWaves => waves.Count;
		public bool AutoStartNextWave => autoStartNextWave;
		public bool IsSpawning => isSpawning;
		public bool IsWaveActive => enemiesAlive > 0 || isSpawning;
		public bool IsRewardOfferPending => rewardOfferPending;
		public bool HasSelectedReward => rewardOfferResolved;
		public string RewardOfferId => rewardOfferId;
		public string SelectedRewardId => selectedRewardId;
		public int RewardOfferCreatedForWave => rewardOfferCreatedForWave;
		public RewardOfferChoice SelectedReward => selectedReward;
		public int LastRewardCurrencyAmount => lastRewardCurrencyAmount;
		public int LastRewardBaseRepairAmount => lastRewardBaseRepairAmount;
		public string LastSpawnedEnemyArchetype => lastSpawnedEnemyArchetype;
		public float LastSpawnedEnemyHealth => lastSpawnedEnemyHealth;
		public float LastSpawnedEnemySpeed => lastSpawnedEnemySpeed;
		public float LastSpawnedEnemyDelay => lastSpawnedEnemyDelay;
		public int LastSpawnedEnemyGroupIndex => lastSpawnedEnemyGroupIndex;
		public int LastSpawnedEnemyIndex => lastSpawnedEnemyIndex;
		public int LastSpawnedEnemyGroupTotal => lastSpawnedEnemyGroupTotal;
		public string InterWaveFailureReason => interWaveFailureReason;
		public ChallengeModifier ActiveChallengeModifier => activeChallengeModifier;
		public ChallengeModifier PreviewChallengeModifier => previewChallengeModifier;
		public int ChallengeModifierOptionCount => ChallengeModifierCatalog.SelectableCount;
		public bool CanSelectChallengeModifier => GameManager.Instance != null &&
			GameManager.Instance.CurrentState == GameState.ChallengeSelection &&
			currentWaveIndex < 0 && !challengeModifierResolved;
		public float EnemyCountFactor => ChallengeModifierCatalog.GetEnemyCountFactor(activeChallengeModifier);
		public float EnemyHealthFactor => ChallengeModifierCatalog.GetEnemyHealthFactor(activeChallengeModifier);
		public float EnemySpeedFactor => ChallengeModifierCatalog.GetEnemySpeedFactor(activeChallengeModifier);
		public float CompletionRewardFactor => ChallengeModifierCatalog.GetCompletionRewardFactor(activeChallengeModifier);
		public int EnemiesAlive => enemiesAlive;
		public int EnemiesKilled => enemiesKilled;
		public int EnemiesLeaked => enemiesLeaked;
		public int WavesCompleted => wavesCompleted;
		public float AdaptiveEnemyHealthFactor => adaptiveEnemyHealthFactor;
		public float AdaptiveEnemyCountFactor => adaptiveEnemyCountFactor;
		public float AdaptiveEnemySpeedFactor => adaptiveEnemySpeedFactor;
		public float AdaptiveRewardFactor => adaptiveRewardFactor;
		public float EnemyLevelHealthFactor => enemyLevelHealthFactor;
		public float EnemyLevelCountFactor => enemyLevelCountFactor;
		public float EnemyLevelSpeedFactor => enemyLevelSpeedFactor;
		public IReadOnlyList<GameObject> EnemyArchetypes => enemyArchetypes;
		public GameObject EnemyVisualGenerationBase => enemyVisualGenerationBase;
		public WaveConfig ActiveWaveConfig => activeWaveConfig;
		public WaveConfig PendingGeneratedWave => pendingGeneratedWave;
		public bool HasPendingGeneratedWave => GetTrackedGeneratedWave() != null;
		public int GeneratedWaveGroupCount => GetTrackedGeneratedWave() != null ? GetTrackedGeneratedWave().EnemySpawns.Count : 0;
		public int GeneratedWaveSeed => GetTrackedGeneratedWave() != null ? GetTrackedGeneratedWave().GenerationSeed : 0;
		public float GeneratedWavePredictedDamageFraction => GetTrackedGeneratedWave() != null && targetBase != null
			? GetTrackedGeneratedWave().PredictedBaseDamage / Mathf.Max(1f, targetBase.MaxHealth)
			: 0f;
		public float GeneratedWavePredictedCombatSeconds => GetTrackedGeneratedWave() != null
			? GetTrackedGeneratedWave().PredictedCombatSeconds
			: 0f;
		public float GeneratedWaveAppliedAdaptiveEnemyHealthFactor => GetTrackedGeneratedWave() != null
			? GetTrackedGeneratedWave().AppliedAdaptiveEnemyHealthFactor
			: 1f;
		public float GeneratedWaveAppliedAdaptiveEnemyCountFactor => GetTrackedGeneratedWave() != null
			? GetTrackedGeneratedWave().AppliedAdaptiveEnemyCountFactor
			: 1f;
		public float GeneratedWaveAppliedAdaptiveEnemySpeedFactor => GetTrackedGeneratedWave() != null
			? GetTrackedGeneratedWave().AppliedAdaptiveEnemySpeedFactor
			: 1f;
		public float GeneratedWaveAppliedAdaptiveRewardFactor => GetTrackedGeneratedWave() != null
			? GetTrackedGeneratedWave().AppliedAdaptiveRewardFactor
			: 1f;
		public float GeneratedWaveTensionScore => GetTrackedGeneratedWave() != null ? GetTrackedGeneratedWave().TensionScore : 0f;
		public bool CanGenerateEnemyLevel => CanApplyEnemyLevel && enemyArchetypes != null && enemyArchetypes.Count > 0 && enemyVisualGenerationBase != null;

		private WaveConfig GetTrackedGeneratedWave()
		{
			if (pendingGeneratedWave != null)
				return pendingGeneratedWave;

			return activeWaveConfig != null && activeWaveConfig.GeneratedByMl ? activeWaveConfig : null;
		}
		public float EnemyLevelDifficultyScore =>
			(Mathf.InverseLerp(EnemyLevelFactorMinimum, EnemyLevelFactorMaximum, enemyLevelHealthFactor) +
			 Mathf.InverseLerp(EnemyLevelFactorMinimum, EnemyLevelFactorMaximum, enemyLevelCountFactor) +
			 Mathf.InverseLerp(EnemyLevelSpeedMinimum, EnemyLevelSpeedMaximum, enemyLevelSpeedFactor)) / 3f;
		public float AdaptiveDifficultyScore =>
			(Mathf.InverseLerp(AdaptiveEnemyFactorMinimum, AdaptiveEnemyFactorMaximum, adaptiveEnemyHealthFactor) +
			 Mathf.InverseLerp(AdaptiveEnemyFactorMinimum, AdaptiveEnemyFactorMaximum, adaptiveEnemyCountFactor) +
			 Mathf.InverseLerp(AdaptiveSpeedFactorMinimum, AdaptiveSpeedFactorMaximum, adaptiveEnemySpeedFactor) +
			 Mathf.InverseLerp(AdaptiveRewardFactorMaximum, AdaptiveRewardFactorMinimum, adaptiveRewardFactor)) / 4f;
		public bool CanApplyAdaptiveBalance => !IsWaveActive && !rewardOfferPending;
		public bool CanApplyEnemyLevel => !IsWaveActive && !rewardOfferPending;

		public void SelectChallengeModifier(ChallengeModifier modifier)
		{
			if (!CanSelectChallengeModifier ||
				!ChallengeModifierCatalog.IsSelectable(modifier))
				return;

			activeChallengeModifier = modifier;
			previewChallengeModifier = modifier;
			challengeModifierResolved = true;

			if (Logs)
				Debug.Log(
					$"[WaveManager] Challenge modifier selected: {modifier};" +
					$"count={EnemyCountFactor:F2};health={EnemyHealthFactor:F2};" +
					$"speed={EnemySpeedFactor:F2};reward={CompletionRewardFactor:F2}");
			onChallengeModifierSelected?.Invoke();
		}

		public void CycleChallengeModifier()
		{
			if (!CanSelectChallengeModifier)
				return;

			previewChallengeModifier = ChallengeModifierCatalog.GetNext(previewChallengeModifier);
			if (Logs)
				Debug.Log($"[WaveManager] Challenge modifier preview: {previewChallengeModifier}");
		}

		public bool ConfirmChallengeModifierPreview()
		{
			if (!CanSelectChallengeModifier)
				return false;

			SelectChallengeModifier(previewChallengeModifier);
			return true;
		}

		public bool ApplyAdaptiveBalance(float enemyHealthFactor, float enemyCountFactor, float enemySpeedFactor, float rewardFactor)
		{
			if (!CanApplyAdaptiveBalance)
				return false;

			adaptiveEnemyHealthFactor = Mathf.Clamp(enemyHealthFactor, AdaptiveEnemyFactorMinimum, AdaptiveEnemyFactorMaximum);
			adaptiveEnemyCountFactor = Mathf.Clamp(enemyCountFactor, AdaptiveEnemyFactorMinimum, AdaptiveEnemyFactorMaximum);
			adaptiveEnemySpeedFactor = Mathf.Clamp(enemySpeedFactor, AdaptiveSpeedFactorMinimum, AdaptiveSpeedFactorMaximum);
			adaptiveRewardFactor = Mathf.Clamp(rewardFactor, AdaptiveRewardFactorMinimum, AdaptiveRewardFactorMaximum);

			if (Logs)
				Debug.Log($"[WaveManager] Adaptive balance applied: health={adaptiveEnemyHealthFactor:F2}, count={adaptiveEnemyCountFactor:F2}, speed={adaptiveEnemySpeedFactor:F2}, reward={adaptiveRewardFactor:F2}");

			return true;
		}

		public bool ApplyEnemyLevel(float healthFactor, float countFactor, float speedFactor)
		{
			if (!CanApplyEnemyLevel)
				return false;

			enemyLevelHealthFactor = Mathf.Clamp(healthFactor, EnemyLevelFactorMinimum, EnemyLevelFactorMaximum);
			enemyLevelCountFactor = Mathf.Clamp(countFactor, EnemyLevelFactorMinimum, EnemyLevelFactorMaximum);
			enemyLevelSpeedFactor = Mathf.Clamp(speedFactor, EnemyLevelSpeedMinimum, EnemyLevelSpeedMaximum);

			if (Logs)
				Debug.Log($"[WaveManager] Enemy level applied: health={enemyLevelHealthFactor:F2}, count={enemyLevelCountFactor:F2}, speed={enemyLevelSpeedFactor:F2}");

			return true;
		}

		public bool ApplyEnemyLevelGeneration(
			float healthFactor,
			float countFactor,
			float speedFactor,
			int seed,
			int[] archetypeActions,
			int[] countActions,
			int pacingAction)
		{
			if (!CanGenerateEnemyLevel || seed == 0 || archetypeActions == null || countActions == null ||
				archetypeActions.Length < GeneratedEnemySlotCount || countActions.Length < GeneratedEnemySlotCount)
				return false;

			var generatedWave = BuildGeneratedWave(
				Mathf.Clamp(healthFactor, EnemyLevelFactorMinimum, EnemyLevelFactorMaximum),
				Mathf.Clamp(countFactor, EnemyLevelFactorMinimum, EnemyLevelFactorMaximum),
				Mathf.Clamp(speedFactor, EnemyLevelSpeedMinimum, EnemyLevelSpeedMaximum),
				seed,
				archetypeActions,
				countActions,
				Mathf.Clamp(pacingAction, 0, GeneratedPacingActionSize - 1));
			if (generatedWave == null)
				return false;

			enemyLevelHealthFactor = healthFactor;
			enemyLevelCountFactor = countFactor;
			enemyLevelSpeedFactor = speedFactor;
			pendingGeneratedWave = generatedWave;

			if (Logs)
				Debug.Log($"[WaveManager] Generated enemy wave saved: {generatedWave.WaveName}, seed={generatedWave.GenerationSeed}, groups={generatedWave.EnemySpawns.Count}, predicted damage={generatedWave.PredictedBaseDamage:F1}, combat={generatedWave.PredictedCombatSeconds:F1}s, tension={generatedWave.TensionScore:F2}");

			return true;
		}

		private WaveConfig BuildGeneratedWave(
			float proposedHealthFactor,
			float proposedCountFactor,
			float proposedSpeedFactor,
			int seed,
			int[] archetypeActions,
			int[] countActions,
			int pacingAction)
		{
			if (targetBase == null || targetBase.CurrentHealth <= 0 || enemyVisualGenerationBase == null)
				return null;

			var lastSlot = -1;
			for (var slot = 0; slot < GeneratedEnemySlotCount; slot++)
			{
				if (IsGeneratedArchetypeEnabled(archetypeActions[slot]))
					lastSlot = slot;
			}

			var generatedSpawns = new List<EnemySpawnData>();
			var isClimax = false;
			for (var slot = 0; slot < GeneratedEnemySlotCount; slot++)
			{
				var archetypeIndex = archetypeActions[slot];
				if (!IsGeneratedArchetypeEnabled(archetypeIndex) || archetypeIndex >= enemyArchetypes.Count)
					continue;

				var statSourcePrefab = enemyArchetypes[archetypeIndex];
				if (statSourcePrefab == null)
					return null;

				var roleSeed = unchecked(seed + (slot + 1) * 7919 + archetypeIndex * 104729);
				if (roleSeed == 0)
					roleSeed = 1;

				if (!EnemyVisualAssetGenerator.TryCreateSavedEnemyPrefab(
					enemyVisualGenerationBase,
					statSourcePrefab,
					roleSeed,
					archetypeIndex,
					out var generatedPrefab))
					return null;

				var slotIsClimax = pacingAction >= 3 && slot == lastSlot;
				isClimax |= slotIsClimax;
				var pacing = pacingAction / (float)(GeneratedPacingActionSize - 1);
				var spawnDelay = Mathf.Lerp(1.8f, 0.55f, pacing);
				var healthMultiplier = Mathf.Lerp(0.9f, 1.1f, pacing);
				var speedMultiplier = Mathf.Lerp(0.9f, 1.05f, pacing);
				if (slotIsClimax)
				{
					spawnDelay = 0.2f;
					healthMultiplier *= Mathf.Lerp(1.1f, 1.3f, pacing);
					speedMultiplier *= Mathf.Lerp(1.05f, 1.2f, pacing);
				}

				generatedSpawns.Add(new EnemySpawnData
				{
					enemyPrefab = generatedPrefab,
					count = Mathf.Clamp(countActions[slot] + 1, 1, GeneratedCountActionSize),
					spawnDelay = spawnDelay,
					healthMultiplier = healthMultiplier,
					speedMultiplier = speedMultiplier
				});
			}

			if (generatedSpawns.Count == 0)
				return null;

			var maximumDamage = targetBase.CurrentHealth * GeneratedWaveMaximumDamageFraction;
			var countFactor = EnemyCountFactor * adaptiveEnemyCountFactor * proposedCountFactor;
			var countScaling = 1f;
			var predictedDamage = GetPredictedBaseDamage(generatedSpawns, countScaling, countFactor);
			while (predictedDamage > maximumDamage && generatedSpawns.Count > 1)
			{
				var reducibleIndex = FindHighestDamageSpawn(generatedSpawns, countFactor);
				if (reducibleIndex < 0)
					break;

				if (generatedSpawns[reducibleIndex].count > 1)
					generatedSpawns[reducibleIndex].count--;
				else
					generatedSpawns.RemoveAt(reducibleIndex);

				predictedDamage = GetPredictedBaseDamage(generatedSpawns, countScaling, countFactor);
			}

			if (predictedDamage > maximumDamage)
			{
				for (var step = 19; step >= 1 && predictedDamage > maximumDamage; step--)
				{
					countScaling = step / 20f;
					predictedDamage = GetPredictedBaseDamage(generatedSpawns, countScaling, countFactor);
				}
			}

			if (predictedDamage > maximumDamage)
				return null;

			var estimatedPlayerDps = GetEstimatedPlayerDps();
			if (estimatedPlayerDps <= 0f)
				return null;

			var combatHealthBudget = estimatedPlayerDps * GeneratedWaveMaximumCombatSeconds;
			var predictedCombatHealth = GetPredictedCombatHealth(
				generatedSpawns,
				countScaling,
				countFactor,
				proposedHealthFactor);
			if (predictedCombatHealth > combatHealthBudget)
			{
				for (var step = 19; step >= 1 && predictedCombatHealth > combatHealthBudget; step--)
				{
					countScaling = step / 20f;
					predictedDamage = GetPredictedBaseDamage(generatedSpawns, countScaling, countFactor);
					predictedCombatHealth = GetPredictedCombatHealth(
						generatedSpawns,
						countScaling,
						countFactor,
						proposedHealthFactor);
				}
			}

			if (predictedCombatHealth > combatHealthBudget)
			{
				var healthScale = combatHealthBudget / Mathf.Max(1f, predictedCombatHealth);
				foreach (var spawn in generatedSpawns)
					spawn.healthMultiplier = Mathf.Max(0.1f, spawn.healthMultiplier * healthScale);

				predictedCombatHealth = GetPredictedCombatHealth(
					generatedSpawns,
					countScaling,
					countFactor,
					proposedHealthFactor);
			}

			if (predictedCombatHealth > combatHealthBudget)
				return null;

			var predictedCombatSeconds = predictedCombatHealth / estimatedPlayerDps;

			var safetyMargin = 1f - predictedDamage / Mathf.Max(1f, targetBase.CurrentHealth);
			var tensionScore = isClimax
				? Mathf.Clamp01(0.6f + pacingAction / (float)(GeneratedPacingActionSize - 1) * 0.4f)
				: Mathf.Clamp01(pacingAction / (float)(GeneratedPacingActionSize - 1) * 0.5f);
			var wave = WaveConfig.CreateGenerated(
				$"Wave_ML_{seed}",
				CurrentWaveNumber + 1,
				generatedSpawns,
				Mathf.Lerp(4f, 1f, pacingAction / (float)(GeneratedPacingActionSize - 1)),
				Mathf.RoundToInt(25f + predictedDamage),
				1f,
				countScaling,
				seed,
				predictedDamage,
				safetyMargin,
				tensionScore,
				predictedCombatSeconds,
				adaptiveEnemyHealthFactor,
				adaptiveEnemyCountFactor,
				adaptiveEnemySpeedFactor,
				adaptiveRewardFactor);
			if (wave == null || !SaveGeneratedWaveAsset(wave))
			{
				if (wave != null)
				{
					if (Application.isPlaying)
						Destroy(wave);
					else
						DestroyImmediate(wave);
				}
				return null;
			}

			return wave;
		}

		private static bool IsGeneratedArchetypeEnabled(int action)
		{
			return action >= 0 && action < GeneratedArchetypeActionSize - 1;
		}

		private float GetPredictedBaseDamage(List<EnemySpawnData> spawns, float countScaling, float countFactor)
		{
			var totalDamage = 0f;
			foreach (var spawn in spawns)
			{
				var damage = GetBaseEnemyDamage(spawn.enemyPrefab);
				var count = Mathf.Max(1, Mathf.RoundToInt(spawn.count * countScaling * countFactor));
				totalDamage += damage * count;
			}

			return totalDamage;
		}

		private float GetPredictedCombatHealth(
			List<EnemySpawnData> spawns,
			float countScaling,
			float countFactor,
			float healthFactor)
		{
			var totalHealth = 0f;
			var loopHealthFactor = Mathf.Pow(difficultyScalingPerLoop, currentLoopCount);
			var challengeHealthFactor = EnemyHealthFactor * adaptiveEnemyHealthFactor * healthFactor;
			foreach (var spawn in spawns)
			{
				var baseHealth = GetBaseEnemyHealth(spawn.enemyPrefab);
				if (baseHealth <= 0f)
					return float.PositiveInfinity;

				var count = Mathf.Max(1, Mathf.RoundToInt(spawn.count * countScaling * loopHealthFactor * countFactor));
				totalHealth += baseHealth * Mathf.Max(0f, spawn.healthMultiplier) * loopHealthFactor * challengeHealthFactor * count;
			}

			return totalHealth;
		}

		private float GetEstimatedPlayerDps()
		{
			var estimatedDps = 0f;
			var towers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
			foreach (var tower in towers)
			{
				if (tower == null || tower.Stats == null)
					continue;

				estimatedDps += Mathf.Max(0f, tower.Stats.Damage.Value) * Mathf.Max(0f, tower.Stats.FireRate.Value);
			}

			var currency = ResourceManager.Instance != null ? ResourceManager.Instance.CurrentCurrency : 0;
			var towerStats = Resources.LoadAll<TowerStatsSO>("TowerStats");
			var bestAffordableDps = 0f;
			foreach (var stats in towerStats)
			{
				if (stats == null || stats.Cost > currency)
					continue;

				bestAffordableDps = Mathf.Max(bestAffordableDps, stats.Damage.BaseValue * stats.FireRate.BaseValue);
			}

			return estimatedDps + bestAffordableDps;
		}

		private static int FindHighestDamageSpawn(List<EnemySpawnData> spawns, float countFactor)
		{
			var index = -1;
			var highestDamage = float.MinValue;
			for (var i = 0; i < spawns.Count; i++)
			{
				var damage = GetBaseEnemyDamage(spawns[i].enemyPrefab) * spawns[i].count * countFactor;
				if (damage <= highestDamage)
					continue;

				highestDamage = damage;
				index = i;
			}

			return index;
		}

		private static int GetBaseEnemyDamage(GameObject enemyPrefab)
		{
			var stats = enemyPrefab != null ? enemyPrefab.GetComponent<MonsterStats>() : null;
			return stats != null && stats.statsSO != null ? Mathf.Max(0, stats.statsSO.Damage.BaseValueInt) : 0;
		}

		private static float GetBaseEnemyHealth(GameObject enemyPrefab)
		{
			var health = enemyPrefab != null ? enemyPrefab.GetComponent<MonsterHealth>() : null;
			return health != null ? Mathf.Max(0f, health.MaxHealth) : 0f;
		}

		private static bool SaveGeneratedWaveAsset(WaveConfig wave)
		{
#if UNITY_EDITOR
			const string folder = "Assets/Resources/WaveConfigs/Generated";
			if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
			{
				if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources/WaveConfigs"))
					return false;

				UnityEditor.AssetDatabase.CreateFolder("Assets/Resources/WaveConfigs", "Generated");
			}

			if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
				return false;

			var assetPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath($"{folder}/{wave.name}.asset");
			UnityEditor.AssetDatabase.CreateAsset(wave, assetPath);
			UnityEditor.AssetDatabase.SaveAssets();
			return UnityEditor.AssetDatabase.Contains(wave);
#else
			return false;
#endif
		}

		public void RecordGeneratedWaveEvaluation(GameplayEvaluationMetrics evaluation, bool victory, bool defeat)
		{
			var normalizedScore = Mathf.Clamp(evaluation.BalanceReward, -1f, 1f);
			var generatedWave = GetTrackedGeneratedWave();
			if (generatedWave == null)
				return;

			generatedWave.RecordGenerationEvaluation(
				normalizedScore,
				evaluation.BaseHealthFraction,
				victory,
				defeat);
		}

		public int EnemiesSpawned => enemiesSpawned;
		public int TotalEnemiesInWave => totalEnemiesInWave;
		public float WaveProgress => totalEnemiesInWave > 0 ? (float)enemiesSpawned / totalEnemiesInWave : 0f;
		public WaveConfig UpcomingWave
		{
			get
			{
				if (pendingGeneratedWave != null)
					return pendingGeneratedWave;

				if (waves == null || waves.Count == 0)
					return null;

				var nextWaveIndex = currentWaveIndex + 1;
				if (nextWaveIndex >= waves.Count)
				{
					if (!loopWaves)
						return null;

					nextWaveIndex = 0;
				}

				return waves[nextWaveIndex];
			}
		}

		private Color GetSpawnProgressColor()
		{
			if (spawnProgress < 0.33f) return Color.red;
			if (spawnProgress < 0.66f) return Color.yellow;

			return Color.green;
		}

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				if (Application.isPlaying)
					Destroy(gameObject);
				else
					DestroyImmediate(gameObject);
				return;
			}

			Instance = this;

			if (inputActions != null)
			{
				startWaveAction = inputActions.FindAction("Player/Start Wave", true);
			}
		}

		private void OnEnable()
		{
			if (startWaveAction == null) return;

			startWaveAction.Enable();
			startWaveAction.performed += OnStartWaveInput;
		}

		private void OnDisable()
		{
			if (startWaveAction != null) startWaveAction.performed -= OnStartWaveInput;
		}

		private void OnStartWaveInput(InputAction.CallbackContext context)
		{
			if (context.performed)
			{
				GameManager.Instance?.StartNextWave();
			}
		}

		private void Start()
		{
			ValidateSpawnPoints();
		}

		private void ValidateSpawnPoints()
		{
			if (spawnPoints == null || spawnPoints.Length == 0)
			{
				Debug.LogError("WaveManager: No validated spawn points assigned; waiting for GameplayBootstrap.");
				spawnPoints = new Transform[0];
			}
		}

		public void Initialize(
			List<WaveConfig> waveConfigs,
			Transform[] spawners,
			PlayerBase baseTarget = null,
			TileMapManager mapOwner = null,
			TilePlacementSystem tileOwner = null)
		{
			ResetRuntimeState();
			targetBase = baseTarget;
			tileMapManager = mapOwner;
			tilePlacementSystem = tileOwner;

			if (waveConfigs != null && waveConfigs.Count > 0)
			{
				waves = waveConfigs;
			}

			if (spawners != null && spawners.Length > 0)
			{
				spawnPoints = spawners;
			}

			if (Logs)
			{
				Debug.Log(
					$"[WaveManager] Initialized with {waves.Count} waves and {spawnPoints?.Length ?? 0} spawn points; " +
					$"mapOwner={tileMapManager?.name ?? "null"}, tileOwner={tilePlacementSystem?.name ?? "null"}");
			}
		}

		private void ResetRuntimeState()
		{
			CancelInterWaveScope();
			CancelWaveScope();

			currentWaveIndex = -1;
			currentLoopCount = 0;
			enemiesAlive = 0;
			enemiesSpawned = 0;
			totalEnemiesInWave = 0;
			enemiesKilled = 0;
			enemiesLeaked = 0;
			wavesCompleted = 0;
			registeredEnemyIds.Clear();
			terminalEnemyIds.Clear();

			adaptiveEnemyHealthFactor = 1f;
			adaptiveEnemyCountFactor = 1f;
			adaptiveEnemySpeedFactor = 1f;
			adaptiveRewardFactor = 1f;
			enemyLevelHealthFactor = 1f;
			enemyLevelCountFactor = 1f;
			enemyLevelSpeedFactor = 1f;
			pendingGeneratedWave = null;
			activeWaveConfig = null;

			spawnProgress = 0f;
			timeUntilNextWave = 0f;
			currentWaveStatus = "Idle";
			interWaveFailureReason = string.Empty;
			isSpawning = false;
			rewardOfferPending = false;
			rewardOfferResolved = false;
			rewardOfferId = string.Empty;
			selectedRewardId = string.Empty;
			rewardOfferCreatedForWave = -1;
			selectedReward = RewardOfferChoice.ResourceCache;
			nextCompletionRewardBonus = 0;
			lastRewardCurrencyAmount = 0;
			lastRewardBaseRepairAmount = 0;
			terminalRewardSuppressionLogged = false;
			activeChallengeModifier = ChallengeModifier.None;
			previewChallengeModifier = ChallengeModifier.ReinforcedHorde;
			challengeModifierResolved = false;
		}

		public void StartNextWave()
		{
			if (spawnPoints == null || spawnPoints.Length == 0)
			{
				Debug.LogError("WaveManager: Cannot start wave without validated spawn points.");
				return;
			}

			if (targetBase == null)
			{
				Debug.LogError("WaveManager: Cannot start wave without an initialized PlayerBase target.");
				return;
			}

			if (isSpawning)
			{
				Debug.LogWarning("WaveManager: Cannot start wave while already spawning!");
				return;
			}

			currentWaveIndex++;

			if (currentWaveIndex >= waves.Count)
			{
				if (loopWaves)
				{
					currentWaveIndex = 0;
					currentLoopCount++;
					if (Logs) Debug.Log($"[WaveManager] Starting loop {currentLoopCount + 1} with {difficultyScalingPerLoop}x difficulty");
				}
				else
				{
					return;
				}
			}

			activeWaveConfig = pendingGeneratedWave != null ? pendingGeneratedWave : waves[currentWaveIndex];
			pendingGeneratedWave = null;
			CancelInterWaveScope();
			spawnTask = SpawnWaveAsync(activeWaveConfig, BeginWaveScope());
		}

		private async UniTask SpawnWaveAsync(WaveConfig waveConfig, CancellationToken cancellationToken)
		{
			try
			{
				isSpawning = true;
				registeredEnemyIds.Clear();
				terminalEnemyIds.Clear();
				enemiesSpawned = 0;
				totalEnemiesInWave = GetTotalEnemyCount(waveConfig);
				spawnProgress = 0f;
				currentWaveStatus = $"Wave {currentWaveIndex + 1} - Waiting to start";

				if (Logs) Debug.Log($"[WaveManager] ==== Wave {currentWaveIndex + 1} Started ====");
				if (Logs)
					Debug.Log(
						$"[WaveManager] Total enemies: {totalEnemiesInWave}, Loop: {currentLoopCount + 1}, Difficulty: {Mathf.Pow(difficultyScalingPerLoop, currentLoopCount):F2}x");

				onWaveStarted?.Invoke(currentWaveIndex + 1);

				await UniTask.Delay((int)(waveConfig.DelayBeforeWave * 1000f), cancellationToken: cancellationToken);
				if (IsTerminalRunState())
				{
					isSpawning = false;
					return;
				}

				timeUntilNextWave = 0f;
				currentWaveStatus = $"Wave {currentWaveIndex + 1} - Spawning";

				int spawnGroupIndex = 0;
				foreach (var enemySpawn in waveConfig.EnemySpawns)
				{
					int count = GetScaledEnemyCount(enemySpawn, waveConfig);
					spawnGroupIndex++;

					if (Logs)
						Debug.Log(
							$"[WaveManager] Spawning group {spawnGroupIndex}: {count}x {enemySpawn.enemyPrefab?.name ?? "NULL"} (delay: {enemySpawn.spawnDelay}s)");

						for (int i = 0; i < count; i++)
					{
						if (IsTerminalRunState())
						{
							isSpawning = false;
							return;
						}

						if (SpawnEnemy(enemySpawn, waveConfig, spawnGroupIndex, i + 1, count))
						{
							enemiesSpawned++;
							spawnProgress = (float)enemiesSpawned / totalEnemiesInWave;
							onEnemySpawned?.Invoke(enemiesSpawned);

							if (detailedLogs) Debug.Log($"[WaveManager] Spawned enemy {enemiesSpawned}/{totalEnemiesInWave} ({spawnProgress * 100:F1}%)");
						}

						await UniTask.Delay((int)(enemySpawn.spawnDelay * 1000f), cancellationToken: cancellationToken);
					}
				}

				isSpawning = false;
				spawnProgress = 1f;
				currentWaveStatus = $"Wave {currentWaveIndex + 1} - Fighting ({enemiesAlive} enemies alive)";

				if (Logs) Debug.Log($"[WaveManager] All enemies spawned. Waiting for {enemiesAlive} enemies to be defeated...");

				while (enemiesAlive > 0)
				{
					currentWaveStatus = $"Wave {currentWaveIndex + 1} - Fighting ({enemiesAlive} enemies alive)";
					await UniTask.DelayFrame(1, cancellationToken: cancellationToken);
				}

				if (IsTerminalRunState())
					return;

				currentWaveStatus = $"Wave {currentWaveIndex + 1} - Completed!";
				OnWaveCompleted(waveConfig);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				isSpawning = false;
			}
		}

		private bool SpawnEnemy(EnemySpawnData enemySpawn, WaveConfig waveConfig, int groupIndex, int enemyIndexInGroup, int groupTotal)
		{
			if (enemySpawn.enemyPrefab == null)
			{
				Debug.LogError("[WaveManager] Enemy prefab is null!");
				return false;
			}

			Transform spawnPoint = GetSpawnPoint();
			var spawnAreaMask = NavMesh.AllAreas;
			var prefabAgent = enemySpawn.enemyPrefab.GetComponent<NavMeshAgent>();
			if (prefabAgent != null)
				spawnAreaMask = prefabAgent.areaMask;

			if (!NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit spawnHit, 2f, spawnAreaMask))
			{
				Debug.LogWarning($"[WaveManager] Spawn point {spawnPoint.name} has no NavMesh position within 2m.");
				return false;
			}

			GameObject enemyObject = Instantiate(enemySpawn.enemyPrefab, spawnHit.position, spawnPoint.rotation);

			var enemyHealth = enemyObject.GetComponent<MonsterHealth>();
			var enemyMovement = enemyObject.GetComponent<MonsterMove>();
			if (enemyHealth == null || enemyMovement == null)
			{
				Debug.LogError($"[WaveManager] Enemy prefab {enemyObject.name} must have MonsterHealth and MonsterMove.");
				if (Application.isPlaying)
					Destroy(enemyObject);
				else
					DestroyImmediate(enemyObject);
				return false;
			}

			if (!enemyMovement.Initialize(targetBase))
			{
				Debug.LogError($"[WaveManager] Enemy prefab {enemyObject.name} could not initialize its PlayerBase target.");
				if (Application.isPlaying)
					Destroy(enemyObject);
				else
					DestroyImmediate(enemyObject);
				return false;
			}

			float scaledHealth = enemyHealth.MaxHealth * enemySpawn.healthMultiplier * waveConfig.HealthScaling *
			                     Mathf.Pow(difficultyScalingPerLoop, currentLoopCount) * EnemyHealthFactor * adaptiveEnemyHealthFactor * enemyLevelHealthFactor;

			{
				enemyHealth.Initialize(scaledHealth);

				var enemyId = enemyHealth.GetInstanceID();
				registeredEnemyIds.Add(enemyId);
				enemyHealth.onDeath.AddListener(() => OnEnemyKilled(enemyHealth));
				enemyHealth.onLeak.AddListener(() => OnEnemyLeaked(enemyHealth));
				enemyHealth.onRewardGiven.AddListener((reward) => OnEnemyRewardGiven(reward));

				if (detailedLogs)
				{
					Debug.Log(
						$"[WaveManager]   → Enemy [{groupIndex}.{enemyIndexInGroup}/{groupTotal}]: {enemyObject.name} | HP: {scaledHealth:F0} | Spawn: {spawnPoint.name}");
				}
			}

			{
				float scaledSpeed = enemyMovement.Speed * enemySpawn.speedMultiplier * EnemySpeedFactor * adaptiveEnemySpeedFactor * enemyLevelSpeedFactor;
				enemyMovement.Speed = scaledSpeed;
				lastSpawnedEnemyArchetype = enemyMovement.Archetype.ToString();
				lastSpawnedEnemyHealth = scaledHealth;
				lastSpawnedEnemySpeed = scaledSpeed;
				lastSpawnedEnemyDelay = enemySpawn.spawnDelay;
				lastSpawnedEnemyGroupIndex = groupIndex;
				lastSpawnedEnemyIndex = enemyIndexInGroup;
				lastSpawnedEnemyGroupTotal = groupTotal;

				if (detailedLogs)
				{
					Debug.Log($"[WaveManager]   → Speed: {scaledSpeed:F2} units/sec");
				}
			}

			enemiesAlive++;
			return true;
		}

		private int GetTotalEnemyCount(WaveConfig waveConfig)
		{
			int total = 0;
			foreach (var enemySpawn in waveConfig.EnemySpawns)
			{
				total += GetScaledEnemyCount(enemySpawn, waveConfig);
			}

			return total;
		}

		public int GetUpcomingWaveTotalEnemyCount()
		{
			var upcomingWave = UpcomingWave;
			if (upcomingWave == null)
				return 0;

			var loopCount = GetUpcomingWaveLoopCount();
			var total = 0;
			foreach (var enemySpawn in upcomingWave.EnemySpawns)
			{
				total += GetScaledEnemyCount(enemySpawn, upcomingWave, loopCount);
			}

			return total;
		}

		public int GetUpcomingWaveEnemyCount(EnemySpawnData enemySpawn)
		{
			var upcomingWave = UpcomingWave;
			if (upcomingWave == null || enemySpawn == null)
				return 0;

			return GetScaledEnemyCount(enemySpawn, upcomingWave, GetUpcomingWaveLoopCount());
		}

		private int GetUpcomingWaveLoopCount()
		{
			if (pendingGeneratedWave != null || waves == null || waves.Count == 0)
				return currentLoopCount;

			var nextWaveIndex = currentWaveIndex + 1;
			return nextWaveIndex >= waves.Count && loopWaves ? currentLoopCount + 1 : currentLoopCount;
		}

		private int GetScaledEnemyCount(EnemySpawnData enemySpawn, WaveConfig waveConfig)
		{
			return GetScaledEnemyCount(enemySpawn, waveConfig, currentLoopCount);
		}

		private int GetScaledEnemyCount(EnemySpawnData enemySpawn, WaveConfig waveConfig, int loopCount)
		{
			return Mathf.Max(1, Mathf.RoundToInt(enemySpawn.count * waveConfig.CountScaling *
				Mathf.Pow(difficultyScalingPerLoop, loopCount) * EnemyCountFactor * adaptiveEnemyCountFactor * enemyLevelCountFactor));
		}

		private Transform GetSpawnPoint()
		{
			var activeEnemies = FindObjectsByType<MonsterMove>(FindObjectsSortMode.None);
			var availableSpawnPoints = new List<Transform>();
			Transform mostOpenSpawnPoint = spawnPoints[0];
			var mostOpenDistanceSqr = -1f;
			var firstSpawnPointClear = false;

			for (var i = 0; i < spawnPoints.Length; i++)
			{
				var spawnPoint = spawnPoints[i];
				if (spawnPoint == null)
					continue;

				var minimumDistanceSqr = float.PositiveInfinity;
				for (var enemyIndex = 0; enemyIndex < activeEnemies.Length; enemyIndex++)
				{
					var enemyHealth = activeEnemies[enemyIndex].GetComponent<MonsterHealth>();
					if (enemyHealth == null || !enemyHealth.IsAlive)
						continue;

					var spawnPosition = spawnPoint.position;
					var enemyPosition = activeEnemies[enemyIndex].transform.position;
					spawnPosition.y = 0f;
					enemyPosition.y = 0f;
					minimumDistanceSqr = Mathf.Min(minimumDistanceSqr, (spawnPosition - enemyPosition).sqrMagnitude);
				}

				var isClear = minimumDistanceSqr >= MinimumSpawnSeparation * MinimumSpawnSeparation;
				if (i == 0)
					firstSpawnPointClear = isClear;

				if (isClear)
					availableSpawnPoints.Add(spawnPoint);

				if (minimumDistanceSqr > mostOpenDistanceSqr)
				{
					mostOpenDistanceSqr = minimumDistanceSqr;
					mostOpenSpawnPoint = spawnPoint;
				}
			}

			if (!randomizeSpawnPoint && firstSpawnPointClear)
				return spawnPoints[0];

			if (availableSpawnPoints.Count > 0)
				return randomizeSpawnPoint
					? availableSpawnPoints[UnityEngine.Random.Range(0, availableSpawnPoints.Count)]
					: availableSpawnPoints[0];

			return mostOpenSpawnPoint;
		}

		private bool TryAcceptEnemyTerminal(MonsterHealth enemy)
		{
			if (enemy == null || !registeredEnemyIds.Contains(enemy.GetInstanceID()) ||
				!terminalEnemyIds.Add(enemy.GetInstanceID()))
				return false;

			enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
			return true;
		}

		private void OnEnemyKilled(MonsterHealth enemy)
		{
			if (!TryAcceptEnemyTerminal(enemy))
				return;

			enemiesKilled++;
			onEnemyKilled?.Invoke(enemiesAlive);
		}

		private void OnEnemyLeaked(MonsterHealth enemy)
		{
			if (!TryAcceptEnemyTerminal(enemy))
				return;

			enemiesLeaked++;
			onEnemyLeaked?.Invoke(enemiesAlive);
		}

		private void OnEnemyRewardGiven(int reward)
		{
			if (IsTerminalRunState())
			{
				if (Logs && !terminalRewardSuppressionLogged)
				{
					terminalRewardSuppressionLogged = true;
					Debug.Log("[WaveManager] Enemy reward suppressed after terminal state.");
				}

				return;
			}

			ResourceManager.Instance?.AddCurrency(Mathf.RoundToInt(reward * adaptiveRewardFactor));
		}

		private void OnWaveCompleted(WaveConfig waveConfig)
		{
			if (IsTerminalRunState())
			{
				CancelWaveScope();
				if (Logs)
					Debug.Log("[WaveManager] Wave completion suppressed after terminal state.");
				return;
			}

			CancelWaveScope();
			wavesCompleted++;
			int completionReward = Mathf.RoundToInt((waveConfig.CompletionReward + nextCompletionRewardBonus) * CompletionRewardFactor * adaptiveRewardFactor);
			nextCompletionRewardBonus = 0;

			if (Logs) Debug.Log($"[WaveManager] Wave {currentWaveIndex + 1} completed! Reward: {completionReward}");

			ResourceManager.Instance?.AddCurrency(completionReward);
			ResourceManager.Instance?.GivePassiveIncome();

			onWaveCompleted?.Invoke(currentWaveIndex + 1);

			if (currentWaveIndex + 1 >= waves.Count)
			{
				if (Logs) Debug.Log("[WaveManager] All waves completed!");
				onAllWavesCompleted?.Invoke();
				return;
			}

			_ = InterWavePhase(BeginInterWaveScope());
		}

		public static bool ShouldBlockTerminalResolution(bool baseDestroyed, bool gameOver)
		{
			return baseDestroyed || gameOver;
		}

		private bool IsTerminalRunState()
		{
			return ShouldBlockTerminalResolution(
				targetBase != null && targetBase.IsDestroyed,
				GameManager.Instance != null && GameManager.Instance.IsGameOver);
		}

		public bool SelectRewardOffer(string offerId, int choiceIndex)
		{
			if (!rewardOfferPending || offerId != rewardOfferId || choiceIndex < (int)RewardOfferChoice.ResourceCache ||
				choiceIndex > (int)RewardOfferChoice.BountyContract)
				return false;

			if ((targetBase != null && targetBase.IsDestroyed) ||
				(GameManager.Instance != null && GameManager.Instance.IsGameOver))
			{
				if (Logs) Debug.Log("[WaveManager] Reward offer rejected after terminal state.");
				return false;
			}

			lastRewardCurrencyAmount = 0;
			lastRewardBaseRepairAmount = 0;
			var choice = (RewardOfferChoice)choiceIndex;
			switch (choice)
			{
				case RewardOfferChoice.ResourceCache:
					if (ResourceManager.Instance == null)
						return false;

					lastRewardCurrencyAmount = GetResourceCacheAmount();
					ResourceManager.Instance.AddCurrency(lastRewardCurrencyAmount);
					break;
				case RewardOfferChoice.EmergencyRepairs:
				{
					if (GameManager.Instance == null)
						return false;

					var currentHealth = targetBase != null ? targetBase.CurrentHealth : 0;
					var maxHealth = targetBase != null ? targetBase.MaxHealth : 0;
					lastRewardBaseRepairAmount = GetEmergencyRepairAmount(currentHealth, maxHealth);
					if (lastRewardBaseRepairAmount <= 0 || !GameManager.Instance.TryRepairBase(lastRewardBaseRepairAmount))
						return false;

					break;
				}
				case RewardOfferChoice.BountyContract:
					nextCompletionRewardBonus = BountyContractBonus;
					break;
			}

			selectedReward = choice;
			selectedRewardId = choice.ToString();
			rewardOfferResolved = true;
			rewardOfferPending = false;
			if (Logs)
			{
				var currency = ResourceManager.Instance != null ? ResourceManager.Instance.CurrentCurrency : -1;
				Debug.Log($"[WaveManager] Reward offer selected: {choice} amount={lastRewardCurrencyAmount};baseRepair={lastRewardBaseRepairAmount};currency={currency}");
			}
			onRewardSelected?.Invoke((int)choice);
			return true;
		}

		private int GetResourceCacheAmount()
		{
			var currentCurrency = ResourceManager.Instance != null ? ResourceManager.Instance.CurrentCurrency : 0;
			var cheapestTowerCost = GetCheapestTowerCost();
			if (cheapestTowerCost <= 0 || currentCurrency >= cheapestTowerCost)
				return ResourceCacheAmount;

			return Mathf.Max(ResourceCacheAmount, cheapestTowerCost - currentCurrency);
		}

		public static int GetEmergencyRepairAmount(int currentHealth, int maxHealth)
		{
			if (maxHealth <= 0 || currentHealth >= maxHealth)
				return 0;

			var targetHealth = Mathf.CeilToInt(maxHealth * EmergencyRepairTargetHealthFraction);
			return Mathf.Max(0, targetHealth - Mathf.Max(0, currentHealth));
		}

		private static int GetCheapestTowerCost()
		{
			var towerStats = Resources.LoadAll<TowerStatsSO>("TowerStats");
			var cheapestTowerCost = int.MaxValue;
			for (var i = 0; i < towerStats.Length; i++)
			{
				var stats = towerStats[i];
				if (stats != null && stats.Cost > 0)
					cheapestTowerCost = Mathf.Min(cheapestTowerCost, stats.Cost);
			}

			return cheapestTowerCost == int.MaxValue ? 0 : cheapestTowerCost;
		}

		private async UniTask InterWavePhase(CancellationToken cancellationToken)
		{
			try
			{
				await RewardOfferPhase(cancellationToken);
				await TilePlacementPhase(cancellationToken);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
			}
		}

		private async UniTask RewardOfferPhase(CancellationToken cancellationToken)
		{
			rewardOfferId = $"loop-{currentLoopCount}-wave-{CurrentWaveNumber}";
			rewardOfferCreatedForWave = CurrentWaveNumber;
			rewardOfferResolved = false;
			selectedRewardId = string.Empty;
			selectedReward = RewardOfferChoice.ResourceCache;
			rewardOfferPending = true;
			onRewardOfferCreated?.Invoke(rewardOfferId);
			await UniTask.WaitUntil(() => !rewardOfferPending,
				cancellationToken: cancellationToken);
		}

		private async UniTask TilePlacementPhase(CancellationToken cancellationToken)
		{
			if (tileMapManager == null || TileDatabase.Instance == null || tilePlacementSystem == null)
			{
				ReportInterWaveFailure("Required tile placement owner is missing.");
				return;
			}

			if (Logs) Debug.Log("[WaveManager] Tile placement phase started");

			var tilePrefabs = TileDatabase.Instance.GetAllTilePrefabs();
			var placementChoices = tileMapManager.BuildPlacementChoices(tilePrefabs, 3);
			if (placementChoices.Count < 3)
			{
				ReportInterWaveFailure($"Only {placementChoices.Count} valid tile choices are available; at least 3 are required.");
				return;
			}

			await UniTask.Delay(500, cancellationToken: cancellationToken);

			tilePlacementSystem.StartTilePlacementOptions(placementChoices);
			if (!tilePlacementSystem.IsPlacing)
			{
				ReportInterWaveFailure("Tile placement owner rejected the valid choice set.");
				return;
			}

			if (Logs)
			{
				for (var i = 0; i < placementChoices.Count; i++)
				{
					var choice = placementChoices[i];
					Debug.Log(
						$"[WaveManager] Tile option {i + 1}: {choice.TileName} {choice.Rotation * 90}° at {choice.GridPosition}, " +
						$"open ends {choice.OpenRoadEndCountBefore}->{choice.OpenRoadEndCountAfter}");
				}
			}

			await UniTask.WaitUntil(() => !tilePlacementSystem.IsPlacing,
				cancellationToken: cancellationToken);

			if (!RefreshSpawnPoints(tileMapManager))
			{
				ReportInterWaveFailure("Committed tile placement produced no valid spawn anchors.");
				return;
			}

			if (Logs) Debug.Log("[WaveManager] Tile placement phase completed");

			ContinueToNextWave();
		}

		private void ReportInterWaveFailure(string reason)
		{
			interWaveFailureReason = reason;
			currentWaveStatus = $"Inter-wave blocked: {reason}";
			Debug.LogError($"[WaveManager] Inter-wave phase blocked: {reason}");
		}

		private bool RefreshSpawnPoints(TileMapManager tileMapManager)
		{
			var spawnPositions = tileMapManager.SpawnPositions;
			if (spawnPositions == null || spawnPositions.Count == 0)
				return false;

			for (var i = 0; i < spawnPositions.Count; i++)
			{
				if (spawnPositions[i] == Vector3.zero)
					return false;
			}

			var refreshedSpawnPoints = new Transform[spawnPositions.Count];
			for (var i = 0; i < spawnPositions.Count; i++)
			{
				if (spawnPoints != null && i < spawnPoints.Length && spawnPoints[i] != null)
				{
					refreshedSpawnPoints[i] = spawnPoints[i];
				}
				else
				{
					var spawnPoint = new GameObject($"Spawner_{i}");
					refreshedSpawnPoints[i] = spawnPoint.transform;
				}

				refreshedSpawnPoints[i].position = spawnPositions[i];
			}

			spawnPoints = refreshedSpawnPoints;
			return true;
		}

		private void ContinueToNextWave()
		{
			if (currentWaveIndex + 1 >= waves.Count)
			{
				if (Logs) Debug.Log("[WaveManager] All waves completed!");
				onAllWavesCompleted?.Invoke();
			}
			else
			{
				onPreparationReady?.Invoke();

				if (autoStartNextWave)
				{
					Invoke(nameof(RequestNextWave), autoStartDelay);
				}
			}
		}

		private void RequestNextWave()
		{
			GameManager.Instance?.StartNextWave();
		}

		private CancellationToken BeginWaveScope()
		{
			CancelWaveScope();
			waveCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
			return waveCancellationSource.Token;
		}

		private CancellationToken BeginInterWaveScope()
		{
			CancelInterWaveScope();
			interWaveCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
			return interWaveCancellationSource.Token;
		}

		private void CancelWaveScope()
		{
			if (waveCancellationSource == null)
				return;

			waveCancellationSource.Cancel();
			waveCancellationSource.Dispose();
			waveCancellationSource = null;
		}

		private void CancelInterWaveScope()
		{
			if (interWaveCancellationSource != null)
			{
				interWaveCancellationSource.Cancel();
				interWaveCancellationSource.Dispose();
				interWaveCancellationSource = null;
			}

			if (tilePlacementSystem != null && tilePlacementSystem.IsPlacing)
				tilePlacementSystem.CancelPlacement();
		}

		public void ForceStopWave()
		{
			CancelInterWaveScope();
			CancelWaveScope();
			isSpawning = false;

			if (rewardOfferPending)
			{
				rewardOfferPending = false;
				rewardOfferResolved = false;
				if (Logs) Debug.Log("[WaveManager] Pending reward offer cancelled by run stop.");
			}

			var enemies = FindObjectsByType<MonsterHealth>(FindObjectsSortMode.None);
			foreach (var enemy in enemies)
			{
				if (Application.isPlaying)
					Destroy(enemy.gameObject);
				else
					DestroyImmediate(enemy.gameObject);
			}

			enemiesAlive = 0;
			registeredEnemyIds.Clear();
			terminalEnemyIds.Clear();
		}

		private void OnDestroy()
		{
			CancelInterWaveScope();
			CancelWaveScope();

			if (Instance == this)
			{
				Instance = null;
			}

			onWaveStarted?.RemoveAllListeners();
			onWaveCompleted?.RemoveAllListeners();
			onRewardOfferCreated?.RemoveAllListeners();
			onRewardSelected?.RemoveAllListeners();
			onEnemySpawned?.RemoveAllListeners();
			onEnemyKilled?.RemoveAllListeners();
			onEnemyLeaked?.RemoveAllListeners();
			onAllWavesCompleted?.RemoveAllListeners();
			onChallengeModifierSelected?.RemoveAllListeners();
			onPreparationReady?.RemoveAllListeners();
		}
	}
}
