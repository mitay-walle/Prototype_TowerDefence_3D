using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace TD.UI.Information
{
	public class AutoPositionalTooltip : MonoBehaviour
	{
		[ShowInInspector] bool Logs;
		[SerializeField] private GameObject _visual;
		[SerializeField] private ButtonData[] _buttons;
		[SerializeField] private LocalizeStringEvent titleEvent;
		[SerializeField] private LocalizeStringEvent messageEvent;
		[ShowInInspector, HideInEditorMode, ReadOnly] public RectTransform lastTarget { get; private set; }
		private RectTransform selfRect;
		private Canvas rootCanvas;

		[Serializable]
		public class ButtonData
		{
			public Button Button;
			public Action onClick;
		}

		private enum Side
		{
			Left,
			Right,
			Top,
			Bottom
		}

		private void Awake()
		{
			selfRect = _visual.transform as RectTransform;
			rootCanvas = GetComponentInParent<Canvas>();
			Hide();
			foreach (var button in _buttons)
			{
				button.Button.onClick.AddListener(() => { button.onClick?.Invoke(); });
			}
		}

		private void Update()
		{
			PositionTooltip(lastTarget);
			if (lastTarget != null && !lastTarget.gameObject.activeInHierarchy)
			{
				Hide();
			}
		}

		public void Show(RectTransform target, LocalizedString title, LocalizedString message, IEnumerable<(Action, LocalizedString)> actions = null)
		{
			_buttons[0].Button.transform.parent.gameObject.SetActive(actions != null);

			foreach (ButtonData button in _buttons)
			{
				button.Button.gameObject.SetActive(false);
			}

			if (actions != null)
			{
				int i = 0;
				foreach (var tuple in actions)
				{
					_buttons[i].Button.gameObject.SetActive(tuple.Item1 != null);
					_buttons[i].onClick = tuple.Item1;
					_buttons[i].Button.GetComponentInChildren<LocalizeStringEvent>().StringReference = tuple.Item2;
					i++;
				}
			}

			if (Logs) Debug.Log("Show");

			lastTarget = target;
			_visual.SetActive(true);

			// назначаем строки напрямую — LocalizedStringEvent сам обновит TMP_Text
			titleEvent.StringReference = title;
			messageEvent.StringReference = message;
			titleEvent.RefreshString();
			messageEvent.RefreshString();
			LayoutRebuilder.ForceRebuildLayoutImmediate(_visual.GetComponent<RectTransform>());
			LayoutRebuilder.MarkLayoutForRebuild(_visual.GetComponent<RectTransform>());

			PositionTooltip(target);
		}

		public void Hide()
		{
			// сбрасываем, чтобы не оставались ссылки
			titleEvent.StringReference = null;
			messageEvent.StringReference = null;
			_visual.SetActive(false);
			lastTarget = null;
			if (Logs) Debug.Log("Hide");
		}

		private void PositionTooltip(RectTransform target)
		{
			if (target == null) return;

			RectTransform canvasRect = rootCanvas.transform as RectTransform;

			// мировые углы target
			Vector3[] corners = new Vector3[4];
			target.GetWorldCorners(corners);

			// границы target в screen space
			Vector2 min = RectTransformUtility.WorldToScreenPoint(rootCanvas.worldCamera, corners[0]);
			Vector2 max = RectTransformUtility.WorldToScreenPoint(rootCanvas.worldCamera, corners[2]);

			float leftSpace = min.x;
			float rightSpace = Screen.width - max.x;
			float topSpace = Screen.height - max.y;
			float bottomSpace = min.y;

			// ищем наибольшее свободное пространство
			Side bestSide = Side.Right;
			float bestSpace = rightSpace;

			if (leftSpace > bestSpace)
			{
				bestSide = Side.Left;
				bestSpace = leftSpace;
			}

			if (topSpace > bestSpace)
			{
				bestSide = Side.Top;
				bestSpace = topSpace;
			}

			if (bottomSpace > bestSpace)
			{
				bestSide = Side.Bottom;
				bestSpace = bottomSpace;
			}

			// позиция и пивот
			Vector2 targetPos = Vector2.zero;
			Vector2 pivot = new Vector2(0.5f, 0.5f);
			Vector2 offset = new Vector2(10, 10);

			switch (bestSide)
			{
				case Side.Right:
					targetPos = new Vector2(max.x, (min.y + max.y) * 0.5f);
					pivot = new Vector2(0f, 0.5f);
					targetPos += new Vector2(offset.x, 0);
					break;

				case Side.Left:
					targetPos = new Vector2(min.x, (min.y + max.y) * 0.5f);
					pivot = new Vector2(1f, 0.5f);
					targetPos -= new Vector2(offset.x, 0);
					break;

				case Side.Top:
					targetPos = new Vector2((min.x + max.x) * 0.5f, max.y);
					pivot = new Vector2(0.5f, 0f);
					targetPos += new Vector2(0, offset.y);
					break;

				case Side.Bottom:
					targetPos = new Vector2((min.x + max.x) * 0.5f, min.y);
					pivot = new Vector2(0.5f, 1f);
					targetPos -= new Vector2(0, offset.y);
					break;
			}

			// в local canvas space
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetPos, rootCanvas.worldCamera, out Vector2 localPoint);

			selfRect.pivot = pivot;
			selfRect.anchoredPosition = localPoint;
		}
	}
}