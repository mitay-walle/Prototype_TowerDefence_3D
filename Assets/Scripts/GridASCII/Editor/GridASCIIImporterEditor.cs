using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace TD.GridASCII.Editor
{
	[CustomEditor(typeof(GridASCIIImporter))]
	public class GridASCIIImporterEditor : ScriptedImporterEditor
	{
		private CharPalette palette;

		public override void OnEnable()
		{
			base.OnEnable();
			var importer = target as GridASCIIImporter;
			if (importer != null)
			{
				if (string.IsNullOrEmpty(importer.userData))
				{
					palette = new CharPalette();
					importer.userData = palette.Serialize();
				}
				else
				{
					try
					{
						palette = JsonUtility.FromJson<CharPalette>(importer.userData);
					}
					catch
					{
						palette = new CharPalette();
					}
				}
			}
		}

		public override void OnInspectorGUI()
		{
			var importer = target as GridASCIIImporter;
			if (importer == null) return;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Char Palette", EditorStyles.boldLabel);

			bool changed = false;

			for (int i = 0; i < palette.entries.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();

				EditorGUI.BeginChangeCheck();
				palette.entries[i].character = EditorGUILayout.TextField(palette.entries[i].character.ToString(), GUILayout.Width(30))[0];
				palette.entries[i].color = EditorGUILayout.ColorField(palette.entries[i].color);
				if (EditorGUI.EndChangeCheck())
				{
					changed = true;
				}

				if (GUILayout.Button("X", GUILayout.Width(25)))
				{
					palette.entries.RemoveAt(i);
					changed = true;
					break;
				}

				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button("Add Entry"))
			{
				palette.entries.Add(new CharPalette.Entry { character = '?', color = Color.white });
				changed = true;
			}

			if (changed)
			{
				importer.userData = palette.Serialize();
				EditorUtility.SetDirty(importer);
			}

			EditorGUILayout.Space();
			ApplyRevertGUI();
		}
	}
}