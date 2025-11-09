using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Plugins.UI.Utility
{
	public static class SelectableColorBlock
	{
		[MenuItem ("CONTEXT/Selectable/Copy Color Block")]
		public static void CopyBlock(MenuCommand command)
		{
			EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson((command.context as Selectable).colors);
		}

		[MenuItem ("CONTEXT/Selectable/Paste Color Block")]
		public static void PasteBlock(MenuCommand command)
		{
			Undo.RecordObject(command.context, "paste colors");
			(command.context as Selectable).colors = JsonUtility.FromJson<ColorBlock>(EditorGUIUtility.systemCopyBuffer);
			EditorUtility.SetDirty(command.context);
		}
		

	}
}