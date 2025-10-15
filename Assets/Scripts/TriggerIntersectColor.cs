using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TD
{
	public class TriggerIntersectColor : MonoBehaviour
	{
		static readonly int TINT_COLOR = Shader.PropertyToID("_MatCapTint");
		private List<Renderer> renderers = new List<Renderer>();
		[ShowInInspector] public bool IsIntersected { get; private set; }

		void OnTriggerStay(Collider other)
		{
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
	}
}