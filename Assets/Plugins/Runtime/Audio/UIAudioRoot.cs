using System;
using UnityEngine;

namespace Plugins.Audio
{
	public class UIAudioRoot : MonoBehaviour
	{
		[SerializeField] private AudioClip[] _onPointerEnter;
		[SerializeField] private AudioClip[] _onClick;
		[SerializeField] private AudioClip _onMove;
		[SerializeField] private AudioSource _source;
		[SerializeField] private AudioClip _onWindowAppear;
		[SerializeField] private TimeTrigger _windowTimer = new TimeTrigger(.1f);

		public void OnWindowAppear()
		{
			if (!_windowTimer.IsReady()) return;
			_windowTimer.Restart();
			if (_onWindowAppear)
			{
				if (_source)
				{
					_source.PlayOneShot(_onWindowAppear);
				}
				else
				{
					AudioSource.PlayClipAtPoint(_onWindowAppear, Vector3.zero);
				}
			}
		}

		public void OnPointerEnter()
		{
			if (_onPointerEnter is { Length: > 0 })
			{
				if (_source)
				{
					_source.PlayOneShot(Random(_onPointerEnter));
				}
				else
				{
					AudioSource.PlayClipAtPoint(Random(_onPointerEnter), Vector3.zero);
				}
			}
		}

		public void OnClick()
		{
			if (_onClick is { Length: > 0 })
			{
				if (_source)
				{
					_source.PlayOneShot(Random(_onClick));
				}
				else
				{
					AudioSource.PlayClipAtPoint(Random(_onClick), Vector3.zero);
				}
			}
		}

		public void OnMove()
		{
			if (_onMove)
			{
				if (_source)
				{
					_source.PlayOneShot(_onMove);
				}
				else
				{
					AudioSource.PlayClipAtPoint(_onMove, Vector3.zero);
				}
			}
		}

		[Serializable]
		private struct TimeTrigger
		{
			float endTime;
			public float time;

			public TimeTrigger(float time)
			{
				this.time = time;
				endTime = 0;
			}

			public bool CheckAndRestart()
			{
				bool isComplete = IsReady();
				if (isComplete)
					Restart();
				return isComplete;
			}

			public bool CheckAndCancel()
			{
				bool isComplete = IsReady();
				if (isComplete)
					endTime = float.MaxValue;
				return isComplete;
			}

			public bool IsReady()
			{
				return Time.time >= endTime;
			}

			public void Restart()
			{
				endTime = Time.time + time;
			}

			public void Restart(float time)
			{
				this.time = time;
				endTime = Time.time + time;
			}

			public void Reset()
			{
				endTime = Time.time;
			}

			public void Reset(float time)
			{
				endTime = Time.time + time;
			}

			public float GetTimeLeft()
			{
				return Mathf.Max(0, endTime - Time.time);
			}

			public float GetTimeElapsed()
			{
				return Mathf.Max(0, endTime - GetTimeLeft());
			}

			public float GetStartTime()
			{
				return endTime - time;
			}
		}

		private static AudioClip Random(AudioClip[] clips)
		{
			if (clips == null) return null;
			if (clips.Length == 0) return null;

			return clips[UnityEngine.Random.Range(0, clips.Length)];
		}
	}
}