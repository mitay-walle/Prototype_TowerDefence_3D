using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TD.Towers
{
	[Serializable]
	public class TowerStatsVisual
	{
		[SerializeField] GameObject _range;
		private DecalProjector _rangeProjector;
		private Material _coverageMaterial;

		public void Show(Tower tower)
		{
			if (_range == null || tower == null)
				return;

			UpdateRange(tower);
			_range.SetActive(true);
		}

		public void UpdateRange(Tower tower)
		{
			if (_range == null || tower == null)
				return;

			_range.transform.localScale = Vector3.one * tower.EffectiveRange * 2;
		}

		public void Hide()
		{
			if (_range == null)
				return;

			_range.SetActive(false);
		}

		public void SetCoverageFeedback(int coveredEntrances, int totalEntrances)
		{
			if (_range == null || totalEntrances <= 0)
				return;

			if (_rangeProjector == null)
				_rangeProjector = _range.GetComponent<DecalProjector>();

			if (_rangeProjector == null || _rangeProjector.material == null)
				return;

			if (_coverageMaterial == null)
			{
				_coverageMaterial = new Material(_rangeProjector.material);
				_rangeProjector.material = _coverageMaterial;
			}

			if (_coverageMaterial.HasProperty("_BaseColor"))
			{
				var coverageRatio = Mathf.Clamp01((float)coveredEntrances / totalEntrances);
				_coverageMaterial.SetColor("_BaseColor", Color.Lerp(Color.red, Color.green, coverageRatio));
			}
		}

		public void Dispose()
		{
			if (_coverageMaterial == null)
				return;

			if (Application.isPlaying)
				UnityEngine.Object.Destroy(_coverageMaterial);
			else
				UnityEngine.Object.DestroyImmediate(_coverageMaterial);

			_coverageMaterial = null;
		}
	}
}