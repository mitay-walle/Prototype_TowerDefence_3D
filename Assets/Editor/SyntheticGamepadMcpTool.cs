using System;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using TD.Interactions;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

[McpForUnityTool("td_virtual_gamepad")]
public class SyntheticGamepadMcpTool
{
	public class Parameters
	{
		[ToolParameter("Operation: status, button_down, button_up, press, click, set_stick, set_trigger, set_dpad, or reset.")]
		public string operation { get; set; }

		[ToolParameter("Input source. Must be synthetic.", Required = false, DefaultValue = "synthetic")]
		public string gamepad { get; set; }

		[ToolParameter("Gamepad button: south, north, east, west, left_stick, right_stick, left_shoulder, right_shoulder, start, select, dpad_up, dpad_down, dpad_left, or dpad_right.", Required = false)]
		public string button { get; set; }

		[ToolParameter("Stick to set: left or right.", Required = false)]
		public string stick { get; set; }

		[ToolParameter("Trigger to set: left or right.", Required = false)]
		public string trigger { get; set; }

		[ToolParameter("Stick or D-pad X value. Stick range is -1 to 1.", Required = false)]
		public float? x { get; set; }

		[ToolParameter("Stick or D-pad Y value. Stick range is -1 to 1.", Required = false)]
		public float? y { get; set; }

		[ToolParameter("Trigger value from 0 to 1.", Required = false)]
		public float? value { get; set; }

		[ToolParameter("D-pad direction: neutral, up, down, left, right, up_left, up_right, down_left, or down_right.", Required = false)]
		public string direction { get; set; }
	}

	public static object HandleCommand(JObject parameters)
	{
		var gamepadSource = ReadGamepadSource(parameters);
		if (!Application.isPlaying)
		{
			return new { success = false, error = "Gameplay must be running in Play Mode.", gamepad = gamepadSource };
		}

		var gamepad = UnityEngine.Object.FindFirstObjectByType<SyntheticGamepad>(FindObjectsInactive.Include);
		if (gamepad == null)
		{
			return new { success = false, error = "SyntheticGamepad is not authored in the active scene.", gamepad = gamepadSource };
		}

		var operation = parameters.Value<string>("operation")?.ToLowerInvariant();
		gamepad.ExecuteOnUnityThread(() =>
		{
			switch (operation)
			{
				case "status":
					break;
				case "button_down":
					gamepad.Press(ReadButton(parameters));
					break;
				case "button_up":
					gamepad.Release(ReadButton(parameters));
					break;
				case "press":
				case "click":
					gamepad.Click(ReadButton(parameters));
					break;
				case "set_stick":
					gamepad.SetStick(ReadStick(parameters), ReadVector(parameters, "set_stick"));
					break;
				case "set_trigger":
					gamepad.SetTrigger(ReadTrigger(parameters), ReadTriggerValue(parameters));
					break;
				case "set_dpad":
					gamepad.SetDpad(ReadDpad(parameters));
					break;
				case "reset":
					gamepad.Reset();
					break;
				default:
					throw new ArgumentException("operation must be one of: status, button_down, button_up, press, click, set_stick, set_trigger, set_dpad, reset");
			}

			UnityEngine.InputSystem.InputSystem.Update();
			gamepad.ApplyCurrentState();
		});

		return CreateStatus(operation, gamepadSource, gamepad);
	}

	private static string ReadGamepadSource(JObject parameters)
	{
		var gamepadSource = parameters.Value<string>("gamepad")?.ToLowerInvariant() ?? "synthetic";
		if (gamepadSource != "synthetic")
		{
			throw new ArgumentException("gamepad must be 'synthetic'; hardware input is outside td_virtual_gamepad");
		}

		return gamepadSource;
	}

	private static object CreateStatus(string operation, string gamepadSource, SyntheticGamepad gamepad)
	{
		return new
		{
			success = true,
			operation,
			gamepad = gamepadSource,
			device_added = gamepad.IsReady,
			left_stick = new { x = gamepad.LeftStick.x, y = gamepad.LeftStick.y },
			right_stick = new { x = gamepad.RightStick.x, y = gamepad.RightStick.y },
			triggers = new { left = gamepad.LeftTrigger, right = gamepad.RightTrigger },
			buttons = new
			{
				south = gamepad.IsButtonPressed(GamepadButton.South),
				north = gamepad.IsButtonPressed(GamepadButton.North),
				east = gamepad.IsButtonPressed(GamepadButton.East),
				west = gamepad.IsButtonPressed(GamepadButton.West),
				left_stick = gamepad.IsButtonPressed(GamepadButton.LeftStick),
				right_stick = gamepad.IsButtonPressed(GamepadButton.RightStick),
				left_shoulder = gamepad.IsButtonPressed(GamepadButton.LeftShoulder),
				right_shoulder = gamepad.IsButtonPressed(GamepadButton.RightShoulder),
				start = gamepad.IsButtonPressed(GamepadButton.Start),
				select = gamepad.IsButtonPressed(GamepadButton.Select)
			},
			dpad = new
			{
				up = gamepad.IsButtonPressed(GamepadButton.DpadUp),
				down = gamepad.IsButtonPressed(GamepadButton.DpadDown),
				left = gamepad.IsButtonPressed(GamepadButton.DpadLeft),
				right = gamepad.IsButtonPressed(GamepadButton.DpadRight)
			}
		};
	}

	private static GamepadButton ReadButton(JObject parameters)
	{
		var value = parameters.Value<string>("button")?.ToLowerInvariant();
		return value switch
		{
			"up" or "dpad_up" => GamepadButton.DpadUp,
			"down" or "dpad_down" => GamepadButton.DpadDown,
			"left" or "dpad_left" => GamepadButton.DpadLeft,
			"right" or "dpad_right" => GamepadButton.DpadRight,
			"south" or "a" or "cross" => GamepadButton.South,
			"north" or "y" or "triangle" => GamepadButton.North,
			"east" or "b" or "circle" => GamepadButton.East,
			"west" or "x" or "square" => GamepadButton.West,
			"left_stick" or "left_stick_press" => GamepadButton.LeftStick,
			"right_stick" or "right_stick_press" => GamepadButton.RightStick,
			"left_shoulder" or "left_bumper" or "lb" => GamepadButton.LeftShoulder,
			"right_shoulder" or "right_bumper" or "rb" => GamepadButton.RightShoulder,
			"start" or "menu" => GamepadButton.Start,
			"select" or "back" => GamepadButton.Select,
			_ => throw new ArgumentException("button must be one of: south, north, east, west, left_stick, right_stick, left_shoulder, right_shoulder, start, select, dpad_up, dpad_down, dpad_left, dpad_right")
		};
	}

	private static bool ReadStick(JObject parameters)
	{
		var value = parameters.Value<string>("stick")?.ToLowerInvariant();
		return value switch
		{
			"left" => true,
			"right" => false,
			_ => throw new ArgumentException("stick must be 'left' or 'right'")
		};
	}

	private static bool ReadTrigger(JObject parameters)
	{
		var value = parameters.Value<string>("trigger")?.ToLowerInvariant();
		return value switch
		{
			"left" => true,
			"right" => false,
			_ => throw new ArgumentException("trigger must be 'left' or 'right'")
		};
	}

	private static float ReadTriggerValue(JObject parameters)
	{
		var value = parameters["value"]?.Value<float?>();
		if (!value.HasValue)
		{
			throw new ArgumentException("set_trigger requires a numeric value parameter");
		}

		return value.Value;
	}

	private static Vector2 ReadVector(JObject parameters, string operation)
	{
		var x = parameters["x"]?.Value<float?>();
		var y = parameters["y"]?.Value<float?>();
		if (!x.HasValue || !y.HasValue)
		{
			throw new ArgumentException(operation + " requires numeric x and y parameters");
		}

		return new Vector2(x.Value, y.Value);
	}

	private static Vector2 ReadDpad(JObject parameters)
	{
		var direction = parameters.Value<string>("direction")?.ToLowerInvariant();
		if (!string.IsNullOrWhiteSpace(direction))
		{
			return direction switch
			{
				"neutral" => Vector2.zero,
				"up" => Vector2.up,
				"down" => Vector2.down,
				"left" => Vector2.left,
				"right" => Vector2.right,
				"up_left" => new Vector2(-1f, 1f),
				"up_right" => new Vector2(1f, 1f),
				"down_left" => new Vector2(-1f, -1f),
				"down_right" => new Vector2(1f, -1f),
				_ => throw new ArgumentException("direction must be neutral, up, down, left, right, up_left, up_right, or down_right")
			};
		}

		return ReadVector(parameters, "set_dpad");
	}
}
