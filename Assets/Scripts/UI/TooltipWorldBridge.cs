using System;
using System.Collections.Generic;
using TD.UI.Information;
using UnityEngine;
using UnityEngine.Localization;

namespace TD.UI
{
	public class TooltipWorldBridge : MonoBehaviour
	{
		private const string tableName = "UI";

		private AutoPositionalTooltip tooltipSystem;
		private RectTransform proxyRect;
		private Canvas tooltipCanvas;
		private Camera mainCamera;

		public bool IsShowingTooltip => proxyRect && tooltipSystem.lastTarget == proxyRect;

		private void Awake()
		{
			mainCamera = Camera.main;
			FindTooltipSystem();
			CreateProxyRect();
		}

		private void FindTooltipSystem()
		{
			tooltipSystem = FindFirstObjectByType<AutoPositionalTooltip>();

			if (tooltipSystem != null)
			{
				tooltipCanvas = tooltipSystem.GetComponentInParent<Canvas>();
			}
		}

		private void CreateProxyRect()
		{
			if (tooltipCanvas == null) return;

			GameObject proxy = new GameObject($"TooltipProxy_{gameObject.name}");
			proxy.transform.SetParent(tooltipCanvas.transform, false);
			proxyRect = proxy.AddComponent<RectTransform>();
			proxyRect.sizeDelta = Vector2.one;
			proxyRect.gameObject.SetActive(false);
		}

		public void ShowTooltip()
		{
			if (tooltipSystem == null || proxyRect == null) return;

			UpdateProxyPosition();
			LocalizedString title = null;
			LocalizedString description = null;
			IEnumerable<(Action, LocalizedString)> buttonActions = null;
			if (TryGetComponent<ITooltipValues>(out var tooltip))
			{
				title = tooltip.Title;
				description = tooltip.Description;
				buttonActions = tooltip.OnTooltipButtonClick;
			}

			proxyRect.gameObject.SetActive(true);
			tooltipSystem.Show(proxyRect, title, description, buttonActions);
		}

		public void HideTooltip()
		{
			if (tooltipSystem == null || proxyRect == null) return;

			tooltipSystem.Hide();
			proxyRect.gameObject.SetActive(false);
		}

		private void Update()
		{
			if (proxyRect != null && proxyRect.gameObject.activeSelf)
			{
				UpdateProxyPosition();
			}
		}

		private void UpdateProxyPosition()
		{
			if (mainCamera == null || proxyRect == null || tooltipCanvas == null) return;

			// 1️⃣ Получаем мировую позицию объекта и конвертируем в экранную
			Vector3 worldPosition = transform.position;
			Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

			if (screenPosition.z < 0)
			{
				proxyRect.gameObject.SetActive(false);
				return;
			}

			// 2️⃣ Рассчитываем общий Bounds всех MeshRenderer в объекте
			var renderers = GetComponentsInChildren<MeshRenderer>();
			if (renderers.Length > 0)
			{
				Bounds combinedBounds = renderers[0].bounds;
				for (int i = 1; i < renderers.Length; i++)
				{
					combinedBounds.Encapsulate(renderers[i].bounds);
				}

				// Переводим размеры Bounds в локальные координаты Canvas
				Vector3[] corners = new Vector3[8];

				// Создаем 8 вершин куба Bounds
				Vector3 extents = combinedBounds.extents;
				Vector3 center = combinedBounds.center;

				corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
				corners[1] = center + new Vector3(extents.x, -extents.y, -extents.z);
				corners[2] = center + new Vector3(-extents.x, extents.y, -extents.z);
				corners[3] = center + new Vector3(extents.x, extents.y, -extents.z);
				corners[4] = center + new Vector3(-extents.x, -extents.y, extents.z);
				corners[5] = center + new Vector3(extents.x, -extents.y, extents.z);
				corners[6] = center + new Vector3(-extents.x, extents.y, extents.z);
				corners[7] = center + new Vector3(extents.x, extents.y, extents.z);

				Vector2 min = Vector2.positiveInfinity;
				Vector2 max = Vector2.negativeInfinity;

				foreach (var corner in corners)
				{
					Vector3 screenPoint = mainCamera.WorldToScreenPoint(corner);
					RectTransformUtility.ScreenPointToLocalPointInRectangle(tooltipCanvas.transform as RectTransform, screenPoint,
						tooltipCanvas.worldCamera, out Vector2 localPoint);

					min = Vector2.Min(min, localPoint);
					max = Vector2.Max(max, localPoint);
				}

				proxyRect.anchoredPosition = (min + max) / 2f;
				proxyRect.sizeDelta = max - min;
			}
			else
			{
				// Если нет MeshRenderer, просто позиционируем как раньше
				RectTransform canvasRect = tooltipCanvas.transform as RectTransform;
				RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, tooltipCanvas.worldCamera,
					out Vector2 localPoint);

				proxyRect.anchoredPosition = localPoint;
				proxyRect.sizeDelta = Vector2.one;
			}

			proxyRect.gameObject.SetActive(true);
		}

		private void OnDestroy()
		{
			if (proxyRect != null)
			{
				Destroy(proxyRect.gameObject);
			}
		}

		private void OnDisable()
		{
			HideTooltip();
		}

		public void RefreshTooltipIfNeed()
		{
			if (IsShowingTooltip)
			{
				ShowTooltip();
			}
		}
	}
}