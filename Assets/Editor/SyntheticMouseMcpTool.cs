using System;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using TD.Interactions;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

[McpForUnityTool("td_virtual_mouse")]
public class SyntheticMouseMcpTool
{
	public class Parameters
	{
		[ToolParameter("Operation: status, move, button_down, button_up, click, scroll, or reset.")]
		public string operation { get; set; }

		[ToolParameter("Input source. Must be synthetic.", Required = false, DefaultValue = "synthetic")]
		public string mouse { get; set; }

		[ToolParameter("Screen-space X coordinate.", Required = false)]
		public float? x { get; set; }

		[ToolParameter("Screen-space Y coordinate.", Required = false)]
		public float? y { get; set; }

		[ToolParameter("Mouse button: left, right, middle, forward, or back.", Required = false)]
		public string button { get; set; }

		[ToolParameter("Horizontal scroll delta.", Required = false)]
		public float? delta_x { get; set; }

		[ToolParameter("Vertical scroll delta.", Required = false)]
		public float? delta_y { get; set; }
	}

	public static object HandleCommand(JObject parameters)
	{
		var mouseSource = ReadMouseSource(parameters);
		if (!Application.isPlaying)
		{
			return new { success = false, error = "Gameplay must be running in Play Mode.", mouse = mouseSource };
		}

		var mouse = UnityEngine.Object.FindFirstObjectByType<SyntheticMouse>();
		if (mouse == null)
		{
			return new { success = false, error = "SyntheticMouse is not authored in the active scene.", mouse = mouseSource };
		}

		var operation = parameters.Value<string>("operation")?.ToLowerInvariant();
		var actualPosition = Vector2.zero;
		mouse.ExecuteOnUnityThread(() =>
		{
			switch (operation)
			{
				case "status":
					break;
				case "move":
					mouse.Move(ReadPosition(parameters));
					break;
				case "button_down":
					MoveIfProvided(mouse, parameters);
					mouse.Press(ReadButton(parameters));
					break;
				case "button_up":
					mouse.Release(ReadButton(parameters));
					break;
				case "click":
					MoveIfProvided(mouse, parameters);
					mouse.Click(ReadButton(parameters));
					break;
				case "scroll":
					mouse.Scroll(ReadScroll(parameters));
					break;
				case "reset":
					mouse.Reset();
					break;
				default:
					throw new ArgumentException("operation must be one of: status, move, button_down, button_up, click, scroll, reset");
			}

			UnityEngine.InputSystem.InputSystem.Update();
			mouse.ApplyCurrentState();
			actualPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
		});

		return CreateStatus(operation, mouseSource, mouse, actualPosition);
	}

	private static string ReadMouseSource(JObject parameters)
	{
		var mouseSource = parameters.Value<string>("mouse")?.ToLowerInvariant() ?? "synthetic";
		if (mouseSource != "synthetic")
		{
			throw new ArgumentException("mouse must be 'synthetic'; use the regular input operation when hardware input is desired");
		}

		return mouseSource;
	}

	private static object CreateStatus(string operation, string mouseSource, SyntheticMouse mouse, Vector2 actualPosition)
	{
		return new
		{
			success = true,
			operation,
			mouse = mouseSource,
			device_added = mouse.IsReady,
			position = new { x = mouse.Position.x, y = mouse.Position.y },
			actual_position = new { x = actualPosition.x, y = actualPosition.y },
			buttons = new
			{
				left = mouse.IsButtonPressed(MouseButton.Left),
				right = mouse.IsButtonPressed(MouseButton.Right),
				middle = mouse.IsButtonPressed(MouseButton.Middle)
			}
		};
	}

	private static void MoveIfProvided(SyntheticMouse mouse, JObject parameters)
	{
		if (parameters.ContainsKey("x") || parameters.ContainsKey("y"))
		{
			mouse.Move(ReadPosition(parameters));
		}
	}

	private static Vector2 ReadPosition(JObject parameters)
	{
		var x = parameters["x"]?.Value<float?>();
		var y = parameters["y"]?.Value<float?>();
		if (!x.HasValue || !y.HasValue)
		{
			throw new ArgumentException("move/click coordinates require numeric x and y parameters");
		}
		return new Vector2(x.Value, y.Value);
	}

	private static Vector2 ReadScroll(JObject parameters)
	{
		var x = parameters["delta_x"]?.Value<float?>() ?? 0f;
		var y = parameters["delta_y"]?.Value<float?>() ?? 0f;
		if (x == 0f && y == 0f)
		{
			throw new ArgumentException("scroll requires a non-zero delta_x or delta_y parameter");
		}
		return new Vector2(x, y);
	}

	private static MouseButton ReadButton(JObject parameters)
	{
		var value = parameters.Value<string>("button")?.ToLowerInvariant();
		return value switch
		{
			"left" => MouseButton.Left,
			"right" => MouseButton.Right,
			"middle" => MouseButton.Middle,
			"forward" => MouseButton.Forward,
			"back" => MouseButton.Back,
			_ => throw new ArgumentException("button must be one of: left, right, middle, forward, back")
		};
	}
}
