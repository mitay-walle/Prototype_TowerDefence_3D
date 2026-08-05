using System.Reflection;
using NUnit.Framework;
using TD.GameLoop;
using UnityEngine;

namespace TD.Tests
{
	public class ChallengeModifierContractTests
	{
		[Test]
		public void ReinforcedHordeAppliesFactorsAndResetsForNewOwner()
		{
			var gameManagerObject = new GameObject("ChallengeModifierContractTests.GameManager");
			var waveManagerObject = new GameObject("ChallengeModifierContractTests");
			GameObject restartedObject = null;

			try
			{
				var gameManager = gameManagerObject.AddComponent<GameManager>();
				typeof(GameManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(gameManager, null);
				gameManager.BeginMapBuild();
				gameManager.CompleteMapBuild();

				var waveManager = waveManagerObject.AddComponent<WaveManager>();

				Assert.That(waveManager.ActiveChallengeModifier, Is.EqualTo(ChallengeModifier.None));
				Assert.That(waveManager.CanSelectChallengeModifier, Is.True);
				Assert.That(waveManager.EnemyCountFactor, Is.EqualTo(1f));
				Assert.That(waveManager.EnemyHealthFactor, Is.EqualTo(1f));
				Assert.That(waveManager.CompletionRewardFactor, Is.EqualTo(1f));

				waveManager.SelectChallengeModifier(ChallengeModifier.ReinforcedHorde);

				Assert.That(waveManager.ActiveChallengeModifier, Is.EqualTo(ChallengeModifier.ReinforcedHorde));
				Assert.That(waveManager.CanSelectChallengeModifier, Is.False);
				Assert.That(Mathf.RoundToInt(7f * waveManager.EnemyCountFactor), Is.EqualTo(9));
				Assert.That(Mathf.RoundToInt(100f * waveManager.EnemyHealthFactor), Is.EqualTo(125));
				Assert.That(Mathf.RoundToInt(50f * waveManager.CompletionRewardFactor), Is.EqualTo(75));
				Assert.That(waveManager.ApplyAdaptiveBalance(1.2f, 0.8f, 1.15f, 0.8f), Is.True);
				Assert.That(waveManager.AdaptiveEnemyHealthFactor, Is.EqualTo(1.2f));
				Assert.That(waveManager.AdaptiveEnemyCountFactor, Is.EqualTo(0.8f));
				Assert.That(waveManager.AdaptiveEnemySpeedFactor, Is.EqualTo(1.15f));
				Assert.That(waveManager.AdaptiveRewardFactor, Is.EqualTo(0.8f));
				Assert.That(waveManager.AdaptiveDifficultyScore, Is.EqualTo(0.75f).Within(0.0001f));

				waveManager.SelectChallengeModifier(ChallengeModifier.None);
				Assert.That(waveManager.ActiveChallengeModifier, Is.EqualTo(ChallengeModifier.ReinforcedHorde));

				Object.DestroyImmediate(waveManagerObject);
				waveManagerObject = null;

				restartedObject = new GameObject("ChallengeModifierContractTests.Restarted");
				var restartedWaveManager = restartedObject.AddComponent<WaveManager>();
				Assert.That(restartedWaveManager.ActiveChallengeModifier, Is.EqualTo(ChallengeModifier.None));
				Assert.That(restartedWaveManager.CanSelectChallengeModifier, Is.True);
				Assert.That(restartedWaveManager.EnemyCountFactor, Is.EqualTo(1f));
				Assert.That(restartedWaveManager.EnemyHealthFactor, Is.EqualTo(1f));
				Assert.That(restartedWaveManager.CompletionRewardFactor, Is.EqualTo(1f));
				Assert.That(restartedWaveManager.AdaptiveDifficultyScore, Is.EqualTo(0.5f).Within(0.0001f));
			}
			finally
			{
				if (restartedObject != null) Object.DestroyImmediate(restartedObject);
				if (waveManagerObject != null) Object.DestroyImmediate(waveManagerObject);
				Object.DestroyImmediate(gameManagerObject);
			}
		}
	}
}
