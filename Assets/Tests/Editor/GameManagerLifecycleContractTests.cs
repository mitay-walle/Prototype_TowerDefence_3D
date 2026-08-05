using System.Reflection;
using NUnit.Framework;
using TD.GameLoop;
using UnityEngine.Events;

namespace TD.Tests
{
	public class GameManagerLifecycleContractTests
	{
		[Test]
		public void RestartPublishesAdditiveLifecycleEvent()
		{
			var eventField = typeof(GameManager).GetField(nameof(GameManager.onRestartRequested), BindingFlags.Instance | BindingFlags.Public);

			Assert.That(eventField, Is.Not.Null);
			Assert.That(eventField.FieldType, Is.EqualTo(typeof(UnityEvent)));
		}

		[Test]
		public void TerminalRunResultIsImmutableAndPublishedByGameManager()
		{
			var eventField = typeof(GameManager).GetField(nameof(GameManager.onRunFinished), BindingFlags.Instance | BindingFlags.Public);

			Assert.That(eventField, Is.Not.Null);
			Assert.That(eventField.FieldType, Is.EqualTo(typeof(RunResultEvent)));
			Assert.That(typeof(GameManager).GetProperty(nameof(GameManager.LastRunResult)), Is.Not.Null);
			Assert.That(typeof(RunResult).GetProperty(nameof(RunResult.RunId)).CanWrite, Is.False);
			Assert.That(typeof(RunResult).GetProperty(nameof(RunResult.Outcome)).CanWrite, Is.False);
			Assert.That(typeof(RunResult).GetProperty(nameof(RunResult.WavesCompleted)).CanWrite, Is.False);
		}

		[Test]
		public void RunResultCarriesTerminalSummary()
		{
			var result = new RunResult("run-1", 1, RunOutcome.Defeat, 42, "ReinforcedHorde", 2, 3, 7, 20, 15, 4, 2, 12.5f, "test");

			Assert.That(result.RunId, Is.EqualTo("run-1"));
			Assert.That(result.Outcome, Is.EqualTo(RunOutcome.Defeat));
			Assert.That(result.Seed, Is.EqualTo(42));
			Assert.That(result.WavesCompleted, Is.EqualTo(2));
			Assert.That(result.FinalWaveIndex, Is.EqualTo(3));
			Assert.That(result.BaseHealth, Is.EqualTo(7));
			Assert.That(result.DurationSeconds, Is.EqualTo(12.5f));
		}

		[Test]
		public void WaveManagerExposesTerminalCancellationOwner()
		{
			var stopMethod = typeof(WaveManager).GetMethod(nameof(WaveManager.ForceStopWave), BindingFlags.Instance | BindingFlags.Public);

			Assert.That(stopMethod, Is.Not.Null);
			Assert.That(stopMethod.ReturnType, Is.EqualTo(typeof(void)));
		}
	}
}
