using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD.Rendering
{
	[Serializable]
	public sealed class SpriteSocketRecord
	{
		public Sprite sprite;
		public List<SpriteSocketTransform> sockets = new List<SpriteSocketTransform>();

		public bool TryGetSocket(string socketName, out SpriteSocketTransform socket)
		{
			socket = null;
			if (sockets == null)
			{
				return false;
			}

			string normalizedName = SpriteResolverSockets.NormalizeSocketName(socketName);
			if (string.IsNullOrEmpty(normalizedName))
			{
				return false;
			}

			foreach (SpriteSocketTransform candidate in sockets)
			{
				if (candidate != null &&
					string.Equals(
						SpriteResolverSockets.NormalizeSocketName(candidate.name),
						normalizedName,
						StringComparison.OrdinalIgnoreCase))
				{
					socket = candidate;
					return true;
				}
			}

			return false;
		}
	}
}