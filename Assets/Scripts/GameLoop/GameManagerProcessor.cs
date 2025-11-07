#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace TD.GameLoop
{
	[UsedImplicitly]
	public class GameManagerProcessor : OdinAttributeProcessor<GameManager>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			switch (member.Name)
			{
				case nameof(GameManager.onVictory):
				case nameof(GameManager.onGameOver):
				case nameof(GameManager.onGamePaused):
				case nameof(GameManager.onGameUnpaused):
				case nameof(GameManager.onGameStarted):
				case nameof(GameManager.onGameStateChanged):
				{
					attributes.Add(new FoldoutGroupAttribute("Events"));
					break;
				}
			}
		}
	}
}
	#endif