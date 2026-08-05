using System.Collections.Generic;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using TD.GameLoop;
using TD.Interactions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

[McpForUnityTool("td_gameplay_telemetry")]
public class GameplayTelemetryMcpTool
{
	public class Parameters
	{
		[ToolParameter("Operation: status, snapshot, events, or clear.")]
		public string operation { get; set; }

		[ToolParameter("Return events after this sequence number.", Required = false)]
		public int? after_sequence { get; set; }

		[ToolParameter("Maximum number of returned events.", Required = false, DefaultValue = "100")]
		public int? max_events { get; set; }
	}

	public static object HandleCommand(JObject parameters)
	{
		if (!Application.isPlaying)
			return new { success = false, error = "Gameplay must be running in Play Mode." };

		var telemetry = Object.FindFirstObjectByType<GameplayTelemetry>(FindObjectsInactive.Include);
		if (telemetry == null)
			return new { success = false, error = "GameplayTelemetry is not authored in the active scene." };

		var operation = parameters.Value<string>("operation")?.ToLowerInvariant() ?? "status";
		if (operation == "clear")
			telemetry.ClearEvents();

		if (operation != "status" && operation != "snapshot" && operation != "events" && operation != "clear")
			throw new System.ArgumentException("operation must be one of: status, snapshot, events, clear");

		var afterSequence = parameters["after_sequence"]?.Value<int?>() ?? 0;
		var maxEvents = Mathf.Clamp(parameters["max_events"]?.Value<int?>() ?? 100, 1, 500);
		var snapshot = telemetry.CaptureSnapshot();
		var response = new
		{
			success = true,
			operation,
			first_sequence = telemetry.FirstSequence,
			last_sequence = telemetry.LastSequence,
			snapshot,
			input = CreateInputSnapshot(),
			pointer = CreatePointerSnapshot(),
			events = operation == "snapshot" ? new List<GameplayTelemetryEvent>() : telemetry.GetEventsSince(afterSequence, maxEvents)
		};

		return response;
	}

	private static object CreateInputSnapshot()
	{
		var mouse = Object.FindFirstObjectByType<SyntheticMouse>(FindObjectsInactive.Include);
		var currentMouse = Mouse.current;
		var position = mouse != null ? mouse.Position : currentMouse != null ? currentMouse.position.ReadValue() : Vector2.zero;
		return new
		{
			device = currentMouse != null ? currentMouse.name : string.Empty,
			position = new { x = position.x, y = position.y },
			buttons = new
			{
				left = mouse != null ? mouse.IsButtonPressed(MouseButton.Left) : currentMouse != null && currentMouse.leftButton.isPressed,
				right = mouse != null ? mouse.IsButtonPressed(MouseButton.Right) : currentMouse != null && currentMouse.rightButton.isPressed,
				middle = mouse != null ? mouse.IsButtonPressed(MouseButton.Middle) : currentMouse != null && currentMouse.middleButton.isPressed
			}
		};
	}

	private static object CreatePointerSnapshot()
	{
		var input = CreateInputSnapshot();
		var mouse = Object.FindFirstObjectByType<SyntheticMouse>(FindObjectsInactive.Include);
		var currentMouse = Mouse.current;
		var position = mouse != null ? mouse.Position : currentMouse != null ? currentMouse.position.ReadValue() : Vector2.zero;
		var eventSystem = EventSystem.current;
		var pointerData = eventSystem == null ? null : new PointerEventData(eventSystem) { position = position };
		var raycastResults = new List<RaycastResult>();
		if (pointerData != null)
			eventSystem.RaycastAll(pointerData, raycastResults);

		var topUiObject = raycastResults.Count > 0 ? raycastResults[0].gameObject : null;
		var camera = Camera.main;
		var ray = camera != null ? camera.ScreenPointToRay(position) : default;
		var physicsHit = Physics.Raycast(ray, out var hit, 1000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
		var targetable = physicsHit ? hit.collider.GetComponentInParent<ITargetable>() as MonoBehaviour : null;
		var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
		var button = topUiObject != null ? topUiObject.GetComponentInParent<Button>() : null;

		return new
		{
			position = new { x = position.x, y = position.y },
			eventSystem = new
			{
				currentSelectedObject = selected != null ? selected.name : string.Empty,
				currentSelectedPath = GetPath(selected),
				pointerOverGameObject = eventSystem != null && eventSystem.IsPointerOverGameObject(),
				pointerUiTarget = topUiObject != null ? topUiObject.name : string.Empty,
				pointerUiTargetPath = GetPath(topUiObject)
			},
			activeCamera = camera == null ? null : new
			{
				position = new { x = camera.transform.position.x, y = camera.transform.position.y, z = camera.transform.position.z },
				rotation = new { x = camera.transform.eulerAngles.x, y = camera.transform.eulerAngles.y, z = camera.transform.eulerAngles.z }
			},
			screenRay = camera == null ? null : new
			{
				origin = new { x = ray.origin.x, y = ray.origin.y, z = ray.origin.z },
				direction = new { x = ray.direction.x, y = ray.direction.y, z = ray.direction.z }
			},
			physicsRaycast = new
			{
				hit = physicsHit,
				objectName = physicsHit ? hit.collider.name : string.Empty,
				objectPath = physicsHit ? GetPath(hit.collider.gameObject) : string.Empty,
				point = physicsHit ? new { x = hit.point.x, y = hit.point.y, z = hit.point.z } : null
			},
			selectedGameplayObject = targetable == null ? null : new { name = targetable.name, path = GetPath(targetable.gameObject) },
			uiTarget = topUiObject == null ? null : new
			{
				name = topUiObject.name,
				path = GetPath(topUiObject),
				active = topUiObject.activeInHierarchy,
				interactable = button == null || button.interactable
			},
			transport = input
		};
	}

	private static string GetPath(GameObject gameObject)
	{
		if (gameObject == null)
			return string.Empty;

		var path = gameObject.name;
		var parent = gameObject.transform.parent;
		while (parent != null)
		{
			path = parent.name + "/" + path;
			parent = parent.parent;
		}

		return path;
	}
}
