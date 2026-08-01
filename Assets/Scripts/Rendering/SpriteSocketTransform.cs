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

		public void ApplyTo(Transform socket)
		{
			socket.localPosition = localPosition;
			socket.localRotation = Quaternion.Euler(localEulerAngles);
			socket.localScale = localScale;
		}
	}
}