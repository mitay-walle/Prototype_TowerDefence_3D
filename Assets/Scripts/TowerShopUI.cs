using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

namespace TD
{
	public class TowerShopUI : MonoBehaviour
	{
		[SerializeField] private InputActionReference _buildHotkey;
		[SerializeField] private TowerPreviewGenerator previewGen;
		[SerializeField] private TowerPlacementSystem placementSystem;
		[SerializeField] private RectTransform content;
		[SerializeField] private GameObject towerButtonPrefab;

		[System.Serializable]
		public class TowerInfo
		{
			public GameObject prefab;
			public int price;
		}

		public List<TowerInfo> towers;

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

		void OnHotkey(CallbackContext callbackContext)
		{
			gameObject.SetActive(!gameObject.activeSelf);
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}

		void FillUI()
		{
			foreach (Transform c in content) Destroy(c.gameObject);

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

				go.GetComponent<Button>().onClick.AddListener(() =>
				{
					placementSystem.BeginPlacement(info.prefab);
					Hide();
				});
			}
		}
	}
}