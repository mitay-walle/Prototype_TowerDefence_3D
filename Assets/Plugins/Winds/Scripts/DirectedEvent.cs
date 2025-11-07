using UnityEngine;
using UnityEngine.Events;

namespace GameJam.Plugins.Audio.Winds
{
	public class DirectedEvent : MonoBehaviour
	{
		public UnityEvent<float> OnSetDirection;
		public UnityEvent<Vector3> OnSetDirectionVector3;

		private Wind _wind;

		private void SetDirection(float angleDegrees) => OnSetDirection?.Invoke(angleDegrees);
		private void SetDirectionVector3(Vector3 direction) => OnSetDirectionVector3?.Invoke(direction);

		private void OnEnable()
		{
			_wind = FindAnyObjectByType<Wind>();
			if (!_wind) return;

			_wind.DirectionChangedDegree -= SetDirection;
			_wind.DirectionChangedDegree += SetDirection;

			_wind.DirectionChangedVector3 -= SetDirectionVector3;
			_wind.DirectionChangedVector3 += SetDirectionVector3;
		}

		private void OnDisable()
		{
			if (_wind) _wind.DirectionChangedDegree -= SetDirection;
		}
	}
}