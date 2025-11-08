using TD.Plugins.GameDesign;
using UnityEditor;
using UnityEngine;

namespace TD.Towers.Editor
{
	public class TowerStatsGridWindow : GenericAssetsGridWindow<TowerStatsSO>
	{
		[MenuItem("Window/TowerStats Grid Window")]
		private static void OpenWindow()
		{
			var window = GetWindow<TowerStatsGridWindow>("MyScriptableObject Grid");
			window.position = new Rect(100, 100, 1200, 800);
		}
	}
}