using UnityEngine;

namespace TD.Stats
{
	[CreateAssetMenu(fileName = "New Stat Modifier", menuName = "Tower Defence/Stat Modifier")]
	public class ModifierSO : ScriptableObject
	{
		[SerializeReference]
		public StatModifier modifier = new BasicModifier();

		public float Apply(float currentValue, float baseValue, float sumBeforeMultipliers)
		{
			return modifier?.Calculate(currentValue, baseValue, sumBeforeMultipliers) ?? currentValue;
		}

		public void OnAdd(IStats towerStats) => modifier?.OnAdd(towerStats);
		public void OnRemove(IStats towerStats) => modifier?.OnRemove(towerStats);

		public override string ToString() => $"{modifier}";
	}
}