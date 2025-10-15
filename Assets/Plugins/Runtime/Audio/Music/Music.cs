using System;
using System.Collections.Generic;
using System.Linq;
using Plugins.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace TD.Plugins.Runtime.Audio.Music
{
	public class Music : DisablerListBehaviour
	{
		private SortedDictionary<eMusic, MusicLayer> _layers = new();

		[ShowInInspector, HideInEditorMode, ReadOnly] private eMusic? _current;

		protected void Start()
		{
			_layers.Clear();
			var found = GetComponentsInChildren<MusicLayer>().ToDictionary(l =>
			{
				if (Enum.TryParse(l.name, out eMusic value))
				{
					return value;
				}

				Debug.LogError($"[ Music ] Awake | MusicLayer object name '{l.name}' in invalid!", l);
				return default;
			});

			foreach (var kvp in found)
			{
				_layers.Add(kvp.Key, kvp.Value);
			}

			ValidateCurrentLayer();
		}

		private void Update()
		{
			if (!VolumeManager.instance.stack?.GetComponent<MusicVolumeComponent>()) return;

			eMusic last = VolumeManager.instance.stack.GetComponent<MusicVolumeComponent>().Music.value;
			if (last != _current)
			{
				ValidateCurrentLayer();
			}
		}

		private void ValidateCurrentLayer()
		{
			if (!VolumeManager.instance.stack?.GetComponent<MusicVolumeComponent>()) return;

			eMusic last = VolumeManager.instance.stack.GetComponent<MusicVolumeComponent>().Music.value;

			if (Logs) Debug.Log($"[ Music ] ValidateCurrentLayer | current {_current} | new {last}");

			if (_current == null || _current != last)
			{
				_current = last;
				foreach (var kvp in _layers)
				{
					if (kvp.Key == last)
					{
						if (Logs) Debug.Log($"[ Music ] ValidateCurrentLayer | enable {last}");
						kvp.Value.DisablerList.Add(this);
						kvp.Value.DisablerList.Remove(this);
					}
					else
					{
						kvp.Value.DisablerList.Add(this);
					}
				}
			}
		}
	}
}