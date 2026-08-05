using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;

namespace TD.Interactions
{
	[DefaultExecutionOrder(-1000)]
	public class SyntheticGamepad : MonoBehaviour
	{
		private Gamepad _virtualGamepad;
		private GamepadState _state;
		private GamepadButton? _buttonToRelease;
		private bool _afterUpdateHookRegistered;
		private InputSettings.BackgroundBehavior _previousBackgroundBehavior;
		private InputSettings.UpdateMode _previousUpdateMode;
		private bool _backgroundBehaviorCaptured;
		private SynchronizationContext _unityContext;
		private int _unityThreadId;

		public bool IsReady => _virtualGamepad != null && _virtualGamepad.added;
		public Vector2 LeftStick => _state.leftStick;
		public Vector2 RightStick => _state.rightStick;
		public float LeftTrigger => _state.leftTrigger;
		public float RightTrigger => _state.rightTrigger;

		private void OnEnable()
		{
			if (_virtualGamepad == null)
			{
				_virtualGamepad = InputSystem.AddDevice<Gamepad>("TD Synthetic Gamepad");
			}
			else if (!_virtualGamepad.added)
			{
				InputSystem.AddDevice(_virtualGamepad);
			}

			_previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
			_previousUpdateMode = InputSystem.settings.updateMode;
			_backgroundBehaviorCaptured = true;
			InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
			InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
			_unityContext = SynchronizationContext.Current;
			_unityThreadId = Environment.CurrentManagedThreadId;
			InputSystem.onBeforeUpdate += OnInputSystemBeforeUpdate;
			InputSystem.onAfterUpdate += OnInputSystemAfterUpdate;
			Reset();
		}

		private void Start()
		{
			PairWithPlayerInput();
		}

		private void PairWithPlayerInput()
		{
			if (!IsReady)
				return;

			var playerInput = FindFirstObjectByType<PlayerInput>();
			if (playerInput == null || !playerInput.user.valid)
				return;

			InputUser.PerformPairingWithDevice(_virtualGamepad, playerInput.user);
			_virtualGamepad.MakeCurrent();
		}

		private void OnDisable()
		{
			InputSystem.onBeforeUpdate -= OnInputSystemBeforeUpdate;
			InputSystem.onAfterUpdate -= OnInputSystemAfterUpdate;

			if (_backgroundBehaviorCaptured)
			{
				InputSystem.settings.backgroundBehavior = _previousBackgroundBehavior;
				InputSystem.settings.updateMode = _previousUpdateMode;
				_backgroundBehaviorCaptured = false;
			}

			_buttonToRelease = null;
			_afterUpdateHookRegistered = false;

			if (_virtualGamepad != null && _virtualGamepad.added)
			{
				InputSystem.RemoveDevice(_virtualGamepad);
			}
		}

		public void ExecuteOnUnityThread(Action action)
		{
			if (Environment.CurrentManagedThreadId == _unityThreadId)
			{
				action();
				return;
			}

			if (_unityContext == null)
			{
				throw new InvalidOperationException("Unity synchronization context is unavailable.");
			}

			Exception exception = null;
			using (var completed = new ManualResetEventSlim(false))
			{
				_unityContext.Post(_ =>
				{
					try
					{
						action();
					}
					catch (Exception error)
					{
						exception = error;
					}
					finally
					{
						completed.Set();
					}
				}, null);

				if (!completed.Wait(TimeSpan.FromSeconds(5)))
				{
					throw new TimeoutException("Unity main thread did not process the synthetic gamepad command.");
				}
			}

			if (exception != null)
			{
				throw new InvalidOperationException("Synthetic gamepad command failed on the Unity main thread.", exception);
			}
		}

		public void ApplyCurrentState()
		{
			ApplyState();
		}

		private void OnInputSystemBeforeUpdate()
		{
			if (IsReady)
			{
				InputSystem.QueueStateEvent(_virtualGamepad, _state);
				_virtualGamepad.MakeCurrent();
			}
		}

		private void OnInputSystemAfterUpdate()
		{
			ApplyState();
		}

		private void Update()
		{
			if (!_afterUpdateHookRegistered)
			{
				InputSystem.onAfterUpdate -= OnInputSystemAfterUpdate;
				InputSystem.onAfterUpdate += OnInputSystemAfterUpdate;
				_afterUpdateHookRegistered = true;
			}

			PublishState();
			ApplyState();
		}

		private void LateUpdate()
		{
			if (_buttonToRelease.HasValue)
			{
				var button = _buttonToRelease.Value;
				_buttonToRelease = null;
				Release(button);
			}

			PublishState();
			InputSystem.Update();
			ApplyState();
		}

		public void Press(GamepadButton button)
		{
			_state = _state.WithButton(button);
			PublishState();
		}

		public void Release(GamepadButton button)
		{
			_state = _state.WithButton(button, false);
			PublishState();
		}

		public void Click(GamepadButton button)
		{
			if (_buttonToRelease.HasValue)
			{
				Release(_buttonToRelease.Value);
			}

			Press(button);
			_buttonToRelease = button;
		}

		public void SetStick(bool left, Vector2 value)
		{
			if (left)
			{
				_state.leftStick = new Vector2(Mathf.Clamp(value.x, -1f, 1f), Mathf.Clamp(value.y, -1f, 1f));
			}
			else
			{
				_state.rightStick = new Vector2(Mathf.Clamp(value.x, -1f, 1f), Mathf.Clamp(value.y, -1f, 1f));
			}

			PublishState();
		}

		public void SetTrigger(bool left, float value)
		{
			if (left)
			{
				_state.leftTrigger = Mathf.Clamp01(value);
			}
			else
			{
				_state.rightTrigger = Mathf.Clamp01(value);
			}

			PublishState();
		}

		public void SetDpad(Vector2 value)
		{
			_state = _state
				.WithButton(GamepadButton.DpadUp, value.y > 0.5f)
				.WithButton(GamepadButton.DpadDown, value.y < -0.5f)
				.WithButton(GamepadButton.DpadLeft, value.x < -0.5f)
				.WithButton(GamepadButton.DpadRight, value.x > 0.5f);
			PublishState();
		}

		public void Reset()
		{
			_buttonToRelease = null;
			_state = new GamepadState();
			PublishState();
		}

		public bool IsButtonPressed(GamepadButton button)
		{
			return (int)button < 32 && (_state.buttons & (1U << (int)button)) != 0;
		}

		private void PublishState()
		{
			if (IsReady)
			{
				InputSystem.QueueStateEvent(_virtualGamepad, _state);
				_virtualGamepad.MakeCurrent();
			}
		}

		private void ApplyState()
		{
			if (IsReady)
			{
				InputState.Change(_virtualGamepad, _state);
				_virtualGamepad.MakeCurrent();
			}
		}
	}
}
