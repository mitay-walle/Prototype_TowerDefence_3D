using System;
using UnityEngine.Events;

namespace TD.GameLoop
{
	public enum RunOutcome
	{
		Victory,
		Defeat,
		Abandon
	}

	[Serializable]
	public sealed class RunResult
	{
		public RunResult(
			string runId,
			int resultVersion,
			RunOutcome outcome,
			int seed,
			string difficultyId,
			int wavesCompleted,
			int finalWaveIndex,
			int baseHealth,
			int baseMaxHealth,
			int currency,
			int enemiesKilled,
			int enemiesLeaked,
			float durationSeconds,
			string contentVersion)
		{
			RunId = runId ?? string.Empty;
			ResultVersion = resultVersion;
			Outcome = outcome;
			Seed = seed;
			DifficultyId = difficultyId ?? string.Empty;
			WavesCompleted = wavesCompleted;
			FinalWaveIndex = finalWaveIndex;
			BaseHealth = baseHealth;
			BaseMaxHealth = baseMaxHealth;
			Currency = currency;
			EnemiesKilled = enemiesKilled;
			EnemiesLeaked = enemiesLeaked;
			DurationSeconds = Math.Max(0f, durationSeconds);
			ContentVersion = contentVersion ?? string.Empty;
		}

		public string RunId { get; }
		public int ResultVersion { get; }
		public RunOutcome Outcome { get; }
		public int Seed { get; }
		public string DifficultyId { get; }
		public int WavesCompleted { get; }
		public int FinalWaveIndex { get; }
		public int BaseHealth { get; }
		public int BaseMaxHealth { get; }
		public int Currency { get; }
		public int EnemiesKilled { get; }
		public int EnemiesLeaked { get; }
		public float DurationSeconds { get; }
		public string ContentVersion { get; }
	}

	[Serializable]
	public sealed class RunResultEvent : UnityEvent<RunResult>
	{
	}
}
