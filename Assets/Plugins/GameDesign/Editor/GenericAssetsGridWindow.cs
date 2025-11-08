using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace TD.Plugins.GameDesign
{
	public abstract class GenericAssetsGridWindow<T> : OdinEditorWindow where T : UnityEngine.Object
	{
		public Vector2Int Count = new Vector2Int(3, 1);
		public float cellPadding = 6f;
		public string findFilter => $"t:{typeof(T).Name}";

		[ShowInInspector, ReadOnly]
		private int foundCount = 0;

		private Vector2 scrollPos2;
		private List<T> assets = new List<T>();
		private Dictionary<UnityEngine.Object, Editor> editors = new Dictionary<UnityEngine.Object, Editor>();
		private Dictionary<UnityEngine.Object, Vector2> editorScrollPositions = new Dictionary<UnityEngine.Object, Vector2>();

		protected override void OnEnable()
		{
			base.OnEnable();
			RefreshAssets();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			DestroyEditors();
		}

		private void RefreshAssets()
		{
			LoadAllAssets();
			Repaint();
		}

		private void LoadAllAssets()
		{
			assets.Clear();
			DestroyEditors();

			string typeName = typeof(T).Name;
			string filter = string.IsNullOrEmpty(findFilter) ? $"t:{typeName}" : findFilter;

			string[] guids = AssetDatabase.FindAssets(filter);
			foreach (var guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var obj = AssetDatabase.LoadAssetAtPath<T>(path);
				if (obj != null)
					assets.Add(obj);
			}

			foundCount = assets.Count;
		}

		private void DestroyEditors()
		{
			foreach (var kv in editors)
			{
				if (kv.Value != null)
					DestroyImmediate(kv.Value);
			}

			editors.Clear();
			editorScrollPositions.Clear();
		}

		protected override void OnImGUI()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar);

			if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
				RefreshAssets();

			if (GUILayout.Button("Select All", EditorStyles.toolbarButton, GUILayout.Width(110)))
			{
				if (assets.Count > 0)
				{
					Selection.objects = assets.ToArray();
					EditorGUIUtility.PingObject(assets[0]);
				}
			}

			GUILayout.FlexibleSpace();
			GUILayout.Label($"Found: {foundCount}", EditorStyles.whiteLabel);
			GUILayout.EndHorizontal();

			EditorGUI.BeginChangeCheck();
			Vector2Int count = EditorGUILayout.Vector2IntField("Count", Count);
			if (EditorGUI.EndChangeCheck()) Count = count;

			float totalWidth = position.width;
			float totalHeight = position.height;
			int cols = Count.x;
			int rows = Count.y;
			float padding = cellPadding;
			
			float cellWidth = (totalWidth - (cols + 1) * padding) / cols;
			float cellHeight = (totalHeight - 150f - (rows + 1) * padding) / rows;

			scrollPos2 = EditorGUILayout.BeginScrollView(scrollPos2);

			int index = 0;
			int total = assets.Count;
			int neededRows = Mathf.CeilToInt((float)total / cols);

			for (int r = 0; r < neededRows; r++)
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(padding);

				for (int c = 0; c < cols; c++)
				{
					if (index >= total)
					{
						GUILayout.Box(GUIContent.none, GUILayout.Width(cellWidth), GUILayout.Height(cellHeight));
					}
					else
					{
						var asset = assets[index];

						EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(cellWidth), GUILayout.Height(cellHeight));

						EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
						Texture2D icon = AssetPreview.GetMiniThumbnail(asset);
						if (icon != null)
							GUILayout.Label(new GUIContent(icon), GUILayout.Width(20), GUILayout.Height(20));
						GUILayout.Label(asset.name, EditorStyles.boldLabel);
						GUILayout.FlexibleSpace();
						EditorGUILayout.EndHorizontal();

						EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
						if (GUILayout.Button("Ping", GUILayout.Width(45)))
							EditorGUIUtility.PingObject(asset);
						if (GUILayout.Button("Select", GUILayout.Width(50)))
							Selection.activeObject = asset;
						GUILayout.FlexibleSpace();
						EditorGUILayout.EndHorizontal();

						if (!editors.TryGetValue(asset, out Editor ed) || ed == null)
						{
							ed = Editor.CreateEditor(asset);
							editors[asset] = ed;
						}

						if (!editorScrollPositions.ContainsKey(asset))
							editorScrollPositions[asset] = Vector2.zero;

						float oldLabelWidth = EditorGUIUtility.labelWidth;
						EditorGUIUtility.labelWidth = 80;

						editorScrollPositions[asset] = EditorGUILayout.BeginScrollView(
							editorScrollPositions[asset], 
							GUILayout.Height(cellHeight - 50)
						);
						
						if (ed != null)
							ed.OnInspectorGUI();
						
						EditorGUILayout.EndScrollView();

						EditorGUIUtility.labelWidth = oldLabelWidth;

						EditorGUILayout.EndVertical();
					}

					GUILayout.Space(padding);
					index++;
				}

				EditorGUILayout.EndHorizontal();
				GUILayout.Space(padding);
			}

			EditorGUILayout.EndScrollView();
		}
	}
}