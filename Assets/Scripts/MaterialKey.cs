using UnityEngine;

namespace TD
{
	public class MaterialKey
	{
		public Color color;
		public Color emissionColor;
		public float emissionIntensity;

		public override bool Equals(object obj)
		{
			if (obj is MaterialKey other)
			{
				return color.Equals(other.color) && emissionColor.Equals(other.emissionColor) &&
				       Mathf.Approximately(emissionIntensity, other.emissionIntensity);
			}

			return false;
		}

		public override int GetHashCode()
		{
			return color.GetHashCode() ^ emissionColor.GetHashCode() ^ emissionIntensity.GetHashCode();
		}
	}
}