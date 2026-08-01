using System;
using UnityEngine;

namespace TD.Rendering
{
	[Serializable]
	public sealed class SpriteSocketTransform
	{
		public string name;
		public Vector3 localPosition;
		public Vector3 localEulerAngles;
		public Vector3 localScale = Vector3.one;
		public bool rotateWithSpriteParent;

		public void ApplyTo(Transform socket, Transform spriteParent)
		{
			socket.localPosition = localPosition;
			socket.localScale = localScale;
			if (rotateWithSpriteParent && spriteParent != null)
			{
				socket.rotation = spriteParent.rotation * Quaternion.Euler(localEulerAngles);
				return;
			}

			socket.localRotation = Quaternion.Euler(localEulerAngles);
		}
	}
}
