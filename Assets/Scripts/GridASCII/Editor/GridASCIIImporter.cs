using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace TD.GridASCII.Editor
{
	[ScriptedImporter(3, "enumgrid", AllowCaching = false)]
	public sealed class GridASCIIImporter : ScriptedImporter
	{
		private static readonly char[] SplitDelims = { ' ', '\t', ',', ';', '|' };

		public override void OnImportAsset(AssetImportContext ctx)
		{
			string raw = File.ReadAllText(ctx.assetPath, Encoding.UTF8);

			List<string> lines = raw.Replace("\r", "").Split('\n').Select(l => l.TrimEnd())
				.Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#")).ToList();

			if (lines.Count == 0)
				throw new Exception("EnumGridImporter: файл пуст или содержит только комментарии.");

			bool tokenMode = lines.Any(l => l.IndexOfAny(SplitDelims) >= 0);

			List<string[]> rows = new();
			int width = -1;

			if (tokenMode)
			{
				foreach (string ln in lines)
				{
					string[] parts = ln.Split(SplitDelims, StringSplitOptions.RemoveEmptyEntries);
					if (width < 0) width = parts.Length;
					else if (parts.Length != width)
						throw new Exception(
							$"EnumGridImporter: ширина строк не совпадает. Ожидалось {width}, найдено {parts.Length}. Строка: \"{ln}\"");

					rows.Add(parts);
				}
			}
			else
			{
				foreach (string ln in lines)
				{
					string[] parts = ln.ToCharArray().Select(c => c.ToString()).ToArray();
					if (width < 0) width = parts.Length;
					else if (parts.Length != width)
						throw new Exception(
							$"EnumGridImporter: ширина строк не совпадает. Ожидалось {width}, найдено {parts.Length}. Строка: \"{ln}\"");

					rows.Add(parts);
				}
			}

			int height = rows.Count;
			var grid = new char[height, width];

			for (int y = 0; y < height; y++)
			{
				string[] row = rows[y];
				for (int x = 0; x < width; x++)
				{
					string token = row[x];
					grid[y, x] = token.Length > 0 ? token[0] : '.';
				}
			}

			var asset = ScriptableObject.CreateInstance<GridASCIIAsset>();
			asset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
			asset.SetData(width, height, grid);

			if (string.IsNullOrEmpty(userData))
			{
				userData = new CharPalette().Serialize();
			}

			ctx.AddObjectToAsset("Main", asset);
			ctx.SetMainObject(asset);
		}
	}
}