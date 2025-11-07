using System;
using UnityEngine;

namespace GameJam.Plugins.Audio.Winds
{
	[Serializable]
	public class RangedAudioSource
	{
		[SerializeField] private AudioSource _source;
		[SerializeField] private AnimationCurve _volume = AnimationCurve.Linear(0, 0, 1, 1);
		[SerializeField] private AnimationCurve _pitch = AnimationCurve.Linear(0, .95f, 1, 1.1f);

		public void OnUpdate(float normalizedForce)
		{
			_source.volume = _volume.Evaluate(normalizedForce);
			_source.pitch = _pitch.Evaluate(normalizedForce);
		}
	}
}