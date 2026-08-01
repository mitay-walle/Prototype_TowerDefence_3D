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
	}
}