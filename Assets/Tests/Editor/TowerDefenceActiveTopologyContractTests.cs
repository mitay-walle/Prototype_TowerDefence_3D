using System.Linq;
using NUnit.Framework;
using TD.MLAgents;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TD.Tests
{
	public class TowerDefenceActiveTopologyContractTests
	{
		private const string GameplayScenePath = "Assets/Scenes/Gameplay.unity";

		[Test]
		public void GameplaySceneHasExactlyOneActivePlayerAgent()
		{
			EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

			var agents = Object.FindObjectsByType<TowerDefenceAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			Assert.That(agents, Is.Not.Empty);
			Assert.That(agents.Count(agent => agent.gameObject.activeInHierarchy), Is.EqualTo(1));
		}
	}
}
