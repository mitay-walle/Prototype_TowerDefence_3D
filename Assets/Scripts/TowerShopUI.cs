using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class TowerShopUI : MonoBehaviour
{
	public TowerPreviewGenerator previewGen;
	public TowerPlacementSystem placementSystem;
	public RectTransform content;
	public GameObject towerButtonPrefab;

	[System.Serializable]
	public class TowerInfo
	{
		public GameObject prefab;
		public int price;
	}

	public List<TowerInfo> towers;

	void Start()
	{
		FillUI();
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
			});
		}
	}
}