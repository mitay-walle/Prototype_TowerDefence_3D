using System.Collections;
using System.Collections.Generic;
using TD.GameLoop;
using TD.Interactions;
using TD.Stats;
using TD.Towers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

namespace TD.UI
{
	public class TowerShopUI : MonoBehaviour
	{
		[SerializeField] private List<Tower> towers;
		[SerializeField] private InputActionReference _buildHotkey;
		[SerializeField] private TowerPreviewGenerator previewGen;
		[SerializeField] private TowerPlacementSystem placementSystem;
		[SerializeField] private RectTransform content;
		[SerializeField] private GameObject towerButtonPrefab;
		List<Button> _buttons = new List<Button>();

		public void Initialize()
		{
			gameObject.SetActive(true);
			_buildHotkey.action.Enable();
			_buildHotkey.action.started -= OnHotkey;
			_buildHotkey.action.started += OnHotkey;

			gameObject.SetActive(false);
		}

		void OnDestroy()
		{
			_buildHotkey.action.started -= OnHotkey;
			_buildHotkey.action.Disable();
		}

		void OnEnable()
		{
			FindAnyObjectByType<RTSCameraController>(FindObjectsInactive.Include).DisablerList.Add(this);
			StartCoroutine(FillUI());
		}

		void OnDisable()
		{
			FindAnyObjectByType<RTSCameraController>(FindObjectsInactive.Include).DisablerList.Remove(this);
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

		IEnumerator FillUI()
		{
			foreach (Button button in _buttons)
			{
				Destroy(button.gameObject);
			}

			content.DetachChildren();

			_buttons.Clear();
			previewGen.GeneratePreviews();

			foreach (Tower prefab in towers)
			{
				GameObject go = Instantiate(towerButtonPrefab, content);
				go.name = prefab.name;

				var img = go.transform.Find("Icon").GetComponent<RawImage>();
				var txt = go.transform.Find("Price").GetComponent<TMP_Text>();

				if (previewGen != null && previewGen.previews.TryGetValue(prefab.gameObject, out Texture2D tex))
				{
					img.texture = tex;
				}

				TowerStats towerStats = prefab.Stats;
				txt.text = prefab.Stats.statsSO.Cost.ToStringGoldCanAfford();

				Button btn = go.GetComponent<Button>();
				_buttons.Add(btn);

				btn.onClick.AddListener(() =>
				{
					placementSystem.BeginPlacement(prefab.gameObject);
					Hide();
				});

				if (towerStats != null)
				{
					TowerShopTooltipHelper.SetupTooltip(go, prefab);
				}
			}

			yield return null;

			foreach (Button btn in _buttons)
			{
				Navigation navigation = btn.navigation;
				navigation.wrapAround = false;
				btn.navigation = navigation;
				btn.gameObject.SetActive(false);
				btn.gameObject.SetActive(true);
			}
		}
	}
}