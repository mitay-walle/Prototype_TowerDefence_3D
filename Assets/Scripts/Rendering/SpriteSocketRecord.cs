using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD.Rendering
{
	[Serializable]
	public sealed class SpriteSocketRecord
	{
		public Sprite sprite;
		public Sprite mainSprite;
		public bool inheritMain = true;
		public List<SpriteSocketTransform> sockets = new List<SpriteSocketTransform>();
	}
}
