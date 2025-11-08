using System.Collections.Generic;
using System.IO;
using System.Text;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.AssetImporters;
#endif

namespace TD.GridASCII
{
	[CreateAssetMenu(fileName = "EnumGridAsset RealName", menuName = "Enum Grid Asset")]
	public sealed class GridASCIIAsset : ScriptableObject
	{
		[SerializeField, HideInInspector] private int width;
		[SerializeField, HideInInspector] private int height;
		[SerializeField, HideInInspector] private char[] data;

#if UNITY_EDITOR
		[SerializeField, HideInInspector] private string sourcePath;
#endif

		public int Width => width;
		public int Height => height;

		[ShowInInspector, LabelText("Grid"),
		 TableMatrix(DrawElementMethod = "DrawCellGUI", ResizableColumns = false, IsReadOnly = false, SquareCells = true)]
		public char[,] Grid
		{
			get
			{
				var m = new char[width, height];
				if (data == null || data.Length != width * height) return m;

				for (int y = 0; y < height; y++)
					for (int x = 0; x < width; x++)
						m[x, y] = data[y * width + x];

				return m;
			}
			set
			{
				if (value == null) return;

				int w = value.GetLength(0);
				int h = value.GetLength(1);
				if (w != width || h != height)
				{
					Debug.LogWarning($"Size mismatch: incoming [{w},{h}] vs [{width},{height}]. Ignored.");
					return;
				}

				if (data == null || data.Length != w * h)
					data = new char[w * h];

				for (int y = 0; y < h; y++)
					for (int x = 0; x < w; x++)
						data[y * width + x] = value[x, y];

#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
#endif
			}
		}

		public void SetData(int w, int h, char[,] grid)
		{
			width = w;
			height = h;
			data = new char[w * h];
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
					data[y * w + x] = grid[y, x];

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		public char GetCell(int x, int y)
		{
			if (x < 0 || x >= width || y < 0 || y >= height) return '\0';

			return data[y * width + x];
		}

#if UNITY_EDITOR
		private static bool s_Painting;
		private static char s_AnchorChar;
		private static HashSet<long> s_PaintedThisDrag = new HashSet<long>();
		private static List<char> s_CharCycle;
		private static int s_CharIndex;

		private static char DrawCellGUI(Rect rect, char value, int x, int y)
		{
			var palette = GetPalette();
			Color c = palette.TryGetValue(value, out var col) ? col : new Color(0.12f, 0.12f, 0.12f);

			EditorGUI.DrawRect(rect, c);

			var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { normal = { textColor = Color.black } };
			GUI.Label(rect, value.ToString(), style);

			Event e = Event.current;
			Vector2 mp = e.mousePosition;
			bool inside = rect.Contains(mp);

			if ((e.rawType == EventType.MouseDown || e.type == EventType.MouseDown) && inside && e.button < 2)
			{
				s_Painting = true;
				s_AnchorChar = value;

				if (s_CharCycle == null || s_CharCycle.Count == 0)
					s_CharCycle = new List<char>(palette.Keys);

				s_CharIndex = s_CharCycle.IndexOf(value);
				if (s_CharIndex < 0) s_CharIndex = 0;

				if (e.button == 0)
				{
					s_CharIndex = (int)Mathf.Repeat(s_CharIndex + 1, s_CharCycle.Count);
				}
				else
				{
					s_CharIndex = (int)Mathf.Repeat(s_CharIndex - 1, s_CharCycle.Count);
				}

				value = s_CharCycle[s_CharIndex];

				s_PaintedThisDrag.Add(((long)y << 32) | (uint)x);

				GUI.changed = true;
				e.Use();
				return value;
			}

			if (s_Painting && inside && value == s_AnchorChar && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove ||
			                                                      e.rawType == EventType.MouseDrag || e.rawType == EventType.MouseMove))
			{
				long key = ((long)y << 32) | (uint)x;
				if (!s_PaintedThisDrag.Contains(key))
				{
					value = s_CharCycle[s_CharIndex];
					s_PaintedThisDrag.Add(key);
					GUI.changed = true;
				}
			}

			if ((e.rawType == EventType.MouseUp || e.type == EventType.MouseUp) && e.button < 2)
			{
				s_Painting = false;
				s_PaintedThisDrag.Clear();
			}

			return value;
		}

		private static Dictionary<char, Color> GetPalette()
		{
			var asset = Selection.activeObject as GridASCIIAsset;
			if (asset == null) return new Dictionary<char, Color>();

			string path = AssetDatabase.GetAssetPath(asset);
			if (string.IsNullOrEmpty(path)) return new Dictionary<char, Color>();

			var importer = AssetImporter.GetAtPath(path) as ScriptedImporter;
			if (importer == null) return new Dictionary<char, Color>();

			return CharPalette.Deserialize(importer.userData);
		}

		[Button(ButtonSizes.Medium), PropertyOrder(-1)]
		private void SaveToSource()
		{
			sourcePath = AssetDatabase.GetAssetPath(this);
			if (string.IsNullOrEmpty(sourcePath))
			{
				Debug.LogError("EnumGridAsset: sourcePath is empty. Reimport the asset so importer stores the path.");
				return;
			}

			if (!File.Exists(sourcePath))
			{
				Debug.LogError("EnumGridAsset: source file not found: " + sourcePath);
				return;
			}

			var lines = new string[height];
			for (int y = 0; y < height; y++)
			{
				var sb = new StringBuilder(width);
				for (int x = 0; x < width; x++)
				{
					sb.Append(data[y * width + x]);
				}

				lines[y] = sb.ToString();
			}

			var txt = string.Join("\n", lines) + "\n";
			File.WriteAllText(sourcePath, txt, Encoding.UTF8);
			AssetDatabase.ImportAsset(sourcePath);
			EditorGUIUtility.PingObject(this);
			Debug.Log($"EnumGridAsset: saved & reimported: {sourcePath}");
		}
#endif
	}
}