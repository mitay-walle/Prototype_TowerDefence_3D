using Unity.Mathematics;
using UnityEngine;

namespace Plugins.TransformOperations
{
	public enum eUpdatePattern
	{
		None,
		Update,
		LateUpdate,
		FixedUpdate,
	}

	[ExecuteAlways]
	public class StickToTransform : MonoBehaviour
	{
		public Transform Target;

		[SerializeField] protected eUpdatePattern Pattern = eUpdatePattern.Update;
		[SerializeField] protected bool3 Position = new(true, true, false);
		[SerializeField] protected bool3 Rotation = new(false, false, true);
		[SerializeField] protected bool3 Scale = false;
		[SerializeField] protected bool ComparePositionBeforeChange = true;
		public bool StickToWorldOffset;
		public Vector3 WorldPositionOffset;
		[SerializeField] protected Vector3 TargetPositionOffset;

		protected Transform cachTr;

		protected void Start() => Init();
		public void Init() => cachTr = transform;

		public void FixedUpdate()
		{
			if (Pattern != eUpdatePattern.FixedUpdate) return;

			UpdateAnchors();
		}

		public void Update()
		{
			if (Pattern != eUpdatePattern.Update) return;

			UpdateAnchors();
		}

		public void LateUpdate()
		{
			if (Pattern != eUpdatePattern.LateUpdate) return;

			UpdateAnchors();
		}

		public void UpdateAnchors()
		{
			bool hasTarget = Target;

			var lastPos = cachTr.position;

			if (Position.x || Position.y || Position.z)
			{
				if (ComparePositionBeforeChange)
				{
					if (hasTarget && lastPos != Target.position || StickToWorldOffset && lastPos == WorldPositionOffset)
					{
						ForceStickPosition();
					}
				}
				else
				{
					ForceStickPosition();
				}
			}
			ForceStickRotation();

			ForceStickScale();
		}

		public void ForceStickRotation()
		{
			bool hasTarget = Target;
			Vector3 euler = hasTarget ? Target.eulerAngles : default;
			Vector3 eulerBefore = cachTr.eulerAngles;

			if (!Rotation.x) euler.x = eulerBefore.x;
			if (!Rotation.y) euler.y = eulerBefore.y;
			if (!Rotation.z) euler.z = eulerBefore.z;

			cachTr.eulerAngles = euler;
		}
		public void ForceStickScale()
		{
			bool hasTarget = Target;
			Vector3 scale = hasTarget ? Target.localScale : Vector3.one;
			Vector3 scaleBefore = cachTr.localScale;

			if (!Scale.x) scale.x = scaleBefore.x;
			if (!Scale.y) scale.y = scaleBefore.y;
			if (!Scale.z) scale.z = scaleBefore.z;

			cachTr.localScale = scale;
		}
		public void ForceStickPosition()
		{
			if (!StickToWorldOffset && !Target) return;

			Vector3 pos = cachTr.position;
			Vector3 newPos = StickToWorldOffset ? Vector3.zero : Target.position;

			Vector3 localOffset = default;
			if (TargetPositionOffset != default)
			{
				localOffset = Target.TransformVector(TargetPositionOffset);
			}

			if (Position.x) pos.x = newPos.x + WorldPositionOffset.x + localOffset.x;
			if (Position.y) pos.y = newPos.y + WorldPositionOffset.y + localOffset.y;
			if (Position.z) pos.z = newPos.z + WorldPositionOffset.z + localOffset.z;

			cachTr.position = pos;
		}
	}
}