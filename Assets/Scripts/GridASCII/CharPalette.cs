using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD.GridASCII
{
	[Serializable]
	public class CharPalette
	{
		[Serializable]
		public class Entry
		{
			public char character;
			public Color color = Color.white;
		}

		public List<Entry> entries = new List<Entry>
		{
			new Entry { character = '█', color = new Color(0.20f, 0.20f, 0.20f) },
			new Entry { character = '░', color = new Color(0.80f, 0.80f, 0.80f) },
			new Entry { character = '◎', color = new Color(1.00f, 0.85f, 0.20f) },
			new Entry { character = 'S', color = new Color(0.95f, 0.35f, 0.35f) },
			new Entry { character = '.', color = new Color(0.12f, 0.12f, 0.12f) }
		};

		public Dictionary<char, Color> ToDictionary()
		{
			var dict = new Dictionary<char, Color>();
			foreach (var entry in entries)
			{
				dict[entry.character] = entry.color;
			}

			return dict;
		}

		public string Serialize()
		{
			return JsonUtility.ToJson(this);
		}

		public static Dictionary<char, Color> Deserialize(string json)
		{
			if (string.IsNullOrEmpty(json))
				return new CharPalette().ToDictionary();

			try
			{
				var palette = JsonUtility.FromJson<CharPalette>(json);
				return palette.ToDictionary();
			}
			catch
			{
				return new CharPalette().ToDictionary();
			}
		}
	}
}
