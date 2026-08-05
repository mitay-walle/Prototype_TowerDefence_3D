using System.Collections.Generic;
using TD.GameLoop;
using TD.Interactions;
using TD.Levels;
using TD.MLAgents;
using TD.Towers;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TD.EditorTools
{
	public static class TowerDefenceMlAgentsSetup
	{
		private const string GameplayScenePath = "Assets/Scenes/Gameplay.unity";
		private const string AgentObjectName = "TD ML Agent";
		private const string BalanceAgentObjectName = "TD ML Balance Agent";
		private const string EnemyLevelAgentObjectName = "TD ML Enemy Level Agent";
		private const string InputObjectName = "TD ML Input";
		private const string BehaviorName = "TD3DAgent";
		private const string BalanceBehaviorName = "TD3DBalanceAgent";
		private const string EnemyLevelBehaviorName = "TD3DEnemyLevelAgent";
		private const int DecisionPeriod = 5;
		private const int MaxStep = 0;
		private static readonly List<AgentIsolationState> RuntimeIsolationStates = new List<AgentIsolationState>();
		private static readonly List<PlayerEpisodeRestartState> RuntimePlayerEpisodeRestartStates = new List<PlayerEpisodeRestartState>();
		private static readonly List<TowerIsolationState> RuntimeTowerIsolationStates = new List<TowerIsolationState>();
		private static bool runtimeIsolationActive;
		private static bool runtimeTowerIsolationActive;

		[MenuItem("TD/ML-Agents/Setup Gameplay Agent")]
		private static void SetupGameplayAgent()
		{
			var scene = SceneManager.GetActiveScene();
			if (scene.path != GameplayScenePath)
			{
				Debug.LogError($"[TD ML-Agents] Open {GameplayScenePath} before running setup.");
				return;
			}

			var agentObject = GameObject.Find(AgentObjectName);
			if (agentObject == null)
			{
				agentObject = new GameObject(AgentObjectName);
				Undo.RegisterCreatedObjectUndo(agentObject, "Create TD ML Agent");
			}

			var agent = GetOrAddComponent<TowerDefenceAgent>(agentObject);
			var behaviorParameters = GetOrAddComponent<BehaviorParameters>(agentObject);
			var decisionRequester = GetOrAddComponent<DecisionRequester>(agentObject);
			var inputObject = agentObject.transform.Find(InputObjectName)?.gameObject;
			if (inputObject == null)
			{
				inputObject = new GameObject(InputObjectName);
				inputObject.transform.SetParent(agentObject.transform, false);
				Undo.RegisterCreatedObjectUndo(inputObject, "Create TD ML Input");
			}

			var syntheticMouse = GetOrAddComponent<SyntheticMouse>(inputObject);
			inputObject.SetActive(false);

			var references = FindReferences();
			if (!references.IsComplete)
			{
				Debug.LogError("[TD ML-Agents] Setup stopped because required gameplay references are missing.");
				return;
			}

			ConfigureTelemetry(references.GameplayTelemetry, references.TileMapManager);

			var towerPrefabs = FindTowerPrefabs();
			if (towerPrefabs.Count == 0)
			{
				Debug.LogError("[TD ML-Agents] Setup stopped because no tower prefabs were found in Assets/Prefabs/Towers.");
				return;
			}

			var enemyPrefabs = FindEnemyPrefabs();
			var enemyVisualBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Monster.prefab");
			if (enemyPrefabs.Count < WaveManager.GeneratedArchetypeActionSize - 1 || enemyVisualBase == null)
			{
				Debug.LogError("[TD ML-Agents] Setup stopped because the generated enemy pipeline needs the four enemy archetypes and Monster.prefab as visual base.");
				return;
			}

			ConfigureWaveManager(references.WaveManager, enemyPrefabs, enemyVisualBase);

			ConfigureAgent(agent, references, syntheticMouse, towerPrefabs);
			ConfigureBehavior(behaviorParameters);
			ConfigureDecisionRequester(decisionRequester);
			var existingAgents = Object.FindObjectsByType<TowerDefenceAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			for (var i = 0; i < existingAgents.Length; i++)
			{
				if (existingAgents[i] == agent)
					continue;

				var existingBehavior = GetOrAddComponent<BehaviorParameters>(existingAgents[i].gameObject);
				var existingRequester = GetOrAddComponent<DecisionRequester>(existingAgents[i].gameObject);
				ConfigureBehavior(existingBehavior);
				ConfigureDecisionRequester(existingRequester);
			}

			var balanceAgentObject = GameObject.Find(BalanceAgentObjectName);
			if (balanceAgentObject == null)
			{
				balanceAgentObject = new GameObject(BalanceAgentObjectName);
				Undo.RegisterCreatedObjectUndo(balanceAgentObject, "Create TD ML Balance Agent");
			}

			var balanceAgent = GetOrAddComponent<TowerDefenceBalancerAgent>(balanceAgentObject);
			var balanceBehaviorParameters = GetOrAddComponent<BehaviorParameters>(balanceAgentObject);
			var balanceDecisionRequester = GetOrAddComponent<DecisionRequester>(balanceAgentObject);
			ConfigureBalanceAgent(balanceAgent, references);
			ConfigureBalanceBehavior(balanceBehaviorParameters);
			ConfigureDecisionRequester(balanceDecisionRequester);

			var enemyLevelAgentObject = GameObject.Find(EnemyLevelAgentObjectName);
			if (enemyLevelAgentObject == null)
			{
				enemyLevelAgentObject = new GameObject(EnemyLevelAgentObjectName);
				Undo.RegisterCreatedObjectUndo(enemyLevelAgentObject, "Create TD ML Enemy Level Agent");
			}

			var enemyLevelAgent = GetOrAddComponent<TowerDefenceEnemyLevelAgent>(enemyLevelAgentObject);
			var enemyLevelBehaviorParameters = GetOrAddComponent<BehaviorParameters>(enemyLevelAgentObject);
			var enemyLevelDecisionRequester = GetOrAddComponent<DecisionRequester>(enemyLevelAgentObject);
			ConfigureEnemyLevelAgent(enemyLevelAgent, references);
			ConfigureEnemyLevelBehavior(enemyLevelBehaviorParameters);
			ConfigureDecisionRequester(enemyLevelDecisionRequester);
			EditorSceneManager.MarkSceneDirty(scene);
			if (!EditorSceneManager.SaveScene(scene))
			{
				Debug.LogError("[TD ML-Agents] Gameplay scene could not be saved.");
				return;
			}

			Debug.Log($"[TD ML-Agents] Setup complete: {AgentObjectName} ({BehaviorName}) + {BalanceAgentObjectName} ({BalanceBehaviorName}) + {EnemyLevelAgentObjectName} ({EnemyLevelBehaviorName}).");
		}

		[MenuItem("TD/ML-Agents/Play Mode/Enable Gameplay Smoke Isolation")]
		private static void EnableGameplaySmokeIsolation()
		{
			if (!EditorApplication.isPlaying)
			{
				Debug.LogError("[TD ML-Agents] Gameplay smoke isolation requires Play Mode.");
				return;
			}

			if (runtimeIsolationActive)
				return;

			RuntimeIsolationStates.Clear();
			runtimeIsolationActive = true;
			SceneManager.sceneLoaded -= OnRuntimeIsolationSceneLoaded;
			SceneManager.sceneLoaded += OnRuntimeIsolationSceneLoaded;
			ApplyRuntimeIsolation();
			Debug.Log($"[TD ML-Agents] Runtime gameplay smoke isolation enabled for {RuntimeIsolationStates.Count} diagnostic agents; player agent remains active; terminal hold enabled; scene assets were not modified.");
		}

		private static void ApplyRuntimeIsolation()
		{
			for (var staleIndex = RuntimeIsolationStates.Count - 1; staleIndex >= 0; staleIndex--)
			{
				if (RuntimeIsolationStates[staleIndex].Agent == null)
					RuntimeIsolationStates.RemoveAt(staleIndex);
			}
			for (var staleIndex = RuntimePlayerEpisodeRestartStates.Count - 1; staleIndex >= 0; staleIndex--)
			{
				if (RuntimePlayerEpisodeRestartStates[staleIndex].Agent == null)
					RuntimePlayerEpisodeRestartStates.RemoveAt(staleIndex);
			}

			ApplyPlayerEpisodeTerminalHold();

			var behaviors = Object.FindObjectsByType<BehaviorParameters>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			for (var i = 0; i < behaviors.Length; i++)
			{
				var agent = behaviors[i].GetComponent<Agent>();
				if (agent == null)
					continue;
				if (!(agent is TowerDefenceBalancerAgent) && !(agent is TowerDefenceEnemyLevelAgent))
					continue;

				var alreadyTracked = false;
				for (var stateIndex = 0; stateIndex < RuntimeIsolationStates.Count; stateIndex++)
				{
					if (RuntimeIsolationStates[stateIndex].Agent == agent)
					{
						alreadyTracked = true;
						break;
					}
				}

				if (alreadyTracked)
					continue;

				RuntimeIsolationStates.Add(new AgentIsolationState(agent, behaviors[i].BehaviorType, agent.enabled));
				SetTrainingMode(agent, false);
				behaviors[i].BehaviorType = BehaviorType.HeuristicOnly;
				agent.enabled = false;
			}
		}

		private static void ApplyPlayerEpisodeTerminalHold()
		{
			var agents = Object.FindObjectsByType<TowerDefenceAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			for (var i = 0; i < agents.Length; i++)
			{
				var agent = agents[i];
				if (agent == null || !agent.isActiveAndEnabled)
					continue;

				var alreadyTracked = false;
				for (var stateIndex = 0; stateIndex < RuntimePlayerEpisodeRestartStates.Count; stateIndex++)
				{
					if (RuntimePlayerEpisodeRestartStates[stateIndex].Agent == agent)
					{
						alreadyTracked = true;
						break;
					}
				}

				if (alreadyTracked)
					continue;

				RuntimePlayerEpisodeRestartStates.Add(new PlayerEpisodeRestartState(agent, agent.RestartSceneOnEpisodeReset));
				agent.RestartSceneOnEpisodeReset = false;
			}
		}

		private static void OnRuntimeIsolationSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			if (!runtimeIsolationActive)
				return;

			ApplyRuntimeIsolation();
			Debug.Log($"[TD ML-Agents] Runtime gameplay smoke isolation reapplied after scene load for {RuntimeIsolationStates.Count} diagnostic agents; terminal hold active.");
		}

		[MenuItem("TD/ML-Agents/Play Mode/Disable Gameplay Smoke Isolation")]
		private static void DisableGameplaySmokeIsolation()
		{
			RestoreRuntimeIsolation();
		}

		[MenuItem("TD/ML-Agents/Play Mode/Enable Leak Smoke (Disable Tower Damage)")]
		private static void EnableLeakSmoke()
		{
			if (!EditorApplication.isPlaying)
			{
				Debug.LogError("[TD ML-Agents] Leak smoke requires Play Mode.");
				return;
			}

			if (runtimeTowerIsolationActive)
				return;

			RuntimeTowerIsolationStates.Clear();
			var towers = Object.FindObjectsByType<Tower>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			for (var i = 0; i < towers.Length; i++)
			{
				RuntimeTowerIsolationStates.Add(new TowerIsolationState(towers[i], towers[i].enabled));
				towers[i].enabled = false;
			}

			runtimeTowerIsolationActive = true;
			Debug.Log($"[TD ML-Agents] Leak smoke enabled for {RuntimeTowerIsolationStates.Count} towers; scene assets were not modified.");
		}

		[MenuItem("TD/ML-Agents/Play Mode/Disable Leak Smoke")]
		private static void DisableLeakSmoke()
		{
			RestoreRuntimeTowerIsolation();
		}

		[InitializeOnLoadMethod]
		private static void RegisterIsolationCleanup()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingPlayMode)
			{
				RestoreRuntimeIsolation();
				RestoreRuntimeTowerIsolation();
			}
		}

		private static void RestoreRuntimeIsolation()
		{
			if (!runtimeIsolationActive)
				return;

			SceneManager.sceneLoaded -= OnRuntimeIsolationSceneLoaded;
			for (var i = 0; i < RuntimeIsolationStates.Count; i++)
			{
				var state = RuntimeIsolationStates[i];
				if (state.Agent != null)
					SetTrainingMode(state.Agent, state.TrainingMode);
				if (state.Agent != null)
					state.Agent.enabled = state.AgentEnabled;
				if (state.BehaviorParameters != null)
					state.BehaviorParameters.BehaviorType = state.BehaviorType;
			}
			for (var i = 0; i < RuntimePlayerEpisodeRestartStates.Count; i++)
			{
				var state = RuntimePlayerEpisodeRestartStates[i];
				if (state.Agent != null)
					state.Agent.RestartSceneOnEpisodeReset = state.RestartSceneOnEpisodeReset;
			}

			RuntimeIsolationStates.Clear();
			RuntimePlayerEpisodeRestartStates.Clear();
			runtimeIsolationActive = false;
			Debug.Log("[TD ML-Agents] Runtime gameplay smoke isolation restored.");
		}

		private static bool GetTrainingMode(Agent agent)
		{
			if (agent is TowerDefenceAgent playerAgent)
				return playerAgent.TrainingMode;
			if (agent is TowerDefenceBalancerAgent balanceAgent)
				return balanceAgent.TrainingMode;
			if (agent is TowerDefenceEnemyLevelAgent enemyLevelAgent)
				return enemyLevelAgent.TrainingMode;

			return false;
		}

		private static void SetTrainingMode(Agent agent, bool value)
		{
			if (agent is TowerDefenceAgent playerAgent)
				playerAgent.TrainingMode = value;
			else if (agent is TowerDefenceBalancerAgent balanceAgent)
				balanceAgent.TrainingMode = value;
			else if (agent is TowerDefenceEnemyLevelAgent enemyLevelAgent)
				enemyLevelAgent.TrainingMode = value;
		}

		private static void RestoreRuntimeTowerIsolation()
		{
			if (!runtimeTowerIsolationActive)
				return;

			for (var i = 0; i < RuntimeTowerIsolationStates.Count; i++)
			{
				var state = RuntimeTowerIsolationStates[i];
				if (state.Tower != null)
					state.Tower.enabled = state.TowerEnabled;
			}

			RuntimeTowerIsolationStates.Clear();
			runtimeTowerIsolationActive = false;
			Debug.Log("[TD ML-Agents] Leak smoke tower state restored.");
		}

		private readonly struct AgentIsolationState
		{
			public AgentIsolationState(Agent agent, BehaviorType behaviorType, bool agentEnabled)
			{
				Agent = agent;
				BehaviorParameters = agent.GetComponent<BehaviorParameters>();
				BehaviorType = behaviorType;
				AgentEnabled = agentEnabled;
				TrainingMode = GetTrainingMode(agent);
			}

			public Agent Agent { get; }
			public BehaviorParameters BehaviorParameters { get; }
			public BehaviorType BehaviorType { get; }
			public bool AgentEnabled { get; }
			public bool TrainingMode { get; }
		}

		private readonly struct PlayerEpisodeRestartState
		{
			public PlayerEpisodeRestartState(TowerDefenceAgent agent, bool restartSceneOnEpisodeReset)
			{
				Agent = agent;
				RestartSceneOnEpisodeReset = restartSceneOnEpisodeReset;
			}

			public TowerDefenceAgent Agent { get; }
			public bool RestartSceneOnEpisodeReset { get; }
		}

		private readonly struct TowerIsolationState
		{
			public TowerIsolationState(Tower tower, bool towerEnabled)
			{
				Tower = tower;
				TowerEnabled = towerEnabled;
			}

			public Tower Tower { get; }
			public bool TowerEnabled { get; }
		}

		private static void ConfigureAgent(TowerDefenceAgent agent, GameplayReferences references, SyntheticMouse syntheticMouse, List<Tower> towerPrefabs)
		{
			var serializedAgent = new SerializedObject(agent);
			serializedAgent.Update();
			SetObject(serializedAgent, "_gameManager", references.GameManager);
			SetObject(serializedAgent, "_waveManager", references.WaveManager);
			SetObject(serializedAgent, "_resourceManager", references.ResourceManager);
			SetObject(serializedAgent, "_gameplayTelemetry", references.GameplayTelemetry);
			SetObject(serializedAgent, "_playerBase", references.PlayerBase);
			SetObject(serializedAgent, "_towerPlacementSystem", references.TowerPlacementSystem);
			SetObject(serializedAgent, "_tilePlacementSystem", references.TilePlacementSystem);
			SetObject(serializedAgent, "_tileMapManager", references.TileMapManager);
			SetObject(serializedAgent, "_gameplayCamera", references.GameplayCamera);
			SetObject(serializedAgent, "_syntheticMouse", syntheticMouse);
			var towerArray = serializedAgent.FindProperty("_towerPrefabs");
			towerArray.arraySize = TowerDefenceAgent.MaxTowerPrefabs;
			for (var i = 0; i < TowerDefenceAgent.MaxTowerPrefabs; i++)
			{
				towerArray.GetArrayElementAtIndex(i).objectReferenceValue = i < towerPrefabs.Count ? towerPrefabs[i] : null;
			}
			serializedAgent.FindProperty("_trainingMode").boolValue = true;
			serializedAgent.FindProperty("_restartSceneOnEpisodeReset").boolValue = true;
			serializedAgent.FindProperty("_applyMlTestTimeScale").boolValue = true;
			serializedAgent.FindProperty("_mlTestTimeScale").floatValue = TowerDefenceAgent.DefaultMlTestTimeScale;
			serializedAgent.FindProperty("_episodeTimeLimitSeconds").floatValue = TowerDefenceAgent.DefaultEpisodeTimeLimitSeconds;
			serializedAgent.ApplyModifiedProperties();
			Undo.RecordObject(agent, "Configure TD ML Agent");
			agent.MaxStep = MaxStep;
			EditorUtility.SetDirty(agent);
		}

		private static void ConfigureTelemetry(GameplayTelemetry telemetry, TileMapManager tileMapManager)
		{
			var serializedTelemetry = new SerializedObject(telemetry);
			serializedTelemetry.Update();
			SetObject(serializedTelemetry, "tileMapManager", tileMapManager);
			serializedTelemetry.ApplyModifiedProperties();
			EditorUtility.SetDirty(telemetry);
		}

		private static void ConfigureBehavior(BehaviorParameters behaviorParameters)
		{
			var serializedBehavior = new SerializedObject(behaviorParameters);
			serializedBehavior.Update();
			serializedBehavior.FindProperty("m_BehaviorName").stringValue = BehaviorName;
			serializedBehavior.FindProperty("m_BehaviorType").enumValueIndex = 0;
			var brain = serializedBehavior.FindProperty("m_BrainParameters");
			brain.FindPropertyRelative("VectorObservationSize").intValue = TowerDefenceAgent.ObservationSize;
			brain.FindPropertyRelative("NumStackedVectorObservations").intValue = 1;
			var branchSizes = brain.FindPropertyRelative("m_ActionSpec").FindPropertyRelative("BranchSizes");
			branchSizes.arraySize = TowerDefenceAgent.ActionBranchCount;
			branchSizes.GetArrayElementAtIndex(0).intValue = TowerDefenceAgent.ActionBranchSize;
			branchSizes.GetArrayElementAtIndex(1).intValue = TowerDefenceAgent.TowerBranchSize;
			branchSizes.GetArrayElementAtIndex(2).intValue = TowerDefenceAgent.PlacementBranchSize;
			branchSizes.GetArrayElementAtIndex(3).intValue = TowerDefenceAgent.TileOptionBranchSize;
			branchSizes.GetArrayElementAtIndex(4).intValue = TowerDefenceAgent.UpgradeTargetBranchSize;
			serializedBehavior.ApplyModifiedProperties();
			EditorUtility.SetDirty(behaviorParameters);
		}

		private static void ConfigureBalanceAgent(TowerDefenceBalancerAgent agent, GameplayReferences references)
		{
			var serializedAgent = new SerializedObject(agent);
			serializedAgent.Update();
			SetObject(serializedAgent, "_gameManager", references.GameManager);
			SetObject(serializedAgent, "_waveManager", references.WaveManager);
			SetObject(serializedAgent, "_gameplayTelemetry", references.GameplayTelemetry);
			serializedAgent.FindProperty("_trainingMode").boolValue = true;
			serializedAgent.FindProperty("_episodeTimeLimitSeconds").floatValue = TowerDefenceBalancerAgent.DefaultEpisodeTimeLimitSeconds;
			serializedAgent.ApplyModifiedProperties();
			Undo.RecordObject(agent, "Configure TD ML Balance Agent");
			agent.MaxStep = MaxStep;
			EditorUtility.SetDirty(agent);
		}

		private static void ConfigureBalanceBehavior(BehaviorParameters behaviorParameters)
		{
			var serializedBehavior = new SerializedObject(behaviorParameters);
			serializedBehavior.Update();
			serializedBehavior.FindProperty("m_BehaviorName").stringValue = BalanceBehaviorName;
			serializedBehavior.FindProperty("m_BehaviorType").enumValueIndex = 0;
			var brain = serializedBehavior.FindProperty("m_BrainParameters");
			brain.FindPropertyRelative("VectorObservationSize").intValue = TowerDefenceBalancerAgent.ObservationSize;
			brain.FindPropertyRelative("NumStackedVectorObservations").intValue = 1;
			var branchSizes = brain.FindPropertyRelative("m_ActionSpec").FindPropertyRelative("BranchSizes");
			branchSizes.arraySize = TowerDefenceBalancerAgent.ActionBranchCount;
			for (var i = 0; i < TowerDefenceBalancerAgent.ActionBranchCount; i++)
				branchSizes.GetArrayElementAtIndex(i).intValue = TowerDefenceBalancerAgent.ActionBranchSize;
			serializedBehavior.ApplyModifiedProperties();
			EditorUtility.SetDirty(behaviorParameters);
		}

		private static void ConfigureEnemyLevelAgent(TowerDefenceEnemyLevelAgent agent, GameplayReferences references)
		{
			var serializedAgent = new SerializedObject(agent);
			serializedAgent.Update();
			SetObject(serializedAgent, "_gameManager", references.GameManager);
			SetObject(serializedAgent, "_waveManager", references.WaveManager);
			SetObject(serializedAgent, "_gameplayTelemetry", references.GameplayTelemetry);
			serializedAgent.FindProperty("_trainingMode").boolValue = true;
			serializedAgent.FindProperty("_episodeTimeLimitSeconds").floatValue = TowerDefenceEnemyLevelAgent.DefaultEpisodeTimeLimitSeconds;
			serializedAgent.ApplyModifiedProperties();
			Undo.RecordObject(agent, "Configure TD ML Enemy Level Agent");
			agent.MaxStep = MaxStep;
			EditorUtility.SetDirty(agent);
		}

		private static void ConfigureEnemyLevelBehavior(BehaviorParameters behaviorParameters)
		{
			var serializedBehavior = new SerializedObject(behaviorParameters);
			serializedBehavior.Update();
			serializedBehavior.FindProperty("m_BehaviorName").stringValue = EnemyLevelBehaviorName;
			serializedBehavior.FindProperty("m_BehaviorType").enumValueIndex = 0;
			var brain = serializedBehavior.FindProperty("m_BrainParameters");
			brain.FindPropertyRelative("VectorObservationSize").intValue = TowerDefenceEnemyLevelAgent.ObservationSize;
			brain.FindPropertyRelative("NumStackedVectorObservations").intValue = 1;
			var branchSizes = brain.FindPropertyRelative("m_ActionSpec").FindPropertyRelative("BranchSizes");
			branchSizes.arraySize = TowerDefenceEnemyLevelAgent.ActionBranchCount;
			for (var i = 0; i < 3; i++)
				branchSizes.GetArrayElementAtIndex(i).intValue = TowerDefenceEnemyLevelAgent.ActionBranchSize;
			branchSizes.GetArrayElementAtIndex(3).intValue = TowerDefenceEnemyLevelAgent.SeedBranchSize;
			for (var i = 4; i <= 6; i++)
				branchSizes.GetArrayElementAtIndex(i).intValue = TowerDefenceEnemyLevelAgent.ArchetypeBranchSize;
			for (var i = 7; i <= 9; i++)
				branchSizes.GetArrayElementAtIndex(i).intValue = TowerDefenceEnemyLevelAgent.CountBranchSize;
			branchSizes.GetArrayElementAtIndex(10).intValue = TowerDefenceEnemyLevelAgent.PacingBranchSize;
			serializedBehavior.ApplyModifiedProperties();
			EditorUtility.SetDirty(behaviorParameters);
		}

		private static void ConfigureWaveManager(WaveManager waveManager, List<GameObject> enemyPrefabs, GameObject visualBase)
		{
			var serializedWaveManager = new SerializedObject(waveManager);
			serializedWaveManager.Update();
			var archetypes = serializedWaveManager.FindProperty("enemyArchetypes");
			archetypes.arraySize = enemyPrefabs.Count;
			for (var i = 0; i < enemyPrefabs.Count; i++)
				archetypes.GetArrayElementAtIndex(i).objectReferenceValue = enemyPrefabs[i];
			serializedWaveManager.FindProperty("enemyVisualGenerationBase").objectReferenceValue = visualBase;
			serializedWaveManager.ApplyModifiedProperties();
			EditorUtility.SetDirty(waveManager);
		}

		private static void ConfigureDecisionRequester(DecisionRequester decisionRequester)
		{
			Undo.RecordObject(decisionRequester, "Configure TD ML Decision Requester");
			decisionRequester.DecisionPeriod = DecisionPeriod;
			decisionRequester.DecisionStep = 0;
			decisionRequester.TakeActionsBetweenDecisions = false;
			EditorUtility.SetDirty(decisionRequester);
		}

		private static GameplayReferences FindReferences()
		{
			return new GameplayReferences
			{
				GameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include),
				WaveManager = Object.FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include),
				ResourceManager = Object.FindFirstObjectByType<ResourceManager>(FindObjectsInactive.Include),
				GameplayTelemetry = Object.FindFirstObjectByType<GameplayTelemetry>(FindObjectsInactive.Include),
				PlayerBase = Object.FindFirstObjectByType<PlayerBase>(FindObjectsInactive.Include),
				TowerPlacementSystem = Object.FindFirstObjectByType<TowerPlacementSystem>(FindObjectsInactive.Include),
				TilePlacementSystem = Object.FindFirstObjectByType<TilePlacementSystem>(FindObjectsInactive.Include),
				TileMapManager = Object.FindFirstObjectByType<TileMapManager>(FindObjectsInactive.Include),
				GameplayCamera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include)
			};
		}

		private static List<Tower> FindTowerPrefabs()
		{
			var result = new List<Tower>();
			var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Towers" });
			System.Array.Sort(guids);
			foreach (var guid in guids)
			{
				var assetPath = AssetDatabase.GUIDToAssetPath(guid);
				if (assetPath.Contains("/Generated/"))
					continue;

				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
				var tower = prefab != null ? prefab.GetComponent<Tower>() : null;
				if (tower != null)
				{
					result.Add(tower);
					if (result.Count == TowerDefenceAgent.MaxTowerPrefabs)
					{
						break;
					}
				}
			}
			return result;
		}

		private static List<GameObject> FindEnemyPrefabs()
		{
			var result = new List<GameObject>();
			var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemies" });
			System.Array.Sort(guids);
			foreach (var guid in guids)
			{
				var assetPath = AssetDatabase.GUIDToAssetPath(guid);
				if (assetPath.Contains("/Generated/"))
					continue;

				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
				if (prefab == null || prefab.GetComponent<TD.Monsters.MonsterStats>() == null ||
					prefab.GetComponent<TD.Monsters.MonsterHealth>() == null || prefab.GetComponent<TD.Monsters.MonsterMove>() == null)
					continue;

				result.Add(prefab);
				if (result.Count == TowerDefenceEnemyLevelAgent.ArchetypeBranchSize - 1)
					break;
			}

			return result;
		}

		private static T GetOrAddComponent<T>(GameObject target) where T : Component
		{
			var component = target.GetComponent<T>();
			return component != null ? component : Undo.AddComponent<T>(target);
		}

		private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
		{
			serializedObject.FindProperty(propertyName).objectReferenceValue = value;
		}

		private struct GameplayReferences
		{
			public GameManager GameManager;
			public WaveManager WaveManager;
			public ResourceManager ResourceManager;
			public GameplayTelemetry GameplayTelemetry;
			public PlayerBase PlayerBase;
			public TowerPlacementSystem TowerPlacementSystem;
			public TilePlacementSystem TilePlacementSystem;
			public TileMapManager TileMapManager;
			public Camera GameplayCamera;
			public bool IsComplete => GameManager != null && WaveManager != null && ResourceManager != null && GameplayTelemetry != null && PlayerBase != null && TowerPlacementSystem != null && TilePlacementSystem != null && TileMapManager != null && GameplayCamera != null;
		}
	}
}
