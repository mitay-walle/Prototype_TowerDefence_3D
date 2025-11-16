using TD.GameLoop;
using UnityEngine;

namespace TD.UI
{
	public enum TextStyle
	{
		Gold,
		GoldIconOnly,
		Monster,
		Tower,
	}

	public static class StringUtility
	{
		public static string ToStyle(this string text, TextStyle styleName) => $"<style=\"{styleName}\">{text}</style>";

		public static string ToStringGoldCanAfford(this int Cost)
		{
			if (ResourceManager.Instance.CanAfford(Cost))
			{
				return Cost.ToString().ToStyle(TextStyle.Gold);
			}
			else
			{
				return Cost.ToString().ToStyle(TextStyle.GoldIconOnly).ToColor(Color.red);
			}
		}

		public static string ToStyle(this string text, string styleName) => $"<style=\"{styleName}\">{text}</style>";
		public static string ToColor(this string text, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
	}
}