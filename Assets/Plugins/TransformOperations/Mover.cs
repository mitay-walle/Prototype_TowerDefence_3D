using UnityEngine;

namespace Plugins.TransformOperations
{
	public class Mover : MonoBehaviour
	{
		[SerializeField] private Space _space = Space.Self;
		[SerializeField] private Vector2 _speed = new(0, 50);
		[SerializeField] private Space _rotateSpace = Space.Self;
		[SerializeField] private Vector3 _rotate;
		
		private void FixedUpdate()
		{
			transform.Translate(_speed, _space);
			transform.Rotate(_rotate);
		}
		public Vector2 Speed => _space == Space.World ? _speed : transform.TransformVector(_speed);
	}
}