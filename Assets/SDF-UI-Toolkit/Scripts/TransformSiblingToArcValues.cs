using System;
using Sirenix.OdinInspector;
using TLab.UI.SDF;
using UnityEditor;
using UnityEngine;

namespace TD.SDF_UI_Toolkit.Scripts
{
	[ExecuteAlways, RequireComponent(typeof(SDFArc))]
	public class TransformSiblingToArcValues : MonoBehaviour
	{
		[SerializeField, OnValueChanged("OnTransformParentChanged"), PropertyRange(0, 360)] private float startAngle;
		[SerializeField, OnValueChanged("OnTransformParentChanged"), MaxValue("MaxOffset"), MinValue(0)] private int parentOffset;
		[SerializeField, OnValueChanged("OnTransformParentChanged"), PropertyRange(-180, 180)] private float offsetAngle;
		private int _childCount;

		void OnEnable() => OnTransformParentChanged();

		int MaxOffset() => transform.GetComponentsInParent<Transform>().Length - 2;

		void OnTransformParentChanged()
		{
			SDFArc arc = GetComponent<SDFArc>();
			Transform target = transform;
			if (parentOffset > 0)
			{
				for (int i = 0; i < parentOffset; i++)
				{
					if (target == null) return;

					target = target.parent;
				}
			}

			if (!target) return;
			if (!target.parent) return;

			int siblingIndex = target.GetSiblingIndex();

			float siblingFactor = (float)siblingIndex / target.parent.childCount;
			float singleFactor = (float)1 / target.parent.childCount + offsetAngle / 360;
			arc.startAngle = 360 * siblingFactor + offsetAngle + startAngle;
			arc.startAngle *= -1;
			arc.fillAmount = singleFactor;
			#if UNITY_EDITOR
			EditorUtility.SetDirty(arc);
			#endif
		}

		void Update()
		{
			Transform target = transform;
			if (parentOffset > 0)
			{
				for (int i = 0; i < parentOffset; i++)
				{
					if (target == null) return;

					target = target.parent;
				}
			}

			if (!target) return;
			if (!target.parent) return;

			if (transform.parent != null && transform.parent.childCount != _childCount)
			{
				_childCount = transform.parent.childCount;
				OnTransformParentChanged();
			}
		}
	}
}