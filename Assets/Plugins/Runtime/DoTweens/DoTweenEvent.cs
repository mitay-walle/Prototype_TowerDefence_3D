using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace TD
{
	public class DoTweenEvent : MonoBehaviour
	{
		[SerializeField] private bool playOnEnable = true;
		public float duration = .25f;
		public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		public float currentValue;
		public UnityEvent<float> OnTween;

		void OnEnable()
		{
			if (playOnEnable)
			{
				Animate();
			}
		}

		[Button]
		public void Animate()
		{
			var startValue = currentValue;
			var targetValue = 0;
			Tween tTween = DOTween.To(() => 0f, // твинит время от 0 до 1
				t =>
				{
					currentValue = curve.Evaluate(t); // применяем AnimationCurve
					OnTween?.Invoke(currentValue);
				}, 1f, duration).SetEase(Ease.Linear); // линейно, чтобы кривая решала всё сама

			tTween.SetTarget(this);
		}

		[UsedImplicitly]
		public void SetVolumeWeight(Volume Volume)
		{
			Volume.weight = currentValue;
		}

		[UsedImplicitly]
		public void SetVolumeWeight(float Weight)
		{
			if (TryGetComponent(out Volume volume))
			{
				volume.weight = currentValue;
			}
		}
	}
}