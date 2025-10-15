using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Plugins.mitaywalle.Audio
{
	public enum ePlayAgain
	{
		None,
		ByClipLength,
		ByDelayBetweenPlays,
	}

	[Serializable]
	public class PlayRandomSoundValues
	{
		public AudioClip[] clips;
		public Vector2Int clipIndexes = new Vector2Int(0, int.MaxValue);
		public bool playOnEnable;
		public bool oneShot;
		public bool randomTime;
		public bool useVolume;
		[ShowIf("useVolume")] public Vector2 volume = Vector2.one;
		public bool usePitch;
		[ShowIf("usePitch")] public Vector2 pitch = Vector2.one;
		public Vector2 delay;
		public float fadeIn;
		public ePlayAgain playAgain;
		[ShowIf("@playAgain !=ePlayAgain.None")] public Vector2 delayBetweenPlays;
	}

	//[ExecuteAlways]
	public class PlayRandomSound : MonoBehaviour
	{
		public AudioSource _source;
#if UNITY_EDITOR
		[SerializeField, InlineButton("AddAllFolder", "Load", ShowIf = "_folder")] DefaultAsset _folder;
#endif
  
    
		public PlayRandomSoundValues Values = new PlayRandomSoundValues();

		private Coroutine _fade;
		private Coroutine _repeat;

		private void OnEnable()
		{
			if (Values.playOnEnable)
			{
				if (Values.delay.x > 0 || Values.delay.y > 0)
				{
					Invoke(nameof(Play), Values.delay.Random());
				}
				else
				{
					Play();
				}
			}
		}

		[Button]
		public void Play()
		{
			if (!_source) Reset();
			if (!_source) return;

			if (Values.usePitch) _source.pitch = Values.pitch.Random();

			AudioClip clip = _source.clip;

			if (Values.clips != null && Values.clips.Length > 0)
			{
				Vector2Int clipIndexRange = new Vector2Int(Mathf.Max(Values.clipIndexes.x, 0), Mathf.Min(Values.clips.Length, Values.clipIndexes.y));
				int index = clipIndexRange.Random();
				clip = Values.clips[index];
			}

			if (!clip) return;

			if (Values.oneShot)
			{
				float volume = 1;
				if (Values.useVolume) volume = Values.volume.Random();
				if (_source.isActiveAndEnabled)
				{
					_source.PlayOneShot(clip, volume);
				}
			}
			else
			{
				if (Values.useVolume) _source.volume = Values.volume.Random();
				_source.clip = clip;
				if (_source.isActiveAndEnabled)
				{
					_source.Play();
				}
				FadeInOnPlay();
			}

			if (Values.randomTime && _source.clip) _source.time = Random.Range(0, _source.clip.length);

			TryPlayAgain(clip);
		}

		private void TryPlayAgain(AudioClip lastClip)
		{
			if (_repeat != null) StopCoroutine(_repeat);
			switch (Values.playAgain)
			{
				case ePlayAgain.None: break;
				case ePlayAgain.ByClipLength:
					{
						if (isActiveAndEnabled)
						{
							_repeat = StartCoroutine(PlayAgainDelayed(lastClip.length - _source.time + Values.delayBetweenPlays.Random()));
						}
						break;
					}
				case ePlayAgain.ByDelayBetweenPlays:
					{
						if (isActiveAndEnabled)
						{
							_repeat = StartCoroutine(PlayAgainDelayed(Values.delayBetweenPlays.Random()));
						}
						break;
					}
				default: throw new ArgumentOutOfRangeException();
			}
		}

		private IEnumerator PlayAgainDelayed(float delay)
		{
			yield return new WaitForSeconds(delay);
			Play();
		}

		private void FadeInOnPlay()
		{
			if (Values.fadeIn > 0 && gameObject.activeInHierarchy)
			{
				float volume = _source.volume;

				if (Values.useVolume)
				{
					volume = Values.volume.Random();
				}
				FadeIn(Values.fadeIn, volume);
			}
		}

		public void FadeIn(float duration = 1, float volume = 1)
		{
			if (duration > 0 && gameObject.activeInHierarchy)
			{
				if (_fade != null) StopCoroutine(_fade);
				_fade = StartCoroutine(FadeCoroutine(duration, volume));
			}
		}

		public void FadeOutStop(float duration = 1)
		{
			FadeOut(duration, _source.Stop);
		}

		private void FadeOut(float duration, Action onFinish = null)
		{
			if (duration > 0 && gameObject.activeInHierarchy)
			{
				if (_fade != null) StopCoroutine(_fade);
				_fade = StartCoroutine(FadeCoroutine(duration, 0, onFinish));
			}
		}

		private IEnumerator FadeCoroutine(float duration, float target, Action onFinish = null)
		{
			float volume = _source.volume;

			float startTime = Time.time;
			float time = Time.time - startTime;
			while (time < duration)
			{
				time = Time.time - startTime;
				float normTime = time / duration;
				_source.volume = Mathf.Lerp(volume, target, normTime);
				yield return null;
			}
			onFinish?.Invoke();
		}

		private void Reset()
		{
			_source = GetComponent<AudioSource>();
			if (!_source) _source = GetComponentInChildren<AudioSource>(true);
			if (!_source) _source = GetComponentInChildren<AudioSource>(true);
			if (!_source) _source = GetComponentInParent<AudioSource>();
			if (!_source) _source = gameObject.AddComponent<AudioSource>();
		}

#if UNITY_EDITOR
		//[Button]
		private void AddAllFolder(DefaultAsset folder)
		{
			if (!folder) return;
			Values.clips = AssetDatabase.FindAssets("t:AudioClip", new[] { AssetDatabase.GetAssetPath(folder) }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).ToArray();
			EditorUtility.SetDirty(this);
		}
#endif
	}

	public static class RandomExtension
	{
		public static float Random(this Vector2 range) => UnityEngine.Random.Range(range.x, range.y);
		public static int Random(this Vector2Int range) => UnityEngine.Random.Range(range.x, range.y);

		public static T Random<T>(this IList<T> list) => list[UnityEngine.Random.Range(0, list.Count)];
	}
}