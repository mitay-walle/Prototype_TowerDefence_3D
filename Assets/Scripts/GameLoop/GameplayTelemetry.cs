using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TD.Levels;
using TD.Monsters;
using TD.Towers;

namespace TD.GameLoop
{
	public class GameplayTelemetry : MonoBehaviour
	{
		[SerializeField] private PlayerInput playerInput;
		[SerializeField] private GameManager gameManager;
		[SerializeField] private WaveManager waveManager;
		[SerializeField] private ResourceManager resourceManager;
		[SerializeField] private PlayerBase playerBase;
		[SerializeField] private TowerPlacementSystem towerPlacementSystem;
		[SerializeField] private TilePlacementSystem tilePlacementSystem;
		[SerializeField] private TileMapManager tileMapManager;
		[SerializeField] private int maxEvents = 2048;

		private readonly List<GameplayTelemetryEvent> events = new List<GameplayTelemetryEvent>();
		private readonly HashSet<InputAction> subscribedActions = new HashSet<InputAction>();
		private readonly Dictionary<MonsterHealth, UnityAction<float>> monsterDamageHandlers =
			new Dictionary<MonsterHealth, UnityAction<float>>();
		private readonly Dictionary<MonsterHealth, UnityAction> monsterDeathHandlers =
			new Dictionary<MonsterHealth, UnityAction>();
		private readonly Dictionary<MonsterHealth, UnityAction> monsterLeakHandlers =
			new Dictionary<MonsterHealth, UnityAction>();
		private readonly Dictionary<MonsterHealth, UnityAction<int>> monsterRewardHandlers =
			new Dictionary<MonsterHealth, UnityAction<int>>();
		private readonly Dictionary<Tower, UnityAction<MonsterHealth>> towerTargetHandlers =
			new Dictionary<Tower, UnityAction<MonsterHealth>>();
		private readonly Dictionary<Tower, UnityAction> towerLostTargetHandlers =
			new Dictionary<Tower, UnityAction>();
		private readonly Dictionary<Tower, UnityAction> towerFireHandlers =
			new Dictionary<Tower, UnityAction>();

		private GameplayTelemetrySnapshot lastSnapshot;
		private int nextSequence = 1;
		private int currencyGained;
		private int currencySpent;
		private bool isInitialized;
		private int waveTargetAcquisitions;
		private int waveTowerFires;
		private int waveDamageApplications;
		private int waveKills;
		private int waveLeaks;

		public int LastSequence => nextSequence - 1;
		public int FirstSequence => events.Count == 0 ? nextSequence : events[0].Sequence;

		private void Start()
		{
			InitializeTelemetry();
		}

		private void InitializeTelemetry()
		{
			if (isInitialized)
				return;

			isInitialized = true;
			SubscribeOwnerEvents();
			SubscribeInputActions();
			ObserveActors();
			lastSnapshot = CaptureSnapshot();
			Record("lifecycle", "ObservationStarted", "GameplayTelemetry", string.Empty, string.Empty, string.Empty,
				"Observer is attached to authored gameplay owners.");
		}

		private void SubscribeOwnerEvents()
		{
			if (gameManager != null)
			{
				gameManager.onGameStateChanged.AddListener(OnGameStateChanged);
				gameManager.onGameStarted.AddListener(OnGameStarted);
				gameManager.onGamePaused.AddListener(OnGamePaused);
				gameManager.onGameUnpaused.AddListener(OnGameUnpaused);
				gameManager.onGameOver.AddListener(OnGameOver);
				gameManager.onVictory.AddListener(OnVictory);
				gameManager.onRestartRequested.AddListener(OnRestartRequested);
				gameManager.onRunFinished.AddListener(OnRunFinished);
			}

			if (waveManager != null)
			{
				waveManager.onWaveStarted.AddListener(OnWaveStarted);
				waveManager.onWaveCompleted.AddListener(OnWaveCompleted);
				waveManager.onRewardOfferCreated.AddListener(OnRewardOfferCreated);
				waveManager.onRewardSelected.AddListener(OnRewardSelected);
				waveManager.onEnemySpawned.AddListener(OnEnemySpawned);
				waveManager.onEnemyKilled.AddListener(OnEnemyKilled);
				waveManager.onEnemyLeaked.AddListener(OnEnemyLeaked);
				waveManager.onAllWavesCompleted.AddListener(OnAllWavesCompleted);
				waveManager.onChallengeModifierSelected.AddListener(OnChallengeModifierSelected);
				waveManager.onPreparationReady.AddListener(OnPreparationReady);
			}

			if (resourceManager != null)
			{
				resourceManager.onCurrencyChanged.AddListener(OnCurrencyChanged);
				resourceManager.onCurrencyGained.AddListener(OnCurrencyGained);
				resourceManager.onCurrencySpent.AddListener(OnCurrencySpent);
			}

			if (playerBase != null)
			{
				playerBase.onHealthChanged.AddListener(OnBaseHealthChanged);
				playerBase.onBaseDestroyed.AddListener(OnBaseDestroyed);
			}

			if (tilePlacementSystem != null)
			{
				tilePlacementSystem.onPlacementChoiceSelected.AddListener(OnTilePlacementChoiceSelected);
				tilePlacementSystem.onTilePlaced.AddListener(OnTilePlaced);
				tilePlacementSystem.onPlacementCancelled.AddListener(OnTilePlacementCancelled);
			}

			if (towerPlacementSystem != null)
			{
				towerPlacementSystem.onPlacementPreviewChanged.AddListener(OnTowerPlacementPreviewChanged);
				towerPlacementSystem.onTowerPlaced.AddListener(OnTowerPlaced);
			}
		}

		private void SubscribeInputActions()
		{
			if (playerInput == null || playerInput.actions == null)
				return;

			foreach (var action in playerInput.actions)
			{
				if (action == null || subscribedActions.Contains(action))
					continue;

				action.performed += OnInputActionPerformed;
				subscribedActions.Add(action);
			}
		}

		private void ObserveActors()
		{
			ObserveMonsters();
			ObserveTowers();
		}

		private void ObserveMonsters()
		{
			var monsters = FindObjectsByType<MonsterHealth>(FindObjectsSortMode.None);
			foreach (var monster in monsters)
			{
				if (monster == null || monsterDamageHandlers.ContainsKey(monster))
					continue;

				UnityAction<float> damageHandler = damage => OnMonsterDamageTaken(monster, damage);
				UnityAction deathHandler = () => OnMonsterDeath(monster);
				UnityAction leakHandler = () => OnMonsterLeak(monster);
				UnityAction<int> rewardHandler = reward => OnMonsterReward(monster, reward);
				monster.onDamageTaken.AddListener(damageHandler);
				monster.onDeath.AddListener(deathHandler);
				monster.onLeak.AddListener(leakHandler);
				monster.onRewardGiven.AddListener(rewardHandler);
				monsterDamageHandlers.Add(monster, damageHandler);
				monsterDeathHandlers.Add(monster, deathHandler);
				monsterLeakHandlers.Add(monster, leakHandler);
				monsterRewardHandlers.Add(monster, rewardHandler);
			}
		}

		private void ObserveTowers()
		{
			var towers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
			foreach (var tower in towers)
			{
				if (tower == null || towerTargetHandlers.ContainsKey(tower))
					continue;

				UnityAction<MonsterHealth> targetHandler = target => OnTowerTargetAcquired(tower, target);
				UnityAction lostTargetHandler = () => OnTowerTargetLost(tower);
				UnityAction fireHandler = () => OnTowerFired(tower);
				tower.onTargetAcquired.AddListener(targetHandler);
				tower.onTargetLost.AddListener(lostTargetHandler);
				tower.onFire.AddListener(fireHandler);
				towerTargetHandlers.Add(tower, targetHandler);
				towerLostTargetHandlers.Add(tower, lostTargetHandler);
				towerFireHandlers.Add(tower, fireHandler);
			}
		}

		private void OnInputActionPerformed(InputAction.CallbackContext context)
		{
			var actionMap = context.action.actionMap != null ? context.action.actionMap.name : string.Empty;
			var control = context.control != null ? context.control.path : string.Empty;
			var value = context.ReadValueAsObject();
			Record("input", context.action.name, "PlayerInput", context.phase.ToString(), control,
				value != null ? value.ToString() : string.Empty, actionMap);
		}

		private void OnGameStateChanged(GameState state) => Record("state", "GameStateChanged", "GameManager", state.ToString(), string.Empty, string.Empty, "");
		private void OnGameStarted() => Record("state", "GameStarted", "GameManager", string.Empty, string.Empty, string.Empty, "");
		private void OnGamePaused() => Record("state", "GamePaused", "GameManager", string.Empty, string.Empty, string.Empty, "");
		private void OnGameUnpaused() => Record("state", "GameUnpaused", "GameManager", string.Empty, string.Empty, string.Empty, "");
		private void OnGameOver() => Record("terminal", "Defeat", "GameManager", string.Empty, string.Empty, string.Empty, "");
		private void OnVictory() => Record("terminal", "Victory", "GameManager", string.Empty, string.Empty, string.Empty, "");
		private void OnRestartRequested() => Record("lifecycle", "RestartRequested", "GameManager", string.Empty, "RestartGame", string.Empty, "");
		private void OnRunFinished(RunResult result)
		{
			if (result == null)
				return;

			Record("terminal", "RunFinished", "GameManager", result.Outcome.ToString(), "FinishRun", result.RunId, $"wavesCompleted={result.WavesCompleted};finalWave={result.FinalWaveIndex};baseHealth={result.BaseHealth}/{result.BaseMaxHealth};currency={result.Currency};duration={result.DurationSeconds:F2}");
		}
		private void OnWaveStarted(int wave)
		{
			waveTargetAcquisitions = 0;
			waveTowerFires = 0;
			waveDamageApplications = 0;
			waveKills = 0;
			waveLeaks = 0;
			Record("wave", "WaveStarted", "WaveManager", wave.ToString(), string.Empty, string.Empty, "");
		}
		private void OnWaveCompleted(int wave)
		{
			var details = FormatWaveCombatDetails(
				waveTargetAcquisitions,
				waveTowerFires,
				waveDamageApplications,
				waveKills,
				waveLeaks);
			Record("wave", "WaveCompleted", "WaveManager", wave.ToString(), "EvaluateWaveCombat", string.Empty, details);
		}
		private void OnRewardOfferCreated(string offerId) => Record("reward", "RewardOfferCreated", "WaveManager", offerId, "OpenRewardOffer", string.Empty,
			$"offerId={offerId}");
		private void OnRewardSelected(int choice)
		{
			var offerId = waveManager != null ? waveManager.RewardOfferId : string.Empty;
			var rewardId = waveManager != null ? waveManager.SelectedRewardId : ((RewardOfferChoice)choice).ToString();
			var amount = waveManager != null ? waveManager.LastRewardCurrencyAmount : 0;
			var baseRepair = waveManager != null ? waveManager.LastRewardBaseRepairAmount : 0;
			var currencyAfter = resourceManager != null ? resourceManager.CurrentCurrency : -1;
			Record("reward", "RewardSelected", "WaveManager", offerId, "SelectReward", rewardId,
				$"offerId={offerId};rewardId={rewardId};amount={amount};baseRepair={baseRepair};currencyAfter={currencyAfter}");
		}
		private void OnEnemySpawned(int spawned)
		{
			ObserveMonsters();
			var details = waveManager != null
				? FormatEnemySpawnDetails(
					waveManager.LastSpawnedEnemyGroupIndex,
					waveManager.LastSpawnedEnemyIndex,
					waveManager.LastSpawnedEnemyGroupTotal,
					waveManager.LastSpawnedEnemyArchetype,
					waveManager.LastSpawnedEnemyHealth,
					waveManager.LastSpawnedEnemySpeed,
					waveManager.LastSpawnedEnemyDelay)
				: string.Empty;
			Record("wave", "EnemySpawned", "WaveManager", spawned.ToString(), string.Empty, string.Empty, details);
		}

		private void OnEnemyKilled(int remaining)
		{
			waveKills++;
			Record("combat", "EnemyKilled", "WaveManager", remaining.ToString(), string.Empty, string.Empty, "");
		}
		private void OnEnemyLeaked(int remaining)
		{
			waveLeaks++;
			Record("combat", "EnemyLeaked", "WaveManager", remaining.ToString(), string.Empty, string.Empty, "");
		}
		private void OnAllWavesCompleted() => Record("terminal", "AllWavesCompleted", "WaveManager", string.Empty, string.Empty, string.Empty, "");
		private void OnChallengeModifierSelected()
		{
			var modifier = waveManager != null ? waveManager.ActiveChallengeModifier : ChallengeModifier.None;
			var resolved = waveManager != null && !waveManager.CanSelectChallengeModifier;
			Record("decision", "ChallengeModifierSelected", "WaveManager", modifier.ToString(), "SelectChallengeModifier", modifier.ToString(),
				$"modifier={modifier};resolved={resolved};" +
				$"count={(waveManager != null ? waveManager.EnemyCountFactor : 1f):F2};" +
				$"health={(waveManager != null ? waveManager.EnemyHealthFactor : 1f):F2};" +
				$"speed={(waveManager != null ? waveManager.EnemySpeedFactor : 1f):F2};" +
				$"reward={(waveManager != null ? waveManager.CompletionRewardFactor : 1f):F2}");
		}
		private void OnPreparationReady() => Record("state", "PreparationReady", "WaveManager", string.Empty, string.Empty, string.Empty, "");
		private void OnCurrencyChanged(int currency) => Record("economy", "CurrencyChanged", "ResourceManager", currency.ToString(), string.Empty, string.Empty, "");
		private void OnCurrencyGained(int amount)
		{
			currencyGained += Mathf.Max(0, amount);
			Record("economy", "CurrencyGained", "ResourceManager", amount.ToString(), string.Empty, string.Empty, "");
		}
		private void OnCurrencySpent(int amount)
		{
			currencySpent += Mathf.Max(0, amount);
			Record("economy", "CurrencySpent", "ResourceManager", amount.ToString(), string.Empty, string.Empty, "");
		}
		private void OnBaseHealthChanged(int health) => Record("base", "BaseHealthChanged", "PlayerBase", health.ToString(), string.Empty, string.Empty, "");
		private void OnBaseDestroyed() => Record("terminal", "BaseDestroyed", "PlayerBase", string.Empty, string.Empty, string.Empty, "");

		private void OnMonsterDamageTaken(MonsterHealth monster, float damage)
		{
			waveDamageApplications++;
			Record("combat", "DamageApplied", "MonsterHealth", damage.ToString("F2", CultureInfo.InvariantCulture), GetPath(monster), string.Empty,
				$"damage={damage.ToString("F2", CultureInfo.InvariantCulture)};targetHealth={monster.CurrentHealth.ToString("F2", CultureInfo.InvariantCulture)}");
		}

		private void OnMonsterDeath(MonsterHealth monster) => Record(
			"combat",
			"MonsterDeath",
			"MonsterHealth",
			string.Empty,
			GetPath(monster),
			string.Empty,
			GetMonsterTerminalDetails(monster));
		private void OnMonsterLeak(MonsterHealth monster) => Record(
			"combat",
			"MonsterLeak",
			"MonsterHealth",
			string.Empty,
			GetPath(monster),
			string.Empty,
			GetMonsterTerminalDetails(monster));
		private void OnMonsterReward(MonsterHealth monster, int reward) => Record("economy", "KillReward", "MonsterHealth", reward.ToString(), GetPath(monster), string.Empty, "");
		private static string GetMonsterTerminalDetails(MonsterHealth monster)
		{
			return $"archetype={GetMonsterArchetype(monster)};maxHealth={monster?.MaxHealth.ToString("F2", CultureInfo.InvariantCulture) ?? "-1"};terminalReason={monster?.TerminalReason.ToString() ?? MonsterTerminalReason.None.ToString()}";
		}
		private static string GetMonsterArchetype(MonsterHealth monster)
		{
			if (monster != null && monster.TryGetComponent(out MonsterStats stats) && stats.statsSO != null)
				return stats.statsSO.Archetype.ToString();

			return "Unknown";
		}
		private void OnTowerTargetAcquired(Tower tower, MonsterHealth target)
		{
			waveTargetAcquisitions++;
			var distance = tower != null && target != null ? Vector3.Distance(tower.transform.position, target.transform.position) : -1f;
			var targetHealth = target != null ? target.CurrentHealth : -1f;
			var range = tower != null ? tower.EffectiveRange : -1f;
			Record("combat", "TowerTargetAcquired", "Tower", GetPath(target), GetPath(tower), string.Empty,
				$"archetype={GetMonsterArchetype(target)};priority={tower?.TargetPriority.ToString() ?? "Unknown"};range={range:F2};distance={distance:F2};targetHealth={targetHealth:F2}");
		}
		private void OnTowerTargetLost(Tower tower) => Record("combat", "TowerTargetLost", "Tower", string.Empty, GetPath(tower), string.Empty, "");
		private void OnTowerFired(Tower tower)
		{
			waveTowerFires++;
			var target = tower != null ? tower.CurrentTarget : null;
			var distance = tower != null && target != null ? Vector3.Distance(tower.transform.position, target.transform.position) : -1f;
			var targetHealth = target != null ? target.CurrentHealth : -1f;
			var damage = tower != null && tower.Stats != null ? tower.Stats.Damage : 0f;
			var fireRate = tower != null && tower.Stats != null ? tower.Stats.FireRate : 0f;
			var range = tower != null ? tower.EffectiveRange : -1f;
			Record("combat", "TowerFired", "Tower", GetPath(target), GetPath(tower), string.Empty,
				$"archetype={GetMonsterArchetype(target)};priority={tower?.TargetPriority.ToString() ?? "Unknown"};damage={damage:F2};fireRate={fireRate:F2};range={range:F2};distance={distance:F2};targetHealth={targetHealth:F2}");
		}
		private void OnTilePlacementChoiceSelected(int choiceIndex)
		{
			if (tilePlacementSystem == null || !tilePlacementSystem.HasSelectedChoice)
				return;

			var choice = tilePlacementSystem.SelectedChoice;
			var spawnPositionsBefore = tileMapManager != null ? tileMapManager.SpawnPositions : new List<Vector3>();
			var spawnPositionsAfter = tileMapManager != null
				? tileMapManager.GetSpawnPositionsAfter(choice)
				: new List<Vector3>();
			var towers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
			var coveredEntrancesBefore = TowerPlacementSystem.CountCoveredEntrances(towers, spawnPositionsBefore);
			var coveredEntrancesAfter = TowerPlacementSystem.CountCoveredEntrances(towers, spawnPositionsAfter);
			Record("decision", "TilePlacementChoiceSelected", "TilePlacementSystem", choiceIndex.ToString(), "SelectTileOption", choice.TileName,
				$"index={choiceIndex};count={tilePlacementSystem.PlacementChoices.Count};grid={choice.GridPosition};rotation={choice.Rotation};openEnds={choice.OpenRoadEndCountBefore}->{choice.OpenRoadEndCountAfter};affected={choice.AffectedOpenRoadEnds.Count};coverage={coveredEntrancesBefore}/{spawnPositionsBefore.Count}->{coveredEntrancesAfter}/{spawnPositionsAfter.Count}");
		}
		private void OnTilePlaced(int choiceIndex) => Record("decision", "TilePlaced", "TilePlacementSystem", choiceIndex.ToString(), "PlaceTile", choiceIndex.ToString(), "choiceCommitted=true");
		private void OnTilePlacementCancelled(int choiceIndex) => Record("decision", "TilePlacementCancelled", "TilePlacementSystem", choiceIndex.ToString(), "CancelPlacement", choiceIndex.ToString(), "choiceCommitted=false");
		private void OnTowerPlacementPreviewChanged(int coveredEntrances, int totalEntrances)
		{
			var coverageRatio = totalEntrances > 0 ? (float)coveredEntrances / totalEntrances : 0f;
			var existingCoveredEntrances = towerPlacementSystem != null ? towerPlacementSystem.PreviewExistingCoveredEntrances : -1;
			var candidateCoveredEntrances = towerPlacementSystem != null ? towerPlacementSystem.PreviewCandidateCoveredEntrances : -1;
			var routeCovered = towerPlacementSystem != null ? towerPlacementSystem.PreviewCoveredRouteSamples : -1;
			var routeTotal = towerPlacementSystem != null ? towerPlacementSystem.PreviewTotalRouteSamples : -1;
			var existingRouteCovered = towerPlacementSystem != null ? towerPlacementSystem.PreviewExistingCoveredRouteSamples : -1;
			var candidateRouteCovered = towerPlacementSystem != null ? towerPlacementSystem.PreviewCandidateCoveredRouteSamples : -1;
			var routeRatio = routeTotal > 0 ? (float)routeCovered / routeTotal : 0f;
			Record("decision", "TowerPlacementCoveragePreview", "TowerPlacementSystem", $"{coveredEntrances}/{totalEntrances}", "PreviewCoverage", coverageRatio.ToString("F2", CultureInfo.InvariantCulture),
				$"existing={existingCoveredEntrances};candidate={candidateCoveredEntrances};combined={coveredEntrances};total={totalEntrances};ratio={coverageRatio.ToString("F2", CultureInfo.InvariantCulture)};routeExisting={existingRouteCovered};routeCandidate={candidateRouteCovered};routeCovered={routeCovered};routeTotal={routeTotal};routeRatio={routeRatio.ToString("F2", CultureInfo.InvariantCulture)};coverageMode=combined");
		}
		private void OnTowerPlaced(string details) => Record("decision", "TowerPlaced", "TowerPlacementSystem", details, "PlaceTower", details, $"{details};placementCommitted=true");

		public static string FormatWaveCombatDetails(
			int targetAcquisitions,
			int towerFires,
			int damageApplications,
			int kills,
			int leaks)
		{
			var firePerTarget = targetAcquisitions > 0 ? (float)towerFires / targetAcquisitions : 0f;
			var damagePerFire = towerFires > 0 ? (float)damageApplications / towerFires : 0f;
			return
				$"targetAcquisitions={Mathf.Max(0, targetAcquisitions)};" +
				$"towerFires={Mathf.Max(0, towerFires)};" +
				$"damageApplications={Mathf.Max(0, damageApplications)};" +
				$"kills={Mathf.Max(0, kills)};" +
				$"leaks={Mathf.Max(0, leaks)};" +
				$"firePerTarget={firePerTarget.ToString("F2", CultureInfo.InvariantCulture)};" +
				$"damagePerFire={damagePerFire.ToString("F2", CultureInfo.InvariantCulture)}";
		}

		public static string FormatEnemySpawnDetails(
			int groupIndex,
			int enemyIndex,
			int groupTotal,
			string archetype,
			float health,
			float speed,
			float spawnDelay)
		{
			return
				$"group={Mathf.Max(0, groupIndex)};" +
				$"enemy={Mathf.Max(0, enemyIndex)}/{Mathf.Max(0, groupTotal)};" +
				$"archetype={archetype ?? "Unknown"};" +
				$"health={health.ToString("F2", CultureInfo.InvariantCulture)};" +
				$"speed={speed.ToString("F2", CultureInfo.InvariantCulture)};" +
				$"spawnDelay={spawnDelay.ToString("F2", CultureInfo.InvariantCulture)}";
		}

		public bool TryGetLatestCompletedWaveCombat(
			out int wave,
			out int targetAcquisitions,
			out int towerFires,
			out int damageApplications,
			out int kills,
			out int leaks)
		{
			wave = 0;
			targetAcquisitions = 0;
			towerFires = 0;
			damageApplications = 0;
			kills = 0;
			leaks = 0;

			for (var eventIndex = events.Count - 1; eventIndex >= 0; eventIndex--)
			{
				var telemetryEvent = events[eventIndex];
				if (telemetryEvent == null || telemetryEvent.Name != "WaveCompleted")
					continue;

				if (!int.TryParse(telemetryEvent.Phase, NumberStyles.Integer, CultureInfo.InvariantCulture, out wave))
					continue;

				if (TryParseWaveCombatDetails(
					telemetryEvent.Details,
					out targetAcquisitions,
					out towerFires,
					out damageApplications,
					out kills,
					out leaks))
				{
					return true;
				}
			}

			return false;
		}

		public static bool TryParseWaveCombatDetails(
			string details,
			out int targetAcquisitions,
			out int towerFires,
			out int damageApplications,
			out int kills,
			out int leaks)
		{
			targetAcquisitions = 0;
			towerFires = 0;
			damageApplications = 0;
			kills = 0;
			leaks = 0;

			if (string.IsNullOrWhiteSpace(details))
				return false;

			var foundTargetAcquisitions = false;
			var foundTowerFires = false;
			var foundDamageApplications = false;
			var foundKills = false;
			var foundLeaks = false;
			var fields = details.Split(';');
			foreach (var field in fields)
			{
				var separatorIndex = field.IndexOf('=');
				if (separatorIndex <= 0)
					continue;

				var key = field.Substring(0, separatorIndex);
				var value = field.Substring(separatorIndex + 1);
				if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
					continue;

				switch (key)
				{
					case "targetAcquisitions":
						targetAcquisitions = Mathf.Max(0, parsedValue);
						foundTargetAcquisitions = true;
						break;
					case "towerFires":
						towerFires = Mathf.Max(0, parsedValue);
						foundTowerFires = true;
						break;
					case "damageApplications":
						damageApplications = Mathf.Max(0, parsedValue);
						foundDamageApplications = true;
						break;
					case "kills":
						kills = Mathf.Max(0, parsedValue);
						foundKills = true;
						break;
					case "leaks":
						leaks = Mathf.Max(0, parsedValue);
						foundLeaks = true;
						break;
				}
			}

			return foundTargetAcquisitions && foundTowerFires && foundDamageApplications && foundKills && foundLeaks;
		}

		private void Record(string category, string name, string owner, string phase, string control, string value, string details)
		{
			var after = CaptureSnapshot();
			var before = lastSnapshot ?? after;
			var telemetryEvent = new GameplayTelemetryEvent
			{
				Sequence = nextSequence++,
				Frame = Time.frameCount,
				Time = Time.unscaledTime,
				Category = category,
				Name = name,
				Owner = owner,
				Phase = phase,
				Control = control,
				Value = value,
				Details = details,
				BeforeState = before.GameState,
				AfterState = after.GameState,
				BeforeWave = before.WaveNumber,
				AfterWave = after.WaveNumber,
				BeforeCurrency = before.Currency,
				AfterCurrency = after.Currency,
				BeforeBaseHealth = before.BaseHealth,
				AfterBaseHealth = after.BaseHealth,
                BeforeEnemiesAlive = before.EnemiesAlive,
                AfterEnemiesAlive = after.EnemiesAlive,
                BeforeTowerCount = before.Towers.Count,
                AfterTowerCount = after.Towers.Count,
                BeforePaused = before.IsPaused,
                AfterPaused = after.IsPaused,
                BeforeRewardOfferPending = before.RewardOfferPending,
                AfterRewardOfferPending = after.RewardOfferPending,
                BeforeRewardOfferResolved = before.RewardOfferResolved,
                AfterRewardOfferResolved = after.RewardOfferResolved,
                BeforeRewardOfferId = before.RewardOfferId,
                AfterRewardOfferId = after.RewardOfferId,
                BeforeSelectedRewardId = before.SelectedRewardId,
                AfterSelectedRewardId = after.SelectedRewardId,
                BeforeRewardOfferCreatedForWave = before.RewardOfferCreatedForWave,
                AfterRewardOfferCreatedForWave = after.RewardOfferCreatedForWave,
                BeforeSelectedReward = before.SelectedReward,
                AfterSelectedReward = after.SelectedReward,
                BeforeChallengeModifier = before.ActiveChallengeModifier,
                AfterChallengeModifier = after.ActiveChallengeModifier,
                BeforeTowerPlacing = before.IsTowerPlacing,
                AfterTowerPlacing = after.IsTowerPlacing,
                BeforeTilePlacing = before.IsTilePlacing,
                AfterTilePlacing = after.IsTilePlacing,
                BeforeSelectedTileIndex = before.SelectedTileIndex,
                AfterSelectedTileIndex = after.SelectedTileIndex,
                BeforeTileOptionCount = before.TileOptionCount,
                AfterTileOptionCount = after.TileOptionCount,
                BeforeActiveEnemyCount = before.ActiveEnemyCount,
                AfterActiveEnemyCount = after.ActiveEnemyCount
            };

			events.Add(telemetryEvent);
			while (events.Count > Mathf.Max(1, maxEvents))
				events.RemoveAt(0);

			lastSnapshot = after;
		}

		public GameplayTelemetrySnapshot CaptureSnapshot()
		{
			ObserveActors();
			var levelGenerator = tileMapManager != null ? tileMapManager.GetComponentInParent<LevelGenerator>() : null;
			var snapshot = new GameplayTelemetrySnapshot
			{
				Frame = Time.frameCount,
				Time = Time.unscaledTime,
				GameState = gameManager != null ? gameManager.CurrentState.ToString() : "Unknown",
				IsPaused = gameManager != null && gameManager.IsPaused,
				WaveNumber = waveManager != null ? waveManager.CurrentWaveNumber : 0,
				TotalWaves = waveManager != null ? waveManager.TotalWaves : 0,
				IsSpawning = waveManager != null && waveManager.IsSpawning,
				EnemiesAlive = waveManager != null ? waveManager.EnemiesAlive : 0,
				EnemiesSpawned = waveManager != null ? waveManager.EnemiesSpawned : 0,
				TotalEnemiesInWave = waveManager != null ? waveManager.TotalEnemiesInWave : 0,
				WaveProgress = waveManager != null ? waveManager.WaveProgress : 0f,
				Currency = resourceManager != null ? resourceManager.CurrentCurrency : -1,
				StartingCurrency = resourceManager != null ? resourceManager.StartingCurrency : 0,
				CurrencyGained = currencyGained,
				CurrencySpent = currencySpent,
				BaseHealth = playerBase != null ? playerBase.CurrentHealth : -1,
				BaseMaxHealth = playerBase != null ? playerBase.MaxHealth : -1,
				IsBaseDestroyed = playerBase != null && playerBase.IsDestroyed,
				RewardOfferPending = waveManager != null && waveManager.IsRewardOfferPending,
				RewardOfferResolved = waveManager != null && waveManager.HasSelectedReward,
				RewardOfferId = waveManager != null ? waveManager.RewardOfferId : string.Empty,
				SelectedRewardId = waveManager != null ? waveManager.SelectedRewardId : string.Empty,
				RewardOfferCreatedForWave = waveManager != null ? waveManager.RewardOfferCreatedForWave : -1,
				SelectedReward = waveManager != null ? waveManager.SelectedReward.ToString() : string.Empty,
				ActiveChallengeModifier = waveManager != null ? waveManager.ActiveChallengeModifier.ToString() : "None",
				CanSelectChallengeModifier = waveManager != null && waveManager.CanSelectChallengeModifier,
				ChallengeModifierEnemyCountFactor = waveManager != null ? waveManager.EnemyCountFactor : 1f,
				ChallengeModifierEnemyHealthFactor = waveManager != null ? waveManager.EnemyHealthFactor : 1f,
				ChallengeModifierEnemySpeedFactor = waveManager != null ? waveManager.EnemySpeedFactor : 1f,
				ChallengeModifierCompletionRewardFactor = waveManager != null ? waveManager.CompletionRewardFactor : 1f,
				IsTowerPlacing = towerPlacementSystem != null && towerPlacementSystem.IsPlacing,
				IsTilePlacing = tilePlacementSystem != null && tilePlacementSystem.IsPlacing,
				SelectedTileIndex = tilePlacementSystem != null ? tilePlacementSystem.SelectedChoiceIndex : -1,
				TileOptionCount = tilePlacementSystem != null ? tilePlacementSystem.PlacementChoices.Count : 0,
				EnemiesKilled = waveManager != null ? waveManager.EnemiesKilled : 0,
				EnemiesLeaked = waveManager != null ? waveManager.EnemiesLeaked : 0,
				WavesCompleted = waveManager != null ? waveManager.WavesCompleted : 0,
				AdaptiveEnemyHealthFactor = waveManager != null ? waveManager.AdaptiveEnemyHealthFactor : 1f,
				AdaptiveEnemyCountFactor = waveManager != null ? waveManager.AdaptiveEnemyCountFactor : 1f,
				AdaptiveEnemySpeedFactor = waveManager != null ? waveManager.AdaptiveEnemySpeedFactor : 1f,
				AdaptiveRewardFactor = waveManager != null ? waveManager.AdaptiveRewardFactor : 1f,
				AdaptiveDifficultyScore = waveManager != null ? waveManager.AdaptiveDifficultyScore : 0f,
				EnemyLevelHealthFactor = waveManager != null ? waveManager.EnemyLevelHealthFactor : 1f,
				EnemyLevelCountFactor = waveManager != null ? waveManager.EnemyLevelCountFactor : 1f,
				EnemyLevelSpeedFactor = waveManager != null ? waveManager.EnemyLevelSpeedFactor : 1f,
				EnemyLevelDifficultyScore = waveManager != null ? waveManager.EnemyLevelDifficultyScore : 0.5f,
				HasGeneratedWave = waveManager != null && waveManager.HasPendingGeneratedWave,
				GeneratedWaveGroupCount = waveManager != null ? waveManager.GeneratedWaveGroupCount : 0,
				GeneratedWavePredictedDamageFraction = waveManager != null ? waveManager.GeneratedWavePredictedDamageFraction : 0f,
				GeneratedWaveTensionScore = waveManager != null ? waveManager.GeneratedWaveTensionScore : 0f,
				GeneratedWaveSeed = waveManager != null ? waveManager.GeneratedWaveSeed : 0,
				TileMapGenerationSeed = levelGenerator != null ? levelGenerator.GeneratedSeed : 0
			};

			var monsters = FindObjectsByType<MonsterHealth>(FindObjectsSortMode.None);
			foreach (var monster in monsters)
			{
				if (monster != null && monster.IsAlive)
					snapshot.ActiveEnemyCount++;
			}

			PopulateFirstActiveEnemy(snapshot);

			var towers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
			foreach (var tower in towers)
			{
				if (tower == null)
					continue;

				var stats = tower.Stats;
				if (stats != null)
					snapshot.TowersUpgraded += Mathf.Max(0, stats.currentGrade);
				snapshot.Towers.Add(new GameplayTelemetryTower
				{
					Name = tower.name,
					Path = GetPath(tower),
					Level = stats != null ? stats.currentGrade : -1,
					Damage = stats != null ? stats.Damage.Value : 0f,
					FireRate = stats != null ? stats.FireRate.Value : 0f,
					Range = tower.EffectiveRange,
					TargetPriority = tower.TargetPriority.ToString(),
					CurrentTarget = GetPath(tower.CurrentTarget),
					HasTarget = tower.HasTarget,
					WorldPositionX = tower.transform.position.x,
					WorldPositionY = tower.transform.position.y,
					WorldPositionZ = tower.transform.position.z,
					DistanceToBase = tileMapManager != null
						? Vector3.Distance(tower.transform.position, tileMapManager.BasePosition)
						: -1f
				});
			}

			PopulateTowerCoverage(snapshot, towers);
			PopulateTileMap(snapshot);

			return snapshot;
		}

		private void PopulateFirstActiveEnemy(GameplayTelemetrySnapshot snapshot)
		{
			if (tileMapManager != null)
			{
				snapshot.BaseCenterX = tileMapManager.BasePosition.x;
				snapshot.BaseCenterY = tileMapManager.BasePosition.y;
				snapshot.BaseCenterZ = tileMapManager.BasePosition.z;
			}

			var monsters = FindObjectsByType<TD.Monsters.MonsterMove>(FindObjectsSortMode.None);
			for (var index = 0; index < monsters.Length; index++)
			{
				var monster = monsters[index];
				if (monster == null || monster.GetComponent<MonsterHealth>() == null || !monster.GetComponent<MonsterHealth>().IsAlive)
					continue;

				var agent = monster.GetComponent<UnityEngine.AI.NavMeshAgent>();
				snapshot.FirstActiveEnemyName = monster.name;
				snapshot.FirstActiveEnemyArchetype = monster.Archetype.ToString();
				snapshot.FirstActiveEnemyPositionX = monster.transform.position.x;
				snapshot.FirstActiveEnemyPositionY = monster.transform.position.y;
				snapshot.FirstActiveEnemyPositionZ = monster.transform.position.z;
				snapshot.FirstActiveEnemyDistanceToBase = monster.BaseTarget != null
					? Vector3.Distance(monster.transform.position, monster.BaseTarget.transform.position)
					: float.PositiveInfinity;
				if (agent == null)
					return;

				snapshot.FirstActiveEnemyOnNavMesh = agent.isOnNavMesh;
				if (!snapshot.FirstActiveEnemyOnNavMesh)
					return;

				snapshot.FirstActiveEnemyHasPath = agent.hasPath;
				snapshot.FirstActiveEnemyPathPending = agent.pathPending;
				snapshot.FirstActiveEnemyPathStatus = agent.pathStatus.ToString();
				snapshot.FirstActiveEnemyRemainingDistance = agent.remainingDistance;
				snapshot.FirstActiveEnemyDesiredVelocityX = agent.desiredVelocity.x;
				snapshot.FirstActiveEnemyDesiredVelocityY = agent.desiredVelocity.y;
				snapshot.FirstActiveEnemyDesiredVelocityZ = agent.desiredVelocity.z;
				snapshot.FirstActiveEnemyVelocityX = agent.velocity.x;
				snapshot.FirstActiveEnemyVelocityY = agent.velocity.y;
				snapshot.FirstActiveEnemyVelocityZ = agent.velocity.z;
				snapshot.FirstActiveEnemyDestinationX = agent.destination.x;
				snapshot.FirstActiveEnemyDestinationY = agent.destination.y;
				snapshot.FirstActiveEnemyDestinationZ = agent.destination.z;
				return;
			}
		}

		private void PopulateTowerCoverage(GameplayTelemetrySnapshot snapshot, Tower[] towers)
		{
			if (tileMapManager == null || tileMapManager.SpawnPositions == null)
				return;

			var entrances = tileMapManager.SpawnPositions;
			snapshot.TotalEntrances = entrances.Count;
			if (entrances.Count == 0)
				return;

			var maxDistance = 1f;
			for (var entranceIndex = 0; entranceIndex < entrances.Count; entranceIndex++)
				maxDistance = Mathf.Max(maxDistance, Vector3.Distance(tileMapManager.BasePosition, entrances[entranceIndex]));

			var distanceSum = 0f;
			for (var towerIndex = 0; towerIndex < towers.Length; towerIndex++)
			{
				if (towers[towerIndex] == null)
					continue;

				distanceSum += Vector3.Distance(tileMapManager.BasePosition, towers[towerIndex].transform.position);
			}

			if (towers.Length > 0)
			{
				var averageDistance = distanceSum / towers.Length;
				snapshot.TowerBaseConcentration = 1f - Mathf.Clamp01(averageDistance / maxDistance);
			}

			for (var entranceIndex = 0; entranceIndex < entrances.Count; entranceIndex++)
			{
				for (var towerIndex = 0; towerIndex < towers.Length; towerIndex++)
				{
					var tower = towers[towerIndex];
					if (tower == null || Vector3.Distance(tower.transform.position, entrances[entranceIndex]) > tower.EffectiveRange)
						continue;

					snapshot.CoveredEntrances++;
					break;
				}
			}

			snapshot.EntryCoverageRatio = Mathf.Clamp01((float)snapshot.CoveredEntrances / entrances.Count);
		}

		private void PopulateTileMap(GameplayTelemetrySnapshot snapshot)
		{
			var allTiles = tileMapManager != null ? tileMapManager.GetAllTiles() : null;
			if (allTiles == null || allTiles.Count == 0)
			{
				AddTileMapValidationError(snapshot, tileMapManager == null ? "TileMapManager is missing" : "Tile map is empty");
				return;
			}

			snapshot.TileMapTileCount = allTiles.Count;
			snapshot.TileMapHasBase = allTiles.ContainsKey(Vector2Int.zero);
			if (!snapshot.TileMapHasBase)
				AddTileMapValidationError(snapshot, "Base tile is missing at (0, 0)");

			var rotatedConnections = new Dictionary<Vector2Int, RoadConnections>();
			foreach (var tile in allTiles)
				rotatedConnections[tile.Key] = tile.Value.GetRotatedConnections(tile.Value.rotation);

			foreach (var tile in allTiles)
			{
				var position = tile.Key;
				var tileDefinition = tile.Value;
				var connections = rotatedConnections[position];
				var hasOpenRoadEnd = false;

				snapshot.TileMapTiles.Add(new GameplayTelemetryTile
				{
					GridX = position.x,
					GridY = position.y,
					Rotation = ((tileDefinition.rotation % 4) + 4) % 4,
					Name = tileDefinition.name ?? string.Empty,
					Connections = connections.ToString(),
					ConnectionMask = (int)connections
				});

				foreach (RoadSide side in System.Enum.GetValues(typeof(RoadSide)))
				{
					var neighborPosition = position + GetTileOffset(side);
					var hasConnection = connections.HasConnection(side);
					if (!allTiles.ContainsKey(neighborPosition))
					{
						if (hasConnection)
						{
							snapshot.TileMapOpenRoadEndCount++;
							hasOpenRoadEnd = true;
						}

						continue;
					}

					if (!IsFirstTile(position, neighborPosition))
						continue;

					var neighborConnections = rotatedConnections[neighborPosition];
					var neighborHasConnection = neighborConnections.HasConnection(RoadConnectionsExtensions.GetOppositeSide(side));
					if (hasConnection == neighborHasConnection)
						continue;

					snapshot.TileMapInvalidConnectionCount++;
					AddTileMapValidationError(snapshot,
						$"Connection mismatch {position} {side} <-> {neighborPosition}");
				}

				if (hasOpenRoadEnd)
					snapshot.TileMapTiles[snapshot.TileMapTiles.Count - 1].HasOpenRoadEnd = true;
			}

			var connectedTiles = new HashSet<Vector2Int>();
			if (snapshot.TileMapHasBase)
			{
				var pendingTiles = new Queue<Vector2Int>();
				pendingTiles.Enqueue(Vector2Int.zero);
				connectedTiles.Add(Vector2Int.zero);

				while (pendingTiles.Count > 0)
				{
					var position = pendingTiles.Dequeue();
					var connections = rotatedConnections[position];
					foreach (RoadSide side in System.Enum.GetValues(typeof(RoadSide)))
					{
						if (!connections.HasConnection(side))
							continue;

						var neighborPosition = position + GetTileOffset(side);
						if (!rotatedConnections.TryGetValue(neighborPosition, out var neighborConnections) ||
							!neighborConnections.HasConnection(RoadConnectionsExtensions.GetOppositeSide(side)))
							continue;

						if (connectedTiles.Add(neighborPosition))
							pendingTiles.Enqueue(neighborPosition);
					}
				}
			}

			foreach (var tile in allTiles)
			{
				if (connectedTiles.Contains(tile.Key))
					continue;

				snapshot.TileMapDisconnectedTileCount++;
				AddTileMapValidationError(snapshot, $"Tile {tile.Key} is disconnected from the base");
			}

			snapshot.TileMapValid = snapshot.TileMapHasBase &&
				snapshot.TileMapInvalidConnectionCount == 0 &&
				snapshot.TileMapDisconnectedTileCount == 0;
		}

		private static Vector2Int GetTileOffset(RoadSide side)
		{
			return side switch
			{
				RoadSide.North => Vector2Int.up,
				RoadSide.South => Vector2Int.down,
				RoadSide.East => Vector2Int.right,
				RoadSide.West => Vector2Int.left,
				_ => Vector2Int.zero
			};
		}

		private static bool IsFirstTile(Vector2Int first, Vector2Int second)
		{
			return first.x < second.x || first.x == second.x && first.y < second.y;
		}

		private static void AddTileMapValidationError(GameplayTelemetrySnapshot snapshot, string error)
		{
			if (snapshot.TileMapValidationErrors == null)
				snapshot.TileMapValidationErrors = new List<string>();

			if (snapshot.TileMapValidationErrors.Count < 32)
				snapshot.TileMapValidationErrors.Add(error);
		}

		public List<GameplayTelemetryEvent> GetEventsSince(int afterSequence, int maxCount)
		{
			var result = new List<GameplayTelemetryEvent>();
			var limit = Mathf.Max(1, maxCount);
			foreach (var telemetryEvent in events)
			{
				if (telemetryEvent.Sequence <= afterSequence)
					continue;

				result.Add(telemetryEvent);
				if (result.Count >= limit)
					break;
			}

			return result;
		}

		public void ClearEvents()
		{
			events.Clear();
			nextSequence = 1;
			lastSnapshot = CaptureSnapshot();
		}

		private static string GetPath(MonoBehaviour component)
		{
			return component == null ? string.Empty : GetPath(component.gameObject);
		}

		private static string GetPath(Tower tower)
		{
			return tower == null ? string.Empty : GetPath(tower.gameObject);
		}

		private static string GetPath(MonsterHealth monster)
		{
			return monster == null ? string.Empty : GetPath(monster.gameObject);
		}

		private static string GetPath(GameObject gameObject)
		{
			if (gameObject == null)
				return string.Empty;

			var path = gameObject.name;
			var parent = gameObject.transform.parent;
			while (parent != null)
			{
				path = parent.name + "/" + path;
				parent = parent.parent;
			}

			return path;
		}

		private void OnDisable()
		{
			if (!isInitialized)
				return;

			if (gameManager != null)
			{
				gameManager.onGameStateChanged.RemoveListener(OnGameStateChanged);
				gameManager.onGameStarted.RemoveListener(OnGameStarted);
				gameManager.onGamePaused.RemoveListener(OnGamePaused);
				gameManager.onGameUnpaused.RemoveListener(OnGameUnpaused);
				gameManager.onGameOver.RemoveListener(OnGameOver);
				gameManager.onVictory.RemoveListener(OnVictory);
				gameManager.onRestartRequested.RemoveListener(OnRestartRequested);
				gameManager.onRunFinished.RemoveListener(OnRunFinished);
			}

			if (waveManager != null)
			{
				waveManager.onWaveStarted.RemoveListener(OnWaveStarted);
				waveManager.onWaveCompleted.RemoveListener(OnWaveCompleted);
				waveManager.onRewardOfferCreated.RemoveListener(OnRewardOfferCreated);
				waveManager.onRewardSelected.RemoveListener(OnRewardSelected);
				waveManager.onEnemySpawned.RemoveListener(OnEnemySpawned);
				waveManager.onEnemyKilled.RemoveListener(OnEnemyKilled);
				waveManager.onEnemyLeaked.RemoveListener(OnEnemyLeaked);
				waveManager.onAllWavesCompleted.RemoveListener(OnAllWavesCompleted);
				waveManager.onChallengeModifierSelected.RemoveListener(OnChallengeModifierSelected);
				waveManager.onPreparationReady.RemoveListener(OnPreparationReady);
			}

			if (resourceManager != null)
			{
				resourceManager.onCurrencyChanged.RemoveListener(OnCurrencyChanged);
				resourceManager.onCurrencyGained.RemoveListener(OnCurrencyGained);
				resourceManager.onCurrencySpent.RemoveListener(OnCurrencySpent);
			}

			if (playerBase != null)
			{
				playerBase.onHealthChanged.RemoveListener(OnBaseHealthChanged);
				playerBase.onBaseDestroyed.RemoveListener(OnBaseDestroyed);
			}

			if (tilePlacementSystem != null)
			{
				tilePlacementSystem.onPlacementChoiceSelected.RemoveListener(OnTilePlacementChoiceSelected);
				tilePlacementSystem.onTilePlaced.RemoveListener(OnTilePlaced);
				tilePlacementSystem.onPlacementCancelled.RemoveListener(OnTilePlacementCancelled);
			}

			if (towerPlacementSystem != null)
			{
				towerPlacementSystem.onPlacementPreviewChanged.RemoveListener(OnTowerPlacementPreviewChanged);
				towerPlacementSystem.onTowerPlaced.RemoveListener(OnTowerPlaced);
			}

			foreach (var action in subscribedActions)
				action.performed -= OnInputActionPerformed;
			subscribedActions.Clear();

			foreach (var pair in monsterDamageHandlers)
				if (pair.Key != null) pair.Key.onDamageTaken.RemoveListener(pair.Value);
			foreach (var pair in monsterDeathHandlers)
				if (pair.Key != null) pair.Key.onDeath.RemoveListener(pair.Value);
			foreach (var pair in monsterLeakHandlers)
				if (pair.Key != null) pair.Key.onLeak.RemoveListener(pair.Value);
			foreach (var pair in monsterRewardHandlers)
				if (pair.Key != null) pair.Key.onRewardGiven.RemoveListener(pair.Value);
			foreach (var pair in towerTargetHandlers)
				if (pair.Key != null) pair.Key.onTargetAcquired.RemoveListener(pair.Value);
			foreach (var pair in towerLostTargetHandlers)
				if (pair.Key != null) pair.Key.onTargetLost.RemoveListener(pair.Value);
			foreach (var pair in towerFireHandlers)
				if (pair.Key != null) pair.Key.onFire.RemoveListener(pair.Value);

			monsterDamageHandlers.Clear();
			monsterDeathHandlers.Clear();
			monsterLeakHandlers.Clear();
			monsterRewardHandlers.Clear();
			towerTargetHandlers.Clear();
			towerLostTargetHandlers.Clear();
			towerFireHandlers.Clear();
			isInitialized = false;
		}
	}
}
