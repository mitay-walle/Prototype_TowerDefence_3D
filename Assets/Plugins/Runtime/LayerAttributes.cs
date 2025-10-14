// LayerAndRenderingLayerDrawer.cs
// MIT License — (c) mitay-walle 2025

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TD.Plugins.Runtime
{
#region Attributes
	/// <summary>
	/// Показывает выпадающий список физических слоёв (Physics / LayerMask).
	/// Применяется к int полям.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class LayerAttribute : PropertyAttribute { }

	/// <summary>
	/// Показывает выпадающий список Rendering Layer Mask (URP/HDRP).
	/// Применяется к int или uint полям.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class RenderingLayerAttribute : PropertyAttribute { }
#endregion

#region Drawers
#if UNITY_EDITOR

	[CustomPropertyDrawer(typeof(LayerAttribute))]
	public class LayerAttributeDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			if (property.propertyType == SerializedPropertyType.Integer)
			{
				int currentLayer = property.intValue;
				string currentName = LayerMask.LayerToName(currentLayer);
				if (string.IsNullOrEmpty(currentName))
					currentName = $"Layer {currentLayer}";

				EditorGUI.BeginChangeCheck();
				int newLayer = EditorGUI.LayerField(position, label, currentLayer);
				if (EditorGUI.EndChangeCheck())
					property.intValue = newLayer;
			}
			else
			{
				EditorGUI.LabelField(position, label.text, "Use with int field");
			}

			EditorGUI.EndProperty();
		}
	}

	[CustomPropertyDrawer(typeof(RenderingLayerAttribute))]
	public class RenderingLayerAttributeDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			ulong maskValue = 0;
			bool isInt = false;

			if (property.propertyType == SerializedPropertyType.Integer)
			{
				maskValue = (uint)property.intValue;
				isInt = true;
			}
			else
			{
				EditorGUI.LabelField(position, label.text, "Use with int or uint");
				EditorGUI.EndProperty();
				return;
			}

			// Получаем список имён слоёв рендеринга (URP/HDRP)
			string[] renderingLayerNames = GraphicsSettings.currentRenderPipeline != null
				? GraphicsSettings.currentRenderPipeline.renderingLayerMaskNames : RenderingLayerMask.GetDefinedRenderingLayerNames();

			// EditorGUILayout.LayerMaskField принимает int, поэтому делаем int cast
			EditorGUI.BeginChangeCheck();
			int newMask = EditorGUI.MaskField(position, label, (int)maskValue, renderingLayerNames);
			if (EditorGUI.EndChangeCheck())
			{
				if (isInt)
					property.intValue = newMask;
				else
					property.ulongValue = (ulong)newMask;
			}

			EditorGUI.EndProperty();
		}
	}

#endif
#endregion
}