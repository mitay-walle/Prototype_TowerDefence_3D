using System;
using System.Reflection;
using Sirenix.OdinInspector;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Plugins.Runtime.UI
{
	[ExecuteAlways]
	public class SelectableEffects : MonoBehaviour
	{
		[SerializeReference] private SelectableEffect[] effects = { new SelectableEffectColor() };

		private Selectable _selectable;

		private void OnEnable()
		{
			_selectable = GetComponentInParent<Selectable>();

			if (!_selectable)
			{
				enabled = false;
				return;
			}

			foreach (SelectableEffect effect in effects)
			{
				effect.OnEnable(_selectable);
			}
		}

		private void Update()
		{
			if (!_selectable) return;

			foreach (SelectableEffect effect in effects)
			{
				if (effect.enabled) effect.Update(_selectable);
			}
		}
	}

	[Serializable]
	public abstract class SelectableEffect
	{
		protected static PropertyInfo hasSelection = typeof(Selectable).GetProperty("hasSelection", BindingFlags.Instance | BindingFlags.NonPublic);
		protected static PropertyInfo currentSelectionState = typeof(Selectable).GetProperty("currentSelectionState", BindingFlags.Instance | BindingFlags.NonPublic);

		protected const string DEFAULT = "@mode==eMode.Default";
		protected const string SELECTION_ONLY = "@mode==eMode.Selection || mode==eMode.Default";
		protected const string TOGGLE_IS_ON_ONLY = "@mode==eMode.ToggleIsOn || mode==eMode.Default";

		public bool enabled = true;
		[SerializeField] protected eMode mode;

		public abstract void OnEnable(Selectable selectable);
		public abstract void Update(Selectable selectable);

		protected enum eMode
		{
			Default,
			Selection,
			ToggleIsOn,
		}

		/// <summary>
		/// An enumeration of selected states of objects
		/// </summary>
		public enum SelectionState
		{
			/// <summary>
			/// The UI object can be selected.
			/// </summary>
			Normal,

			/// <summary>
			/// The UI object is highlighted.
			/// </summary>
			Highlighted,

			/// <summary>
			/// The UI object is pressed.
			/// </summary>
			Pressed,

			/// <summary>
			/// The UI object is selected
			/// </summary>
			Selected,

			/// <summary>
			/// The UI object cannot be selected.
			/// </summary>
			Disabled,
		}
	}

	public enum eColorApply
	{
		CanvasRenderer,
		GraphicColor,
	}

	[Serializable]
	public class SelectableEffectColor : SelectableEffect
	{
		private const string Default = "@mode==eMode.Default";
		private const string SelectionOnly = "@mode==eMode.Selection || mode==eMode.Default";
		private const string ToggleIsOnOnly = "@mode==eMode.ToggleIsOn || mode==eMode.Default";
		[SerializeField] private eColorApply _applyMode;
		[SerializeField] private Graphic graphic;

		[SerializeField] private Color normalColor = Color.magenta;
		[SerializeField, ShowIf(ToggleIsOnOnly)] private Color pressedColor = Color.magenta;
		[SerializeField, ShowIf(Default)] private Color highlightedColor = Color.magenta;
		[SerializeField, ShowIf(SelectionOnly)] private Color selectedColor = Color.magenta;
		[SerializeField, ShowIf(ToggleIsOnOnly)] private Color disabledColor = Color.grey;
		[SerializeField] private UnityEvent<Color> _unityEvent;

		public ColorBlock Colors
		{
			get => new()
			{
				colorMultiplier = 1,
				fadeDuration = .1f,
				normalColor = normalColor,
				highlightedColor = highlightedColor,
				pressedColor = pressedColor,
				selectedColor = selectedColor,
				disabledColor = disabledColor,
			};
			set
			{
				normalColor = value.normalColor;
				highlightedColor = value.highlightedColor;
				pressedColor = value.pressedColor;
				selectedColor = value.selectedColor;
				disabledColor = value.disabledColor;
			}
		}

		public override void OnEnable(Selectable selectable)
		{
			if (!graphic)
			{
				graphic = selectable.GetComponentInChildren<TMP_Text>();

				if (!graphic)
				{
					graphic = selectable.GetComponentInChildren<Text>();
				}
			}

			if (!graphic)
			{
				//enabled = false;
				return;
			}
		}

		public override void Update(Selectable selectable)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying && PrefabUtility.IsPartOfPrefabAsset(selectable))
			{
				return;
			}
#endif
			UpdateColorTint(selectable);
		}

		private void UpdateColorTint(Selectable selectable)
		{
			Color color;
			switch (mode)
			{
				case eMode.Selection:
					{
						color = (bool)hasSelection.GetValue(selectable) ? selectedColor : normalColor;
						break;
					}

				case eMode.ToggleIsOn:
					{
						if (!selectable.interactable)
						{
							color = disabledColor;
						}
						else if (selectable is Toggle { isOn: true })
						{
							color = pressedColor;
						}
						else
						{
							color = normalColor;
						}

						break;
					}

				case eMode.Default:
					{
						color = (SelectionState)currentSelectionState.GetValue(selectable) switch
						{
							SelectionState.Normal => normalColor,
							SelectionState.Highlighted => highlightedColor,
							SelectionState.Pressed => pressedColor,
							SelectionState.Selected => selectedColor,
							SelectionState.Disabled => disabledColor,
							_ => normalColor,
						};

						break;
					}

				default:
					{
						color = Color.white;
						break;
					}
			}

			SetColor(color);
		}

		private void SetColor(Color color)
		{
			if (graphic != null)
			{
				switch (_applyMode)
				{
					case eColorApply.CanvasRenderer:
						{
							graphic.canvasRenderer.SetColor(color);
							break;
						}

					case eColorApply.GraphicColor:
						{
							graphic.color = color;
							break;
						}

					default: throw new ArgumentOutOfRangeException();
				}
			}

			_unityEvent?.Invoke(color);
		}

		#if UNITY_EDITOR
		[Button]
		private void CopyBlock()
		{
			EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(Colors);
		}

		[Button]
		private void PasteBlock()
		{
			Colors = JsonUtility.FromJson<ColorBlock>(EditorGUIUtility.systemCopyBuffer);
		}
		#endif
	}

	[Serializable]
	public class SelectableEffectRotation : SelectableEffectBase<Vector3>
	{
		[SerializeField, Required] protected Transform target;
		[SerializeField, Required] protected float maxSpeed = 400;

		protected override void SetValue(Vector3 value)
		{
			if (target) target.localEulerAngles = Vector3.MoveTowards(target.localEulerAngles, value, Time.deltaTime * maxSpeed);
		}
	}

	[Serializable]
	public class SelectableEffectPosition : SelectableEffectBase<Vector3>
	{
		[SerializeField, Required] protected Transform target;
		[SerializeField, Required] protected float maxSpeed = 400;

		protected override void SetValue(Vector3 value)
		{
			if (target) target.localPosition = Vector3.MoveTowards(target.localPosition, value, Time.deltaTime * maxSpeed);
		}
	}

	[Serializable]
	public class SelectableEffectScale : SelectableEffectBase<Vector3>
	{
		[SerializeField, Required] protected Transform target;
		[SerializeField] protected float maxSpeed = 400;

		protected override void SetValue(Vector3 value)
		{
			if (target) target.localScale = Vector3.MoveTowards(target.localScale, value, Time.deltaTime * maxSpeed);
		}
	}

	[Serializable]
	public class SelectableEffectSprite : SelectableEffectBase<Sprite>
	{
		[SerializeField, Required] protected Image target;

		protected override void SetValue(Sprite value)
		{
			if (target) target.sprite = value;
		}
	}

	[Serializable]
	public class SelectableEffectBool : SelectableEffectBase<bool>
	{
		[SerializeField] protected bool _invert;
		[SerializeField] protected UnityEvent<bool> _event;

		protected override void SetValue(bool value)
		{
			_event?.Invoke(value != _invert);
		}
	}

	[Serializable]
	public class SelectableEffectFloat : SelectableEffectBase<float>
	{
		[SerializeField, Required] protected UnityEvent<float> _event;
		[SerializeField] protected float maxSpeed = 400;
		private float current;

		protected override void SetValue(float value)
		{
			current = Mathf.MoveTowards(current, value, Time.deltaTime * maxSpeed);
			_event?.Invoke(current);
		}
	}

	[Serializable]
	public class SelectableEffectVector2 : SelectableEffectBase<Vector2>
	{
		[SerializeField, Required] protected UnityEvent<Vector2> _event;
		[SerializeField] protected float maxSpeed = 400;
		private Vector2 current;

		protected override void SetValue(Vector2 value)
		{
			current = Vector2.MoveTowards(current, value, Time.deltaTime * maxSpeed);
			_event?.Invoke(current);
		}
	}

	[Serializable]
	public abstract class SelectableEffectBase<TValue> : SelectableEffect
	{
		[SerializeField] private TValue normal;
		[SerializeField, ShowIf(TOGGLE_IS_ON_ONLY)] private TValue pressed;
		[SerializeField, ShowIf(DEFAULT)] private TValue highlighted;
		[SerializeField, ShowIf(SELECTION_ONLY)] private TValue selected;
		[SerializeField, ShowIf(TOGGLE_IS_ON_ONLY)] private TValue disabled;

		public override void OnEnable(Selectable selectable) { }

		public override void Update(Selectable selectable)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying && PrefabUtility.IsPartOfPrefabAsset(selectable))
			{
				return;
			}
#endif
			UpdateValue(selectable);
		}

		private void UpdateValue(Selectable selectable)
		{
			TValue color;
			switch (mode)
			{
				case eMode.Selection:
					{
						color = (bool)hasSelection.GetValue(selectable) ? selected : normal;
						break;
					}

				case eMode.ToggleIsOn:
					{
						if (!selectable.interactable)
						{
							color = disabled;
						}
						else if (selectable is Toggle { isOn: true })
						{
							color = pressed;
						}
						else
						{
							color = normal;
						}

						break;
					}

				case eMode.Default:
					{
						color = (SelectionState)currentSelectionState.GetValue(selectable) switch
						{
							SelectionState.Normal => normal,
							SelectionState.Highlighted => highlighted,
							SelectionState.Pressed => pressed,
							SelectionState.Selected => selected,
							SelectionState.Disabled => disabled,
							_ => normal,
						};

						break;
					}

				default:
					{
						color = normal;
						break;
					}
			}

			SetValue(color);
		}

		protected abstract void SetValue(TValue value);
	}
}