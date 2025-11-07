using Plugins.mitaywalle.Audio;
using Plugins.Utilities;
using UnityEngine;

namespace TD.Plugins.Runtime.Audio.Music
{
	[RequireComponent(typeof(PlayRandomSound))]
	public class MusicLayer : DisablerListBehaviour
	{
		[SerializeField] private float _fade = 1;
		[SerializeField] private float _volume = 1;
		private PlayRandomSound _player;

		protected override void OnChanged()
		{
			_player ??= GetComponent<PlayRandomSound>();
			if (DisablerList.NoEntries)
			{
				if (FindAnyObjectByType<Music>().Logs) Debug.Log($"'{name}' play");
				_player._source.ignoreListenerPause = true; 
				_player.Play();
				_player.FadeIn(_fade, _volume);
			}
			else
			{
				if (FindAnyObjectByType<Music>().Logs) Debug.Log($"'{name}' fade stop");
				_player.FadeOutStop();
			}
			base.OnChanged();
		}
	}
}