using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Localization;
using Plugins.TransformOperations;
using Sirenix.OdinInspector;
using System;

namespace Plugins.GUI.Information
{
	public class BlockWaitClick : MonoBehaviour
	{
		[SerializeField] private GameObject _visual;
		[SerializeField] private AutoPositionalTooltip _tooltip;

		private Canvas _attachedCanvas;
		private EventTrigger _attachedTrigger;
		private RectTransform _currentTarget;
		private GameObject _waitingRequestor;
		private Button _blockerButton;
		bool _clickBlockerHide;

		public event Action<Transform, GameObject> stoped;

		/// <summary>
		/// Запускает ожидание клика используя данные из ScriptableObject.
		/// </summary>
		/// <param name="data">Данные конфигурации</param>
		[Button]
		public void StartWait(BlockWaitClickData data)
		{
			if (data == null)
			{
				Debug.LogWarning("BlockWaitClick: BlockWaitClickData is null");
				return;
			}

			StartWait(data.transformPath, data.tooltipTitle, data.tooltipMessage);
		}

		/// <summary>
		/// Запускает ожидание клика по объекту, найденному по строковому пути.
		/// </summary>
		/// <param name="transformPath">Путь к Transform в формате "Root/Child/Child2"</param>
		/// <param name="tooltipTitle">Заголовок тултипа (опционально)</param>
		/// <param name="tooltipMessage">Сообщение тултипа (опционально)</param>
		public void StartWait(string transformPath,
		                      LocalizedString tooltipTitle = null,
		                      LocalizedString tooltipMessage = null,
		                      bool isClickable = true,
		                      bool clickBlockerHide = true)
		{
			if (string.IsNullOrEmpty(transformPath))
			{
				Debug.LogWarning($"BlockWaitClick: Transform path is null or empty");
				return;
			}

			Transform target = TransformUtils.FindByPath(transformPath);
			if (target == null)
			{
				Debug.LogWarning($"BlockWaitClick: Transform not found at path '{transformPath}'");
				return;
			}

			StartWait(target, null, tooltipTitle, tooltipMessage, isClickable, clickBlockerHide);
		}

		public void StartWait(Transform target,
		                      GameObject waitRequestor,
		                      LocalizedString tooltipTitle = null,
		                      LocalizedString tooltipMessage = null,
		                      bool isClickable = true,
		                      bool clickBlockerHide = true)
		{
			if (_visual == null || target == null)
				return;

			_waitingRequestor = waitRequestor;

			_visual.SetActive(true);
			_currentTarget = target as RectTransform;

			_attachedCanvas = target.gameObject.AddComponent<Canvas>();
			_attachedCanvas.overrideSorting = true;
			_attachedCanvas.sortingOrder = 9999;

			var caster = target.gameObject.AddComponent<GraphicRaycaster>();

			_attachedTrigger = target.gameObject.AddComponent<EventTrigger>();

			caster.enabled = isClickable;

			var entry = new EventTrigger.Entry
			{
				eventID = EventTriggerType.PointerClick
			};

			entry.callback.AddListener(_ => StopWait());
			_attachedTrigger.triggers.Add(entry);

			// показываем тултип если указан
			if (_tooltip != null && _currentTarget != null && tooltipTitle != null && tooltipMessage != null)
			{
				_tooltip.Show(_currentTarget, tooltipTitle, tooltipMessage);
			}

			_clickBlockerHide = clickBlockerHide;
			if (!_blockerButton)
			{
				_blockerButton = _visual.AddComponent<Button>();
				_blockerButton.onClick.AddListener(OnClickBlocker);
			}
		}

		[Button]
		private void StopWait()
		{
			stoped?.Invoke(_currentTarget, _waitingRequestor);

			if (_visual != null)
				_visual.SetActive(false);

			// скрываем тултип
			if (_tooltip != null)
				_tooltip.Hide();

			_currentTarget = null;

			if (_attachedTrigger != null)
				Destroy(_attachedTrigger);

			_attachedTrigger = null;

			if (_attachedCanvas != null)
			{
				if (_attachedCanvas.TryGetComponent(out GraphicRaycaster graphicRaycaster))
				{
					Destroy(graphicRaycaster);
				}

				Destroy(_attachedCanvas);
			}

			_attachedCanvas = null;
		}

		private void OnClickBlocker()
		{
			if (_clickBlockerHide)
			{
				StopWait();
			}
		}
	}
}