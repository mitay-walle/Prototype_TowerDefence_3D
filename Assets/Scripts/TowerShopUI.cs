using System;
using System.Collections.Generic;
using Mitaywalle.UI.Sector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

namespace TD
{
	public class TowerShopUI : MonoBehaviour
	{
		[SerializeField] private List<TowerInfo> towers;
		[SerializeField] private InputActionReference _buildHotkey;
		[SerializeField] private TowerPreviewGenerator previewGen;
		[SerializeField] private TowerPlacementSystem placementSystem;
		[SerializeField] private RectTransform content;
		[SerializeField] private GameObject towerButtonPrefab;
		[SerializeField] private TD.UI.TowerShopTooltipHelper tooltipHelper;
		List<Button> _buttons = new List<Button>();

		[Serializable]
		public class TowerInfo
		{
			public GameObject prefab;
			public int price;
		}

		void Start()
		{
			_buildHotkey.action.Enable();
			_buildHotkey.action.started -= OnHotkey;
			_buildHotkey.action.started += OnHotkey;
			FillUI();
			gameObject.SetActive(false);
		}

		void OnDestroy()
		{
			_buildHotkey.action.started -= OnHotkey;
			_buildHotkey.action.Disable();
		}

		void OnEnable()
		{
			FindAnyObjectByType<RTSCameraController>().DisablerList.Add(this);
		}

		void OnDisable()
		{
			FindAnyObjectByType<RTSCameraController>().DisablerList.Remove(this);
		}

		void Update()
		{
			if (_buttons.Count == 0 || !EventSystem.current) return;

			if (!_buttons.Exists(Button => Button.gameObject == EventSystem.current.currentSelectedGameObject))
			{
				EventSystem.current.SetSelectedGameObject(_buttons[0].gameObject);
			}
		}

		void OnHotkey(CallbackContext callbackContext)
		{
			if (FindAnyObjectByType<TowerPlacementSystem>().IsPlacing) return;
			gameObject.SetActive(!gameObject.activeSelf);
			EventSystem.current.SetSelectedGameObject(_buttons[0].gameObject);
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}

		void FillUI()
		{
			// foreach (Transform c in content)
			// {
			// 	Destroy(c.gameObject);
			// }
			_buttons.Clear();

			previewGen.GeneratePreviews();

			foreach (var info in towers)
			{
				var go = Instantiate(towerButtonPrefab, content);
				go.name = info.prefab.name;

				var img = go.transform.Find("Icon").GetComponent<RawImage>();
				var txt = go.transform.Find("Price").GetComponent<TMP_Text>();

				if (previewGen.previews.TryGetValue(info.prefab, out var tex))
				{
					img.texture = tex;
				}

				txt.text = "$" + info.price;

				Button btn = go.GetComponent<Button>();
				_buttons.Add(btn);

				btn.onClick.AddListener(() =>
				{
					placementSystem.BeginPlacement(info.prefab);
					Hide();
				});

				if (tooltipHelper != null)
				{
					var towerStats = info.prefab.GetComponent<Turret>()?.Stats;
					if (towerStats != null)
					{
						tooltipHelper.SetupTooltip(go, towerStats, info.prefab.name);
					}
				}
			}

			foreach (Button btn in _buttons)
			{
				var navigation = btn.navigation;

				// navigation.selectOnLeft = btn.FindSelectableOnLeft();
				// navigation.selectOnRight = btn.FindSelectableOnRight();
				// navigation.selectOnUp = btn.FindSelectableOnUp();
				// navigation.selectOnDown = btn.FindSelectableOnDown();
				// navigation.mode = Navigation.Mode.Explicit;
				navigation.wrapAround = false;
				btn.navigation = navigation;
			}

			Canvas.ForceUpdateCanvases();
		}
	}
}