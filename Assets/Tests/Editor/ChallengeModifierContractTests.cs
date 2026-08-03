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
			var gameObject = new GameObject("ChallengeModifierContractTests");
			var waveManager = gameObject.AddComponent<WaveManager>();

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

			waveManager.SelectChallengeModifier(ChallengeModifier.None);
			Assert.That(waveManager.ActiveChallengeModifier, Is.EqualTo(ChallengeModifier.ReinforcedHorde));

			Object.DestroyImmediate(gameObject);

			var restartedObject = new GameObject("ChallengeModifierContractTests.Restarted");
			var restartedWaveManager = restartedObject.AddComponent<WaveManager>();
			Assert.That(restartedWaveManager.ActiveChallengeModifier, Is.EqualTo(ChallengeModifier.None));
			Assert.That(restartedWaveManager.CanSelectChallengeModifier, Is.True);
			Assert.That(restartedWaveManager.EnemyCountFactor, Is.EqualTo(1f));
			Assert.That(restartedWaveManager.EnemyHealthFactor, Is.EqualTo(1f));
			Assert.That(restartedWaveManager.CompletionRewardFactor, Is.EqualTo(1f));
			Object.DestroyImmediate(restartedObject);
		}
	}
}