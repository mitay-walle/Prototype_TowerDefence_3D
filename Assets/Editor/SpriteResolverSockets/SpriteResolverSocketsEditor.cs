using System;
using System.Collections.Generic;
using TD.Rendering;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace TD.Rendering.Editor
{
	[CustomEditor(typeof(SpriteResolverSockets))]
	public sealed class SpriteResolverSocketsEditor : UnityEditor.Editor
	{
		private const float ToolbarHeight = 24f;
		private const float SocketRowHeight = 128f;

		private SpriteResolverSockets _sockets;
		private SpriteSocketDatabase _database;
		private List<Sprite> _librarySprites = new List<Sprite>();
		private int _selectedSpriteIndex;
		private SpriteSocketRecord _selectedRecord;
		private SpriteSocketRecord _selectedDirectRecord;
		private SpriteSocketRecord _selectedMainRecord;
		private Vector2 _socketScrollPosition;
		private Rect _previewRect;
		private readonly Dictionary<Sprite, string> _spriteLabels =
			new Dictionary<Sprite, string>();
		private string _libraryCategory;
		private string _newSocketName = "Socket_New";
		private string _selectedSocketName;
		private bool _draggingSocket;
		private bool _changesPending;
		private bool _undoRecorded;

		private void OnEnable()
		{
			_sockets = (SpriteResolverSockets)target;
			Undo.undoRedoPerformed += OnUndoRedoPerformed;
			Refresh();
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= OnUndoRedoPerformed;
			SaveDatabase();
		}

		private void OnUndoRedoPerformed()
		{
			_changesPending = false;
			_database = _sockets == null ? null : _sockets.Database;
			LoadSelectedRecord();
			Repaint();
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			DrawDefaultInspector();
			serializedObject.ApplyModifiedProperties();

			if (_sockets == null)
			{
				return;
			}

			if (_database != _sockets.Database)
			{
				_database = _sockets.Database;
				Refresh();
			}

			EditorGUILayout.Space(4f);
			if (GUILayout.Button("Refresh Sprite Socket Preview", GUILayout.Height(26f)))
			{
				Refresh();
			}

			EditorGUILayout.HelpBox(
				"Редактирование выполняется в раскрытом Preview этого компонента. " +
				"Runtime автоматически находит дочерние объекты Socket_*.",
				MessageType.Info);
		}

		public override bool HasPreviewGUI()
		{
			return _sockets != null;
		}

		public override void OnPreviewGUI(Rect previewArea, GUIStyle background)
		{
			if (_sockets == null)
			{
				return;
			}

			if (_database != _sockets.Database)
			{
				_database = _sockets.Database;
				Refresh();
			}

			EditorGUI.DrawRect(previewArea, new Color(0.11f, 0.11f, 0.11f));
			Rect toolbarRect = new Rect(
				previewArea.x,
				previewArea.y,
				previewArea.width,
				ToolbarHeight);
			DrawPreviewToolbar(toolbarRect);

			Rect bodyRect = new Rect(
				previewArea.x,
				previewArea.y + ToolbarHeight,
				previewArea.width,
				Mathf.Max(0f, previewArea.height - ToolbarHeight));

			if (_database == null)
			{
				EditorGUI.HelpBox(
					new Rect(bodyRect.x + 8f, bodyRect.y + 8f, bodyRect.width - 16f, 42f),
					"Assign Sprite Socket Database in the component inspector.",
					MessageType.Warning);
				return;
			}

			if (_librarySprites.Count == 0)
			{
				EditorGUI.HelpBox(
					new Rect(bodyRect.x + 8f, bodyRect.y + 8f, bodyRect.width - 16f, 42f),
					"The SpriteResolver has no sprite variants to edit.",
					MessageType.Warning);
				return;
			}

			float panelWidth = Mathf.Clamp(bodyRect.width * 0.38f, 260f, 360f);
			Rect canvasBounds = new Rect(
				bodyRect.x,
				bodyRect.y,
				Mathf.Max(0f, bodyRect.width - panelWidth - 6f),
				bodyRect.height);
			Rect panelRect = new Rect(
				bodyRect.xMax - panelWidth,
				bodyRect.y,
				panelWidth,
				bodyRect.height);

			DrawCanvas(canvasBounds);
			DrawSocketPanel(panelRect);
		}

		[MenuItem("TD/Sprite Resolver Socket Editor", false, 20)]
		private static void SelectSelectedResolver()
		{
			SpriteResolverSockets sockets = GetSelectedSockets();
			if (sockets == null)
			{
				return;
			}

			Selection.activeObject = sockets;
			EditorGUIUtility.PingObject(sockets);
		}

		[MenuItem("TD/Sprite Resolver Socket Editor", true)]
		private static bool ValidateSelectSelectedResolver()
		{
			return GetSelectedSockets() != null;
		}

		[MenuItem("TD/Add Sprite Resolver Sockets", false, 10)]
		private static void AddToSelectedResolvers()
		{
			int addedCount = 0;
			int undoGroup = Undo.GetCurrentGroup();
			Undo.SetCurrentGroupName("Add Sprite Resolver Sockets");

			foreach (GameObject gameObject in Selection.gameObjects)
			{
				if (gameObject.GetComponent<SpriteResolver>() == null ||
					gameObject.GetComponent<SpriteResolverSockets>() != null)
				{
					continue;
				}

				Undo.AddComponent<SpriteResolverSockets>(gameObject);
				addedCount++;
			}

			Undo.CollapseUndoOperations(undoGroup);
			Debug.Log(string.Concat("[SpriteResolverSockets] Added components: ", addedCount));
		}

		[MenuItem("TD/Add Sprite Resolver Sockets", true)]
		private static bool ValidateAddToSelectedResolvers()
		{
			foreach (GameObject gameObject in Selection.gameObjects)
			{
				if (gameObject.GetComponent<SpriteResolver>() != null &&
					gameObject.GetComponent<SpriteResolverSockets>() == null)
				{
					return true;
				}
			}

			return false;
		}

		[MenuItem("TD/Create Sprite Socket Database", false, 11)]
		private static void CreateDatabase()
		{
			string path = EditorUtility.SaveFilePanelInProject(
				"Create Sprite Socket Database",
				"SpriteSocketDatabase",
				"asset",
				"Select the shared runtime database location.",
				"Assets/Resources");
			if (string.IsNullOrEmpty(path))
			{
				return;
			}

			var database = CreateInstance<SpriteSocketDatabase>();
			AssetDatabase.CreateAsset(database, path);
			AssetDatabase.SaveAssets();
			Selection.activeObject = database;
			EditorGUIUtility.PingObject(database);
			Debug.Log(
				string.Concat("[SpriteResolverSockets] Created database: ", path),
				database);
		}

		private void DrawPreviewToolbar(Rect rect)
		{
			float x = rect.x + 4f;
			Rect refreshRect = new Rect(x, rect.y + 2f, 54f, 20f);
			if (GUI.Button(refreshRect, "Refresh", EditorStyles.toolbarButton))
			{
				Refresh();
			}

			x = refreshRect.xMax + 2f;
			Rect saveRect = new Rect(x, rect.y + 2f, 42f, 20f);
			if (GUI.Button(saveRect, "Save", EditorStyles.toolbarButton))
			{
				SaveDatabase();
			}

			if (_librarySprites.Count == 0)
			{
				return;
			}

			float selectorWidth = Mathf.Min(260f, Mathf.Max(120f, rect.width - 110f));
			float selectorX = rect.xMax - selectorWidth - 4f;
			Rect previousRect = new Rect(
				selectorX,
				rect.y + 2f,
				24f,
				20f);
			if (GUI.Button(previousRect, "◀", EditorStyles.toolbarButton))
			{
				SelectSprite(-1);
			}

			Rect spriteNameRect = new Rect(
				previousRect.xMax + 2f,
				rect.y + 2f,
				selectorWidth - 52f,
				20f);
			GUI.Label(
				spriteNameRect,
				_librarySprites[_selectedSpriteIndex].name,
				EditorStyles.toolbarButton);

			Rect nextRect = new Rect(
				spriteNameRect.xMax + 2f,
				rect.y + 2f,
				24f,
				20f);
			if (GUI.Button(nextRect, "▶", EditorStyles.toolbarButton))
			{
				SelectSprite(1);
			}
		}

		private void DrawCanvas(Rect bounds)
		{
			if (bounds.width < 32f || bounds.height < 32f)
			{
				return;
			}

			float size = Mathf.Min(bounds.width - 16f, bounds.height - 30f);
			if (size <= 0f)
			{
				return;
			}

			_previewRect = new Rect(
				bounds.center.x - size * 0.5f,
				bounds.y + 22f,
				size,
				size);
			EditorGUI.DrawRect(_previewRect, new Color(0.16f, 0.16f, 0.16f));

			Sprite sprite = GetSelectedSprite();
			if (sprite == null || sprite.texture == null)
			{
				return;
			}

			Rect textureCoordinates = new Rect(
				sprite.rect.x / sprite.texture.width,
				sprite.rect.y / sprite.texture.height,
				sprite.rect.width / sprite.texture.width,
				sprite.rect.height / sprite.texture.height);
			GUI.DrawTextureWithTexCoords(
				_previewRect,
				sprite.texture,
				textureCoordinates,
				true);
			DrawPreviewGuides(sprite);
			DrawSocketPoints(sprite);
			HandlePreviewInput(sprite);

			GUI.Label(
				new Rect(bounds.x + 8f, bounds.y + 4f, bounds.width - 16f, 18f),
				string.Concat("Sprite: ", sprite.name, "   Sockets: ", GetSelectedSocketCount()),
				EditorStyles.miniLabel);
		}

		private void DrawSocketPanel(Rect rect)
		{
			GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
			float x = rect.x + 6f;
			float width = rect.width - 12f;
			float y = rect.y + 6f;

			EditorGUI.LabelField(
				new Rect(x, y, width, 18f),
				"Socket transforms",
				EditorStyles.boldLabel);
			y += 22f;
			y = DrawSocketSourceStatus(x, y, width);

			Rect nameRect = new Rect(x, y, width - 48f, 20f);
			_newSocketName = EditorGUI.TextField(nameRect, _newSocketName);
			if (GUI.Button(
				new Rect(nameRect.xMax + 4f, y, 44f, 20f),
				"Add",
				EditorStyles.miniButton))
			{
				AddSocket();
			}
			y += 24f;

			float actionWidth = (width - 4f) * 0.5f;
			if (GUI.Button(
				new Rect(x, y, actionWidth, 20f),
				"Refresh children",
				EditorStyles.miniButton))
			{
				RefreshFromChildren();
			}
			if (GUI.Button(
				new Rect(x + actionWidth + 4f, y, actionWidth, 20f),
				"Apply children",
				EditorStyles.miniButton))
			{
				ApplyToChildren();
			}
			y += 26f;

			if (_selectedRecord == null || _selectedRecord.sockets == null)
			{
				EditorGUI.HelpBox(
					new Rect(x, y, width, 42f),
					"Create a record or refresh it from Socket_* children.",
					MessageType.Info);
				return;
			}

			float scrollHeight = Mathf.Max(0f, rect.yMax - y - 6f);
			Rect scrollRect = new Rect(x, y, width, scrollHeight);
			float contentHeight = Mathf.Max(
				scrollHeight,
				_selectedRecord.sockets.Count * SocketRowHeight);
			Rect contentRect = new Rect(0f, 0f, width - 16f, contentHeight);
			_socketScrollPosition = GUI.BeginScrollView(
				scrollRect,
				_socketScrollPosition,
				contentRect);

			float rowY = 0f;
			for (int index = 0; index < _selectedRecord.sockets.Count; index++)
			{
				if (DrawSocketRow(new Rect(0f, rowY, contentRect.width, SocketRowHeight), index))
				{
					break;
				}

				rowY += SocketRowHeight + 4f;
			}

			GUI.EndScrollView();
		}

		private float DrawSocketSourceStatus(float x, float y, float width)
		{
			if (_selectedDirectRecord == null)
			{
				return y;
			}

			bool inherited = IsInheritedFromMain();
			string sourceText;
			if (_selectedDirectRecord.mainSprite == null)
			{
				sourceText = "Source: Main Library";
			}
			else if (inherited)
			{
				sourceText = string.Concat(
					"Inherited from Main Library: ",
					_selectedDirectRecord.mainSprite.name);
			}
			else
			{
				sourceText = string.Concat(
					"Local Override   Main: ",
					_selectedDirectRecord.mainSprite.name);
			}

			EditorGUI.HelpBox(
				new Rect(x, y, width, 34f),
				sourceText,
				MessageType.Info);
			y += 38f;

			if (_selectedDirectRecord.mainSprite != null && !inherited)
			{
				if (GUI.Button(
					new Rect(x, y, width, 20f),
					"Revert to Main Library",
					EditorStyles.miniButton))
				{
					RevertToMain();
				}

				y += 24f;
			}

			return y;
		}

		private bool DrawSocketRow(Rect rect, int index)
		{
			SpriteSocketTransform socket = _selectedRecord.sockets[index];
			if (socket == null)
			{
				return false;
			}

			GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
			Rect headerRect = new Rect(rect.x + 4f, rect.y + 4f, rect.width - 30f, 20f);
			bool selected = string.Equals(
				_selectedSocketName,
				socket.name,
				StringComparison.OrdinalIgnoreCase);
			if (GUI.Toggle(headerRect, selected, socket.name, EditorStyles.miniButton))
			{
				_selectedSocketName = socket.name;
			}

			if (GUI.Button(
				new Rect(rect.xMax - 25f, rect.y + 4f, 20f, 20f),
				"X",
				EditorStyles.miniButton))
			{
				string socketName = socket.name;
				EnsureLocalOverride();
				SpriteSocketTransform localSocket = FindSocket(socketName);
				Undo.RecordObject(_database, "Remove Sprite Socket");
				if (localSocket != null)
				{
					_selectedRecord.sockets.Remove(localSocket);
				}

				_selectedSocketName = null;
				MarkDatabaseChanged();
				return true;
			}

			float fieldY = rect.y + 28f;
			float fieldWidth = rect.width - 8f;
			EditorGUI.BeginChangeCheck();
			string name = EditorGUI.TextField(
				new Rect(rect.x + 4f, fieldY, fieldWidth, 18f),
				"Name",
				socket.name);
			fieldY += 20f;
			Vector3 position = DrawCompactVector3Field(
				new Rect(rect.x + 4f, fieldY, fieldWidth, 18f),
				"Position",
				socket.localPosition);
			fieldY += 20f;
			Vector3 rotation = DrawCompactVector3Field(
				new Rect(rect.x + 4f, fieldY, fieldWidth, 18f),
				"Rotation",
				socket.localEulerAngles);
			fieldY += 20f;
			Vector3 scale = DrawCompactVector3Field(
				new Rect(rect.x + 4f, fieldY, fieldWidth, 18f),
				"Scale",
				socket.localScale);
			fieldY += 20f;
			bool rotateWithSpriteParent = EditorGUI.ToggleLeft(
				new Rect(rect.x + 4f, fieldY, fieldWidth, 18f),
				"Rotate with sprite parent",
				socket.rotateWithSpriteParent);
			if (EditorGUI.EndChangeCheck())
			{
				string originalName = socket.name;
				EnsureLocalOverride();
				SpriteSocketTransform editedSocket = FindSocket(originalName);
				if (editedSocket == null)
				{
					return false;
				}

				Undo.RecordObject(_database, "Edit Sprite Socket");
				editedSocket.name = SpriteResolverSockets.NormalizeSocketName(name);
				editedSocket.localPosition = position;
				editedSocket.localEulerAngles = rotation;
				editedSocket.localScale = scale;
				editedSocket.rotateWithSpriteParent = rotateWithSpriteParent;
				_selectedSocketName = editedSocket.name;
				MarkDatabaseChanged();
			}

			return false;
		}

		private static Vector3 DrawCompactVector3Field(
			Rect rect,
			string label,
			Vector3 value)
		{
			const float labelWidth = 58f;
			const float axisLabelWidth = 10f;
			const float spacing = 2f;
			float cellWidth = Mathf.Max(
				20f,
				(rect.width - labelWidth - spacing * 2f) / 3f);
			float x = rect.x + labelWidth;

			EditorGUI.LabelField(
				new Rect(rect.x, rect.y, labelWidth - 2f, rect.height),
				label);
			value.x = DrawCompactFloatField(
				new Rect(x, rect.y, cellWidth, rect.height),
				"X",
				value.x,
				axisLabelWidth);
			x += cellWidth + spacing;
			value.y = DrawCompactFloatField(
				new Rect(x, rect.y, cellWidth, rect.height),
				"Y",
				value.y,
				axisLabelWidth);
			x += cellWidth + spacing;
			value.z = DrawCompactFloatField(
				new Rect(x, rect.y, cellWidth, rect.height),
				"Z",
				value.z,
				axisLabelWidth);
			return value;
		}

		private static float DrawCompactFloatField(
			Rect rect,
			string axis,
			float value,
			float axisLabelWidth)
		{
			EditorGUI.LabelField(
				new Rect(rect.x, rect.y, axisLabelWidth, rect.height),
				axis,
				EditorStyles.miniLabel);
			return EditorGUI.FloatField(
				new Rect(
					rect.x + axisLabelWidth,
					rect.y,
					rect.width - axisLabelWidth,
					rect.height),
				value);
		}

		private void DrawPreviewGuides(Sprite sprite)
		{
			float pivotX = _previewRect.x +
				sprite.pivot.x / sprite.rect.width * _previewRect.width;
			float pivotY = _previewRect.y +
				(_previewRect.height -
					sprite.pivot.y / sprite.rect.height * _previewRect.height);
			EditorGUI.DrawRect(
				new Rect(_previewRect.x, pivotY, _previewRect.width, 1f),
				new Color(1f, 1f, 1f, 0.25f));
			EditorGUI.DrawRect(
				new Rect(pivotX, _previewRect.y, 1f, _previewRect.height),
				new Color(1f, 1f, 1f, 0.25f));
		}

		private void DrawSocketPoints(Sprite sprite)
		{
			if (_selectedRecord == null || _selectedRecord.sockets == null)
			{
				return;
			}

			foreach (SpriteSocketTransform socket in _selectedRecord.sockets)
			{
				if (socket == null)
				{
					continue;
				}

				Vector2 point = LocalToPreview(sprite, socket.localPosition);
				bool selected = string.Equals(
					_selectedSocketName,
					socket.name,
					StringComparison.OrdinalIgnoreCase);
				float size = selected ? 10f : 7f;
				EditorGUI.DrawRect(
					new Rect(point.x - size * 0.5f, point.y - size * 0.5f, size, size),
					selected ? Color.yellow : Color.cyan);
				GUI.Label(
					new Rect(point.x + 7f, point.y - 10f, 150f, 20f),
					socket.name);
			}
		}

		private void HandlePreviewInput(Sprite sprite)
		{
			if (_selectedRecord == null || _selectedRecord.sockets == null)
			{
				return;
			}

			Event currentEvent = Event.current;
			if (currentEvent.type == EventType.MouseDown &&
				currentEvent.button == 0 &&
				_previewRect.Contains(currentEvent.mousePosition))
			{
				SpriteSocketTransform socket = FindSocketAt(sprite, currentEvent.mousePosition);
				if (socket != null)
				{
					_selectedSocketName = socket.name;
					_draggingSocket = true;
					_undoRecorded = false;
					GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
					currentEvent.Use();
				}
			}

			if (_draggingSocket &&
				(currentEvent.type == EventType.MouseDrag ||
					currentEvent.type == EventType.MouseMove))
			{
				SpriteSocketTransform socket = FindSocket(_selectedSocketName);
				if (socket != null)
				{
					if (!_undoRecorded)
					{
						EnsureLocalOverride();
						socket = FindSocket(_selectedSocketName);
						if (socket == null)
						{
							return;
						}

						Undo.RecordObject(_database, "Move Sprite Socket");
						_undoRecorded = true;
					}

					Vector3 localPosition = PreviewToLocal(sprite, currentEvent.mousePosition);
					localPosition.z = socket.localPosition.z;
					socket.localPosition = localPosition;
					MarkDatabaseChanged();
					Repaint();
					currentEvent.Use();
				}
			}

			if (_draggingSocket &&
				(currentEvent.type == EventType.MouseUp ||
					currentEvent.rawType == EventType.MouseUp))
			{
				_draggingSocket = false;
				_undoRecorded = false;
				GUIUtility.hotControl = 0;
				SaveDatabase();
				currentEvent.Use();
			}
		}

		private SpriteSocketTransform FindSocketAt(Sprite sprite, Vector2 point)
		{
			SpriteSocketTransform closestSocket = null;
			float closestDistance = 14f;
			foreach (SpriteSocketTransform socket in _selectedRecord.sockets)
			{
				if (socket == null)
				{
					continue;
				}

				Vector2 socketPoint = LocalToPreview(sprite, socket.localPosition);
				float distance = Vector2.Distance(point, socketPoint);
				if (distance < closestDistance)
				{
					closestSocket = socket;
					closestDistance = distance;
				}
			}

			return closestSocket;
		}

		private Vector2 LocalToPreview(Sprite sprite, Vector3 localPosition)
		{
			float pixelsPerUnit = Mathf.Max(sprite.pixelsPerUnit, 0.0001f);
			float pixelX = sprite.pivot.x + localPosition.x * pixelsPerUnit;
			float pixelY = sprite.pivot.y + localPosition.y * pixelsPerUnit;
			return new Vector2(
				_previewRect.x +
				pixelX / sprite.rect.width * _previewRect.width,
				_previewRect.y +
				_previewRect.height -
				pixelY / sprite.rect.height * _previewRect.height);
		}

		private Vector3 PreviewToLocal(Sprite sprite, Vector2 point)
		{
			float pixelsPerUnit = Mathf.Max(sprite.pixelsPerUnit, 0.0001f);
			float pixelX = Mathf.Clamp(
				(point.x - _previewRect.x) /
				_previewRect.width *
				sprite.rect.width,
				0f,
				sprite.rect.width);
			float pixelY = Mathf.Clamp(
				(_previewRect.yMax - point.y) /
				_previewRect.height *
				sprite.rect.height,
				0f,
				sprite.rect.height);
			return new Vector3(
				(pixelX - sprite.pivot.x) / pixelsPerUnit,
				(pixelY - sprite.pivot.y) / pixelsPerUnit,
				0f);
		}

		private void Refresh()
		{
			if (_sockets == null)
			{
				return;
			}

			_database = _sockets.Database;
			_librarySprites = GetLibrarySprites();
			EnsureMainLinks();
			if (_librarySprites.Count == 0)
			{
				_selectedRecord = null;
				Repaint();
				return;
			}

			_selectedSpriteIndex = Mathf.Clamp(
				_selectedSpriteIndex,
				0,
				_librarySprites.Count - 1);
			LoadSelectedRecord();
			Repaint();
		}

		private void LoadSelectedRecord()
		{
			_selectedRecord = null;
			_selectedDirectRecord = null;
			_selectedMainRecord = null;
			Sprite sprite = GetSelectedSprite();
			if (_database != null && sprite != null)
			{
				if (_database.TryGet(sprite, out _selectedDirectRecord))
				{
					if (_selectedDirectRecord.inheritMain &&
						_selectedDirectRecord.mainSprite != null &&
						_database.TryGet(
							_selectedDirectRecord.mainSprite,
							out _selectedMainRecord))
					{
						_selectedRecord = _selectedMainRecord;
					}
					else
					{
						_selectedRecord = _selectedDirectRecord;
					}
				}
			}

			_selectedSocketName = null;
		}

		private void SelectSprite(int direction)
		{
			if (_librarySprites.Count == 0)
			{
				return;
			}

			_selectedSpriteIndex =
				(_selectedSpriteIndex + direction + _librarySprites.Count) %
				_librarySprites.Count;
			LoadSelectedRecord();
			Repaint();
		}

		private void RefreshFromChildren()
		{
			Sprite sprite = GetSelectedSprite();
			if (_database == null || sprite == null)
			{
				return;
			}

			Undo.RecordObject(_database, "Refresh Sprite Sockets From Children");
			_selectedRecord = _database.GetOrCreate(sprite);
			_selectedDirectRecord = _selectedRecord;
			_selectedMainRecord = null;
			_selectedRecord.inheritMain = false;
			if (_selectedRecord.sockets == null)
			{
				_selectedRecord.sockets = new List<SpriteSocketTransform>();
			}

			foreach (Transform child in GetSocketChildren())
			{
				string socketName = SpriteResolverSockets.NormalizeSocketName(child.name);
				SpriteSocketTransform socket = FindSocket(socketName);
				if (socket == null)
				{
					socket = new SpriteSocketTransform
					{
						name = socketName
					};
					_selectedRecord.sockets.Add(socket);
				}

				socket.localPosition = child.localPosition;
				socket.localEulerAngles = GetSocketRotation(child, socket.rotateWithSpriteParent);
				socket.localScale = child.localScale;
			}

			MarkDatabaseChanged();
			SaveDatabase();
			Repaint();
		}

		private void ApplyToChildren()
		{
			if (_selectedRecord == null ||
				_selectedRecord.sockets == null ||
				_sockets == null)
			{
				return;
			}

			int appliedCount = 0;
			foreach (SpriteSocketTransform socketData in _selectedRecord.sockets)
			{
				if (socketData == null ||
					!_sockets.TryGetSocket(socketData.name, out Transform socket))
				{
					continue;
				}

				Undo.RecordObject(socket, "Apply Sprite Sockets To Children");
				socketData.ApplyTo(socket, GetSpriteParent());
				PrefabUtility.RecordPrefabInstancePropertyModifications(socket);
				EditorUtility.SetDirty(socket);
				appliedCount++;
			}

			Sprite selectedSprite = GetSelectedSprite();
			Debug.Log(
				string.Concat(
					"[SpriteResolverSockets] Applied ",
					appliedCount,
					" sockets for ",
					selectedSprite == null ? "current sprite" : selectedSprite.name,
					"."),
				_sockets);
			Repaint();
		}

		private void AddSocket()
		{
			Sprite sprite = GetSelectedSprite();
			if (_database == null || sprite == null)
			{
				return;
			}

			string socketName = SpriteResolverSockets.NormalizeSocketName(_newSocketName);
			if (string.IsNullOrEmpty(socketName))
			{
				return;
			}

			EnsureLocalOverride();
			Undo.RecordObject(_database, "Add Sprite Socket");
			_selectedRecord = _selectedDirectRecord ?? _database.GetOrCreate(sprite);
			_selectedDirectRecord = _selectedRecord;
			_selectedMainRecord = null;
			if (_selectedRecord.sockets == null)
			{
				_selectedRecord.sockets = new List<SpriteSocketTransform>();
			}

			if (FindSocket(socketName) == null)
			{
				_selectedRecord.sockets.Add(new SpriteSocketTransform
				{
					name = socketName
				});
				_selectedSocketName = socketName;
				MarkDatabaseChanged();
				SaveDatabase();
			}
		}

		private void EnsureLocalOverride()
		{
			if (_database == null)
			{
				return;
			}

			if (_selectedDirectRecord == null)
			{
				Sprite sprite = GetSelectedSprite();
				if (sprite == null)
				{
					return;
				}

				Undo.RecordObject(_database, "Create Sprite Socket Override");
				_selectedDirectRecord = _database.GetOrCreate(sprite);
				_selectedDirectRecord.inheritMain = false;
				_selectedRecord = _selectedDirectRecord;
				_selectedMainRecord = null;
				MarkDatabaseChanged();
				return;
			}

			if (!IsInheritedFromMain())
			{
				_selectedRecord = _selectedDirectRecord;
				return;
			}

			Undo.RecordObject(_database, "Create Sprite Socket Override");
			_selectedDirectRecord.sockets = CloneSockets(_selectedMainRecord.sockets);
			_selectedDirectRecord.inheritMain = false;
			_selectedRecord = _selectedDirectRecord;
			_selectedMainRecord = null;
			MarkDatabaseChanged();
		}

		private void RevertToMain()
		{
			if (_database == null ||
				_selectedDirectRecord == null ||
				_selectedDirectRecord.mainSprite == null ||
				IsInheritedFromMain())
			{
				return;
			}

			Undo.RecordObject(_database, "Revert Sprite Sockets To Main");
			_selectedDirectRecord.sockets = new List<SpriteSocketTransform>();
			_selectedDirectRecord.inheritMain = true;
			MarkDatabaseChanged();
			SaveDatabase();
			LoadSelectedRecord();
			Repaint();
		}

		private bool IsInheritedFromMain()
		{
			return _selectedDirectRecord != null &&
				_selectedDirectRecord.inheritMain &&
				_selectedMainRecord != null &&
				_selectedRecord == _selectedMainRecord;
		}

		private static List<SpriteSocketTransform> CloneSockets(
			List<SpriteSocketTransform> sourceSockets)
		{
			var sockets = new List<SpriteSocketTransform>();
			if (sourceSockets == null)
			{
				return sockets;
			}

			foreach (SpriteSocketTransform source in sourceSockets)
			{
				if (source == null)
				{
					continue;
				}

				sockets.Add(new SpriteSocketTransform
				{
					name = source.name,
					localPosition = source.localPosition,
					localEulerAngles = source.localEulerAngles,
					localScale = source.localScale,
					rotateWithSpriteParent = source.rotateWithSpriteParent
				});
			}

			return sockets;
		}

		private SpriteSocketTransform FindSocket(string socketName)
		{
			if (_selectedRecord == null || _selectedRecord.sockets == null)
			{
				return null;
			}

			foreach (SpriteSocketTransform socket in _selectedRecord.sockets)
			{
				if (socket != null &&
					string.Equals(
						SpriteResolverSockets.NormalizeSocketName(socket.name),
						SpriteResolverSockets.NormalizeSocketName(socketName),
						StringComparison.OrdinalIgnoreCase))
				{
					return socket;
				}
			}

			return null;
		}

		private List<Transform> GetSocketChildren()
		{
			var children = new List<Transform>();
			var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (_sockets == null)
			{
				return children;
			}

			foreach (Transform child in _sockets.GetComponentsInChildren<Transform>(true))
			{
				if (child == _sockets.transform ||
					!SpriteResolverSockets.IsSocketName(child.name))
				{
					continue;
				}

				string socketName = SpriteResolverSockets.NormalizeSocketName(child.name);
				if (names.Add(socketName))
				{
					children.Add(child);
				}
			}

			return children;
		}

		private List<Sprite> GetLibrarySprites()
		{
			var sprites = new List<Sprite>();
			_spriteLabels.Clear();
			_libraryCategory = null;
			if (_sockets == null)
			{
				return sprites;
			}

			SpriteRenderer renderer = _sockets.GetComponent<SpriteRenderer>();
			AddSprite(sprites, renderer == null ? null : renderer.sprite);

			SpriteResolver resolver = _sockets.GetComponent<SpriteResolver>();
			SpriteLibrary spriteLibrary = resolver == null ? null : resolver.spriteLibrary;
			SpriteLibraryAsset libraryAsset = spriteLibrary == null
				? null
				: spriteLibrary.spriteLibraryAsset;
			if (resolver == null || libraryAsset == null)
			{
				return sprites;
			}

			_libraryCategory = resolver.GetCategory();
			foreach (string label in libraryAsset.GetCategoryLabelNames(_libraryCategory))
			{
				Sprite sprite = spriteLibrary.GetSprite(_libraryCategory, label);
				AddSprite(sprites, sprite);
				if (sprite != null)
				{
					_spriteLabels[sprite] = label;
				}
			}

			return sprites;
		}

		private Sprite GetSelectedSprite()
		{
			return _selectedSpriteIndex >= 0 &&
				_selectedSpriteIndex < _librarySprites.Count
				? _librarySprites[_selectedSpriteIndex]
				: null;
		}

		private void SaveDatabase()
		{
			if (!_changesPending || _database == null)
			{
				return;
			}

			EditorUtility.SetDirty(_database);
			AssetDatabase.SaveAssets();
			_changesPending = false;
		}

		private void MarkDatabaseChanged()
		{
			_changesPending = true;
			EditorUtility.SetDirty(_database);
		}

		private Transform GetSpriteParent()
		{
			if (_sockets == null)
			{
				return null;
			}

			return _sockets.transform.parent == null
				? _sockets.transform
				: _sockets.transform.parent;
		}

		private Vector3 GetSocketRotation(Transform socket, bool rotateWithSpriteParent)
		{
			if (!rotateWithSpriteParent)
			{
				return socket.localEulerAngles;
			}

			Transform spriteParent = GetSpriteParent();
			return spriteParent == null
				? socket.localEulerAngles
				: (Quaternion.Inverse(spriteParent.rotation) * socket.rotation)
					.eulerAngles;
		}

		private static SpriteResolverSockets GetSelectedSockets()
		{
			GameObject selectedObject = Selection.activeGameObject;
			return selectedObject == null
				? null
				: selectedObject.GetComponent<SpriteResolverSockets>();
		}

		private int GetSelectedSocketCount()
		{
			return _selectedRecord == null || _selectedRecord.sockets == null
				? 0
				: _selectedRecord.sockets.Count;
		}

		private static void AddSprite(List<Sprite> sprites, Sprite sprite)
		{
			if (sprite != null && !sprites.Contains(sprite))
			{
				sprites.Add(sprite);
			}
		}

		private bool TryGetMainLibrarySprite(Sprite sprite, out Sprite mainLibrarySprite)
		{
			mainLibrarySprite = null;
			if (sprite == null ||
				string.IsNullOrEmpty(_libraryCategory) ||
				!_spriteLabels.TryGetValue(sprite, out string label))
			{
				return false;
			}

			SpriteResolver resolver = _sockets == null
				? null
				: _sockets.GetComponent<SpriteResolver>();
			SpriteLibrary spriteLibrary = resolver == null
				? null
				: resolver.spriteLibrary;
			SpriteLibraryAsset libraryAsset = spriteLibrary == null
				? null
				: spriteLibrary.spriteLibraryAsset;
			SpriteLibraryAsset mainLibrary = FindMainLibrary(libraryAsset);
			if (mainLibrary == null)
			{
				return false;
			}

			mainLibrarySprite = mainLibrary.GetSprite(_libraryCategory, label);
			return mainLibrarySprite != null && mainLibrarySprite != sprite;
		}

		private void EnsureMainLinks()
		{
			if (_database == null)
			{
				return;
			}

			bool undoRecorded = false;
			foreach (Sprite sprite in _librarySprites)
			{
				if (!TryGetMainLibrarySprite(sprite, out Sprite mainSprite) ||
					!_database.TryGet(mainSprite, out _))
				{
					continue;
				}

				if (!_database.TryGet(sprite, out SpriteSocketRecord record))
				{
					if (!undoRecorded)
					{
						Undo.RecordObject(_database, "Link Sprite Sockets To Main Library");
						undoRecorded = true;
					}

					record = _database.GetOrCreate(sprite);
				}

				if (record.mainSprite == mainSprite)
				{
					continue;
				}

				if (!undoRecorded)
				{
					Undo.RecordObject(_database, "Link Sprite Sockets To Main Library");
					undoRecorded = true;
				}

				record.mainSprite = mainSprite;
				record.inheritMain = true;
			}

			if (undoRecorded)
			{
				MarkDatabaseChanged();
				SaveDatabase();
			}
		}

		private static SpriteLibraryAsset FindMainLibrary(SpriteLibraryAsset libraryAsset)
		{
			if (libraryAsset == null)
			{
				return null;
			}

			string assetPath = AssetDatabase.GetAssetPath(libraryAsset);
			if (string.IsNullOrEmpty(assetPath))
			{
				return null;
			}

			UnityEngine.Object[] sourceObjects =
				InternalEditorUtility.LoadSerializedFileAndForget(assetPath);
			if (sourceObjects == null)
			{
				return null;
			}

			foreach (UnityEngine.Object sourceObject in sourceObjects)
			{
				if (sourceObject == null)
				{
					continue;
				}

				var serializedSource = new SerializedObject(sourceObject);
				SerializedProperty mainGuidProperty =
					serializedSource.FindProperty("m_PrimaryLibraryGUID");
				if (mainGuidProperty == null ||
					string.IsNullOrEmpty(mainGuidProperty.stringValue))
				{
					continue;
				}

				string mainPath =
					AssetDatabase.GUIDToAssetPath(mainGuidProperty.stringValue);
				return string.IsNullOrEmpty(mainPath)
					? null
					: AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(mainPath);
			}

			return null;
		}

	}
}
