using Plugins.Utilities;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace TD.Plugins.Timing
{
	public class TimeControl
	{
		public static TimeControl Instance = new();
		public bool IsPaused => Time.timeScale == 0;
		[ShowInInspector] public DisablerList Pause { get; private set; }
		[ShowInInspector] public DisablerList SlowMo { get; private set; }
		[ShowInInspector] public DisablerList ForceUnpause { get; private set; }
		[ShowInInspector] private float timeScale => Time.timeScale;
		private float slowMo = .01f;
		private float currentTimeScale;

#if UNITY_EDITOR
		[InitializeOnEnterPlayMode]
#endif
		private static void InitializeOnEnterPlayMode()
		{
			Instance = new();
		}

		public TimeControl()
		{
			Pause = new(OnChanged);
			SlowMo = new(OnChanged);
			ForceUnpause = new(OnChanged);
			OnChanged(false, null);
		}

		public void Update()
		{
			Shader.SetGlobalFloat("_UnscaledTime", Time.realtimeSinceStartup);
		}

		private void OnChanged(bool hasEntries) => OnChanged(false, null);

		private void OnChanged(bool isEnabled, object disabler)
		{
			currentTimeScale = SlowMo.HasEntries ? slowMo : 1;
			if (Pause.HasEntries && ForceUnpause.NoEntries)
			{
				AudioListener.pause = true;
				Time.timeScale = 0;
			}
			else
			{
				AudioListener.pause = false;
				Time.timeScale = currentTimeScale;
			}

			Debug.Log($"timeScale {timeScale}");
		}

        #if UNITY_EDITOR
		[OnInspectorGUI]
		private void OnInspectorGUI()
		{
			GUIHelper.RequestRepaint();
		}
#endif
	}
}