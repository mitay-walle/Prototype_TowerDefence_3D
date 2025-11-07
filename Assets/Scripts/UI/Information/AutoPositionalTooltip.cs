using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace TD.UI.Information
{
	public class AutoPositionalTooltip : MonoBehaviour
	{
		[SerializeField] private GameObject _visual;
		[SerializeField] private LocalizeStringEvent titleEvent;
		[SerializeField] private LocalizeStringEvent messageEvent;

		private RectTransform lastTarget;
		private RectTransform selfRect;
		private Canvas rootCanvas;

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
		}

		private void Update()
		{
			PositionTooltip(lastTarget);
		}

		public void Show(RectTransform target, LocalizedString title, LocalizedString message)
		{
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