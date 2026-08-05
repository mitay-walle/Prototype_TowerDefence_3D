using System.Reflection;
using NUnit.Framework;
using TD.GameLoop;
using UnityEngine.Events;
using UnityEngine;

namespace TD.Tests
{
	public class OptionalChallengeModifierContractTests
	{
		[Test]
		public void ChallengeSelectionRequiresOneModifierAndOffersManyNumericProfiles()
		{
			var gameManagerObject = new GameObject("OptionalChallengeModifierContractTests.GameManager");
			var waveManagerObject = new GameObject("OptionalChallengeModifierContractTests.WaveManager");

			try
			{
				var gameManager = gameManagerObject.AddComponent<GameManager>();
				gameManager.onGameStateChanged = new UnityEvent<GameState>();
				typeof(GameManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(gameManager, null);
				gameManager.BeginMapBuild();
				gameManager.CompleteMapBuild();

				var waveManager = waveManagerObject.AddComponent<WaveManager>();
				waveManager.onChallengeModifierSelected = new UnityEvent();
				var selectedCount = 0;
				waveManager.onChallengeModifierSelected.AddListener(() => selectedCount++);

				Assert.That(waveManager.CanSelectChallengeModifier, Is.True);
				waveManager.SelectChallengeModifier(ChallengeModifier.None);

				Assert.That(waveManager.ActiveChallengeModifier, Is.EqualTo(ChallengeModifier.None));
				Assert.That(waveManager.CanSelectChallengeModifier, Is.True);
				Assert.That(waveManager.EnemyCountFactor, Is.EqualTo(1f));
				Assert.That(waveManager.EnemyHealthFactor, Is.EqualTo(1f));
				Assert.That(waveManager.CompletionRewardFactor, Is.EqualTo(1f));
				Assert.That(selectedCount, Is.EqualTo(0));
				Assert.That(waveManager.ChallengeModifierOptionCount, Is.GreaterThanOrEqualTo(12));

				waveManager.CycleChallengeModifier();
				Assert.That(waveManager.PreviewChallengeModifier, Is.EqualTo(ChallengeModifier.ControlledPressure));
				Assert.That(waveManager.ConfirmChallengeModifierPreview(), Is.True);
				Assert.That(waveManager.ActiveChallengeModifier, Is.EqualTo(ChallengeModifier.ControlledPressure));
				Assert.That(waveManager.CanSelectChallengeModifier, Is.False);
				Assert.That(waveManager.EnemyCountFactor, Is.EqualTo(1.1f));
				Assert.That(waveManager.EnemyHealthFactor, Is.EqualTo(1.1f));
				Assert.That(waveManager.EnemySpeedFactor, Is.EqualTo(1.05f));
				Assert.That(waveManager.CompletionRewardFactor, Is.EqualTo(1.25f));
				Assert.That(selectedCount, Is.EqualTo(1));

				waveManager.SelectChallengeModifier(ChallengeModifier.Swarm);
				Assert.That(waveManager.ActiveChallengeModifier, Is.EqualTo(ChallengeModifier.ControlledPressure));
			}
			finally
			{
				Object.DestroyImmediate(waveManagerObject);
				Object.DestroyImmediate(gameManagerObject);
			}
		}
	}
}
