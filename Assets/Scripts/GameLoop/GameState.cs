namespace TD.GameLoop
{
	public enum GameState
	{
		Boot = 0,
		WaveActive = 1,
		Preparation = 2,
		Paused = 3,
		Defeat = 4,
		Victory = 5,
		MapBuild = 6,
		WaveResolve = 7,
		ChallengeSelection = 8,
		Initial = Boot,
		WavePreparing = Preparation,
		GameOver = Defeat,
	}
}
