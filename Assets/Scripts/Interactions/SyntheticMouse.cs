using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;

namespace TD.Interactions
{
	[DefaultExecutionOrder(-1000)]
	public class SyntheticMouse : MonoBehaviour
	{
		[SerializeField] private Vector2 _initialPosition = new(960f, 540f);
		private Mouse _virtualMouse;
		private MouseState _state;
		private MouseButton? _buttonToRelease;
		private bool _resetScrollNextFrame;
		private bool _stateQueuedForUpdate;
		private bool _afterUpdateHookRegistered;
		private InputSettings.BackgroundBehavior _previousBackgroundBehavior;
		private InputSettings.UpdateMode _previousUpdateMode;
		private bool _backgroundBehaviorCaptured;
		private SynchronizationContext _unityContext;
		private int _unityThreadId;

		public bool IsReady => _virtualMouse != null && _virtualMouse.added;
		public Vector2 Position => _state.position;

		private void OnEnable()
		{
			if (_virtualMouse == null)
			{
				_virtualMouse = InputSystem.AddDevice<Mouse>("TD Synthetic Mouse");
			}
			else if (!_virtualMouse.added)
			{
				InputSystem.AddDevice(_virtualMouse);
			}

			_previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
			_previousUpdateMode = InputSystem.settings.updateMode;
			_backgroundBehaviorCaptured = true;
			InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
			InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
			_unityThreadId = Environment.CurrentManagedThreadId;
			CaptureUnityContext();
			InputSystem.onBeforeUpdate += OnInputSystemBeforeUpdate;
			InputSystem.onAfterUpdate += OnInputSystemAfterUpdate;
			Reset();
		}

		private void CaptureUnityContext()
		{
			if (_unityContext != null)
				return;

			var context = SynchronizationContext.Current;
			if (context == null)
				return;

			_unityContext = context;
			_unityThreadId = Environment.CurrentManagedThreadId;
			Debug.Log("[TD SyntheticMouse] Unity synchronization context captured.");
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

			InputUser.PerformPairingWithDevice(_virtualMouse, playerInput.user);
			_virtualMouse.MakeCurrent();
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
			_resetScrollNextFrame = false;
			_stateQueuedForUpdate = false;
			_afterUpdateHookRegistered = false;

			if (_virtualMouse != null && _virtualMouse.added)
			{
				InputSystem.RemoveDevice(_virtualMouse);
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
					throw new TimeoutException("Unity main thread did not process the synthetic mouse command.");
				}
			}

			if (exception != null)
			{
				throw new InvalidOperationException("Synthetic mouse command failed on the Unity main thread.", exception);
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
				if (!_stateQueuedForUpdate)
				{
					InputSystem.QueueStateEvent(_virtualMouse, _state);
				}

				_stateQueuedForUpdate = false;
				_virtualMouse.MakeCurrent();
			}
		}

		private void OnInputSystemAfterUpdate()
		{
			ApplyState();
			_state.delta = Vector2.zero;
		}

		private void Update()
		{
			CaptureUnityContext();

			if (!_afterUpdateHookRegistered)
			{
				InputSystem.onAfterUpdate -= OnInputSystemAfterUpdate;
				InputSystem.onAfterUpdate += OnInputSystemAfterUpdate;
				_afterUpdateHookRegistered = true;
			}


			if (_resetScrollNextFrame)
			{
				_resetScrollNextFrame = false;
				_state.scroll = Vector2.zero;
				PublishState();
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

		public void Move(Vector2 position)
		{
			_state.delta = position - _state.position;
			_state.position = position;
			_state.scroll = Vector2.zero;
			_resetScrollNextFrame = false;
			PublishState();
		}

		public void Press(MouseButton button)
		{
			_state.delta = Vector2.zero;
			_state.scroll = Vector2.zero;
			_resetScrollNextFrame = false;
			_state.WithButton(button);
			PublishState();
		}

		public void Release(MouseButton button)
		{
			_state.delta = Vector2.zero;
			_state.scroll = Vector2.zero;
			_resetScrollNextFrame = false;
			_state.WithButton(button, false);
			PublishState();
		}

		public void Click(MouseButton button)
		{
			if (_buttonToRelease.HasValue)
			{
				Release(_buttonToRelease.Value);
			}

			Press(button);
			_buttonToRelease = button;
		}

		public void Scroll(Vector2 delta)
		{
			_state.scroll = delta;
			_resetScrollNextFrame = true;
			PublishState();
		}

		public void Reset()
		{
			_buttonToRelease = null;
			_resetScrollNextFrame = false;
			_state = new MouseState { position = _initialPosition };
			PublishState();
		}

		public bool IsButtonPressed(MouseButton button)
		{
			return (_state.buttons & (1 << (int)button)) != 0;
		}

		private void PublishState()
		{
			if (IsReady)
			{
				InputSystem.QueueStateEvent(_virtualMouse, _state);
				_stateQueuedForUpdate = true;
				_virtualMouse.MakeCurrent();
			}
		}

		private void ApplyState()
		{
			if (IsReady)
			{
				InputState.Change(_virtualMouse, _state);
				_virtualMouse.MakeCurrent();
			}
		}

	}
}
