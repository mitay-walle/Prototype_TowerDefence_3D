namespace TD.GameLoop
{
	public enum ChallengeModifier
	{
		None = 0,
		ReinforcedHorde = 1,
		ControlledPressure = 2,
		Swarm = 3,
		Juggernaut = 4,
		Blitz = 5,
		SlowBurn = 6,
		GlassCannon = 7,
		BountyHunt = 8,
		Attrition = 9,
		Stampede = 10,
		Fortified = 11,
		Endurance = 12,
		RushHour = 13,
		Rally = 14,
		Siege = 15,
		FragileHorde = 16,
		Overtime = 17
	}

	public static class ChallengeModifierCatalog
	{
		public const int SelectableCount = (int)ChallengeModifier.Overtime;

		public static bool IsSelectable(ChallengeModifier modifier)
		{
			return modifier >= ChallengeModifier.ReinforcedHorde && modifier <= ChallengeModifier.Overtime;
		}

		public static ChallengeModifier GetByIndex(int index)
		{
			if (index < (int)ChallengeModifier.ReinforcedHorde)
				return ChallengeModifier.ReinforcedHorde;

			if (index > SelectableCount)
				return ChallengeModifier.Overtime;

			return (ChallengeModifier)index;
		}

		public static ChallengeModifier GetNext(ChallengeModifier modifier)
		{
			var nextIndex = IsSelectable(modifier) ? (int)modifier + 1 : (int)ChallengeModifier.ReinforcedHorde;
			if (nextIndex > SelectableCount)
				nextIndex = (int)ChallengeModifier.ReinforcedHorde;

			return GetByIndex(nextIndex);
		}

		public static float GetEnemyCountFactor(ChallengeModifier modifier)
		{
			return modifier switch
			{
				ChallengeModifier.ReinforcedHorde => 1.25f,
				ChallengeModifier.ControlledPressure => 1.10f,
				ChallengeModifier.Swarm => 1.50f,
				ChallengeModifier.Juggernaut => 0.75f,
				ChallengeModifier.Blitz => 1.10f,
				ChallengeModifier.SlowBurn => 1.25f,
				ChallengeModifier.GlassCannon => 0.90f,
				ChallengeModifier.BountyHunt => 1.20f,
				ChallengeModifier.Attrition => 1.15f,
				ChallengeModifier.Stampede => 1.50f,
				ChallengeModifier.Fortified => 0.85f,
				ChallengeModifier.Endurance => 1.35f,
				ChallengeModifier.RushHour => 1.70f,
				ChallengeModifier.Rally => 1.20f,
				ChallengeModifier.Siege => 0.90f,
				ChallengeModifier.FragileHorde => 1.60f,
				ChallengeModifier.Overtime => 1.10f,
				_ => 1f
			};
		}

		public static float GetEnemyHealthFactor(ChallengeModifier modifier)
		{
			return modifier switch
			{
				ChallengeModifier.ReinforcedHorde => 1.25f,
				ChallengeModifier.ControlledPressure => 1.10f,
				ChallengeModifier.Swarm => 0.85f,
				ChallengeModifier.Juggernaut => 1.70f,
				ChallengeModifier.Blitz => 0.90f,
				ChallengeModifier.SlowBurn => 1.10f,
				ChallengeModifier.GlassCannon => 1.60f,
				ChallengeModifier.BountyHunt => 1f,
				ChallengeModifier.Attrition => 1.25f,
				ChallengeModifier.Stampede => 0.95f,
				ChallengeModifier.Fortified => 1.80f,
				ChallengeModifier.Endurance => 1.30f,
				ChallengeModifier.RushHour => 0.80f,
				ChallengeModifier.Rally => 1.15f,
				ChallengeModifier.Siege => 1.55f,
				ChallengeModifier.FragileHorde => 0.70f,
				ChallengeModifier.Overtime => 1.10f,
				_ => 1f
			};
		}

		public static float GetEnemySpeedFactor(ChallengeModifier modifier)
		{
			return modifier switch
			{
				ChallengeModifier.ControlledPressure => 1.05f,
				ChallengeModifier.Swarm => 1.05f,
				ChallengeModifier.Juggernaut => 0.80f,
				ChallengeModifier.Blitz => 1.35f,
				ChallengeModifier.SlowBurn => 0.80f,
				ChallengeModifier.GlassCannon => 1.10f,
				ChallengeModifier.Attrition => 1.15f,
				ChallengeModifier.Stampede => 1.25f,
				ChallengeModifier.Fortified => 0.95f,
				ChallengeModifier.Endurance => 0.90f,
				ChallengeModifier.RushHour => 1.20f,
				ChallengeModifier.Rally => 1.20f,
				ChallengeModifier.Siege => 0.85f,
				ChallengeModifier.Overtime => 1.30f,
				_ => 1f
			};
		}

		public static float GetCompletionRewardFactor(ChallengeModifier modifier)
		{
			return modifier switch
			{
				ChallengeModifier.ReinforcedHorde => 1.50f,
				ChallengeModifier.ControlledPressure => 1.25f,
				ChallengeModifier.Swarm => 1.25f,
				ChallengeModifier.Juggernaut => 1.45f,
				ChallengeModifier.Blitz => 1.35f,
				ChallengeModifier.SlowBurn => 1.35f,
				ChallengeModifier.GlassCannon => 1.50f,
				ChallengeModifier.BountyHunt => 1.75f,
				ChallengeModifier.Attrition => 1.60f,
				ChallengeModifier.Stampede => 1.55f,
				ChallengeModifier.Fortified => 1.55f,
				ChallengeModifier.Endurance => 1.65f,
				ChallengeModifier.RushHour => 1.60f,
				ChallengeModifier.Rally => 1.45f,
				ChallengeModifier.Siege => 1.70f,
				ChallengeModifier.FragileHorde => 1.30f,
				ChallengeModifier.Overtime => 1.75f,
				_ => 1f
			};
		}

		public static string GetDisplayName(ChallengeModifier modifier)
		{
			return modifier switch
			{
				ChallengeModifier.ReinforcedHorde => "Reinforced Horde",
				ChallengeModifier.ControlledPressure => "Controlled Pressure",
				ChallengeModifier.Swarm => "Swarm",
				ChallengeModifier.Juggernaut => "Juggernaut",
				ChallengeModifier.Blitz => "Blitz",
				ChallengeModifier.SlowBurn => "Slow Burn",
				ChallengeModifier.GlassCannon => "Glass Cannon",
				ChallengeModifier.BountyHunt => "Bounty Hunt",
				ChallengeModifier.Attrition => "Attrition",
				ChallengeModifier.Stampede => "Stampede",
				ChallengeModifier.Fortified => "Fortified",
				ChallengeModifier.Endurance => "Endurance",
				ChallengeModifier.RushHour => "Rush Hour",
				ChallengeModifier.Rally => "Rally",
				ChallengeModifier.Siege => "Siege",
				ChallengeModifier.FragileHorde => "Fragile Horde",
				ChallengeModifier.Overtime => "Overtime",
				_ => "None"
			};
		}
	}
}
