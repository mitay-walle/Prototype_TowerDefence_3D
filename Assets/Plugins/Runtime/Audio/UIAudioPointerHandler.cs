using UnityEngine;
using UnityEngine.EventSystems;

namespace Plugins.Audio
{
	public class UIAudioPointerHandler : MonoBehaviour, IPointerDownHandler, IMoveHandler, IPointerEnterHandler
	{
		[SerializeField] private AudioClip _onPointerEnter;
		[SerializeField] private AudioClip _onClick;
		[SerializeField] private AudioClip _onMove;
		[SerializeField] private AudioClip _onEnable;
		[SerializeField] private AudioSource _source;

		public void OnEnablePlay()
		{
			if (_onEnable)
			{
				if (_source)
				{
					_source.PlayOneShot(_onEnable);
				}
				else
				{
					AudioSource.PlayClipAtPoint(_onEnable, Vector3.zero);
				}
			}
		}
		
		public void OnPointerEnter()
		{
			if (_onPointerEnter)
			{
				if (_source)
				{
					_source.PlayOneShot(_onPointerEnter);
				}
				else
				{
					AudioSource.PlayClipAtPoint(_onPointerEnter, Vector3.zero);
				}
			}
		}
		public void OnClick()
		{
			if (_onClick)
			{
				if (_source)
				{
					_source.PlayOneShot(_onClick);
				}
				else
				{
					AudioSource.PlayClipAtPoint(_onClick, Vector3.zero);
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

		void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
		{
			if (_onClick)
			{
				OnClick();
			}
			else
			{
				GetComponentsInParent<UIAudioRoot>()[0].OnClick();
			}
		}
		void IMoveHandler.OnMove(AxisEventData eventData)
		{
			if (_onMove)
			{
				OnMove();
			}
			else
			{
				GetComponentsInParent<UIAudioRoot>()[0].OnMove();
			}
		}
		void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
		{
			if (_onPointerEnter)
			{
				OnPointerEnter();
			}
			else
			{
				GetComponentsInParent<UIAudioRoot>()[0].OnPointerEnter();
			}
		}
	}
}