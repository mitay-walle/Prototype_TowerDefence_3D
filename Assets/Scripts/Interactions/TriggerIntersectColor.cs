using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TD.Interactions
{
	public class TriggerIntersectColor : MonoBehaviour
	{
		static readonly int TINT_COLOR = Shader.PropertyToID("_MatCapTint");
		public LayerMask layerMask;
		private List<Renderer> renderers = new List<Renderer>();
		[ShowInInspector] public bool IsIntersected { get; private set; }

		void OnTriggerStay(Collider other)
		{
			if (!Contains(layerMask, other.gameObject.layer)) return;

			GetComponentsInChildren(renderers);
			foreach (Renderer rend in renderers)
			{
				foreach (Material material in rend.materials)
				{
					material.SetColor(TINT_COLOR, Color.red * 1.7f);
				}
			}

			IsIntersected = true;
		}

		void OnTriggerExit(Collider other)
		{
			GetComponentsInChildren(renderers);
			foreach (Renderer rend in renderers)
			{
				foreach (Material material in rend.materials)
				{
					material.SetColor(TINT_COLOR, Color.green * 1.7f);
				}
			}

			IsIntersected = false;
		}

		bool Contains(LayerMask mask, int layer) => (mask.value & 1 << layer) != 0;
	}
}