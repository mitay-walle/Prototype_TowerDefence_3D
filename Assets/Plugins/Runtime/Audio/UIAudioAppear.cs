using UnityEngine;

namespace Plugins.Audio
{
	public class UIAudioAppear : MonoBehaviour
	{
		[SerializeField] private AudioClip _onEnable;
		[SerializeField] private AudioSource _source;
		
		private void OnEnable()
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
			else
			{
				GetComponentsInParent<UIAudioRoot>()[0].OnWindowAppear();
			}
		}
	}
}