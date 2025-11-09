using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace InputSystemActionPrompts
{
	public abstract class PromptBase : MonoBehaviour
	{
		[ShowInInspector, ReadOnly, HideInEditorMode] private bool isUsingGamepad = false;
		const float analogThreshold = 0.1f;

		void Update()
		{
			DetectMouseUsage();
			DetectGamepadUsage();
		}

		private void DetectMouseUsage()
		{
			// Проверяем движение мыши или нажатия
			if (Mouse.current == null)
			{
				return;
			}

			if (Mouse.current.wasUpdatedThisFrame)
				// if (Mouse.current.delta.IsActuated() || Mouse.current.leftButton.isPressed ||
				//     Mouse.current.rightButton.isPressed)
			{
				if (isUsingGamepad)
				{
					isUsingGamepad = false;
					Refresh();
				}
			}
		}

		private void DetectGamepadUsage()
		{
			if (!IsGamepadActive())
			{
				return;
			}

			if (!isUsingGamepad)
			{
				isUsingGamepad = true;
				Refresh();
			}
		}

		bool IsGamepadActive()
		{
			if (Gamepad.current == null) return false;
			if (Gamepad.current.wasUpdatedThisFrame) return true;

			foreach (var control in Gamepad.current.allControls)
			{
				if (control is StickControl stick && stick.ReadValue().magnitude > analogThreshold)
					return true;

				if (control is AxisControl axis && axis.ReadValue() > analogThreshold)
					return true;
			}

			return false;
		}

		public abstract void Refresh();
	}
}