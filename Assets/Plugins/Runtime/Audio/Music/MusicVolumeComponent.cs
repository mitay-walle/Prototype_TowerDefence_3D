using UnityEngine.Rendering;

namespace TD.Plugins.Runtime.Audio.Music
{
	public class MusicVolumeComponent : VolumeComponent
	{
		public EnumParameter<eMusic> Music = new EnumParameter<eMusic>(eMusic.Discovery);
	}
}