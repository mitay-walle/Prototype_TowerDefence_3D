using System;
using System.Collections.Generic;
using TD.Interactions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

namespace TD.Towers
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

		public void Initialize()
		{
			gameObject.SetActive(true);
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
			_buttons.Clear();

			if (towers == null || towers.Count == 0)
			{
				LoadTowersFromResources();
			}

			if (previewGen != null)
			{
				previewGen.GeneratePreviews();
			}

			foreach (var info in towers)
			{
				var go = Instantiate(towerButtonPrefab, content);
				go.name = info.prefab.name;

				var img = go.transform.Find("Icon").GetComponent<RawImage>();
				var txt = go.transform.Find("Price").GetComponent<TMP_Text>();

				if (previewGen != null && previewGen.previews.TryGetValue(info.prefab, out var tex))
				{
					img.texture = tex;
				}

				var towerStats = info.prefab.GetComponent<Tower>()?.Stats;
				int actualPrice = towerStats != null ? towerStats.Cost : info.price;
				txt.text = $"${actualPrice}";

				Button btn = go.GetComponent<Button>();
				_buttons.Add(btn);

				btn.onClick.AddListener(() =>
				{
					placementSystem.BeginPlacement(info.prefab);
					Hide();
				});

				if (tooltipHelper != null)
				{
					if (towerStats != null)
					{
						tooltipHelper.SetupTooltip(go, towerStats, info.prefab.name);
					}
				}
			}

			foreach (Button btn in _buttons)
			{
				var navigation = btn.navigation;
				navigation.wrapAround = false;
				btn.navigation = navigation;
			}

			Canvas.ForceUpdateCanvases();
		}

		private void LoadTowersFromResources()
		{
			if (towers == null)
				towers = new List<TowerInfo>();
			else
				towers.Clear();

			var stats = Resources.LoadAll<TowerStats>("TowerStats");
			var prefabs = Resources.LoadAll<GameObject>("Towers");

			foreach (var stat in stats)
			{
				var prefab = System.Array.Find(prefabs, p => p.name == stat.TowerName);
				if (prefab != null)
				{
					towers.Add(new TowerInfo { prefab = prefab, price = stat.Cost });
				}
				else
				{
					var anyPrefab = prefabs.Length > 0 ? prefabs[0] : null;
					if (anyPrefab != null)
					{
						towers.Add(new TowerInfo { prefab = anyPrefab, price = stat.Cost });
					}
				}
			}
		}
	}
}