using System;
using System.Collections.Generic;
using TD.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace TD.Rendering.Editor
{
	public sealed class SpriteResolverSocketsWindow : EditorWindow
	{
		private SpriteResolverSockets _sockets;
		private SpriteSocketDatabase _database;
		private List<Sprite> _librarySprites = new List<Sprite>();
		private int _selectedSpriteIndex;
		private SpriteSocketRecord _selectedRecord;
		private Vector2 _socketScrollPosition;
		private Rect _previewRect;
		private string _newSocketName = "Socket_New";
		private string _selectedSocketName;
		private bool _draggingSocket;
		private bool _changesPending;
		private bool _undoRecorded;

		[MenuItem("TD/Sprite Resolver Socket Editor", false, 20)]
		private static void OpenSelected()
		{
			SpriteResolverSockets sockets = GetSelectedSockets();
			if (sockets == null)
			{
				return;
			}

			Open(sockets);
		}

		[MenuItem("TD/Sprite Resolver Socket Editor", true)]
		private static bool ValidateOpenSelected()
		{
			return GetSelectedSockets() != null;
		}

		public static void Open(SpriteResolverSockets sockets)
		{
			var window = GetWindow<SpriteResolverSocketsWindow>("Sprite Sockets");
			window.minSize = new Vector2(760f, 560f);
			window._sockets = sockets;
			window._database = sockets == null ? null : sockets.Database;
			window.Refresh();
			window.Show();
		}

		private void OnEnable()
		{
			Refresh();
		}

		private void OnDisable()
		{
			SaveDatabase();
		}

		private void OnGUI()
		{
			DrawToolbar();
			if (_sockets == null)
			{
				EditorGUILayout.HelpBox(
					"Select a GameObject with SpriteResolverSockets or assign it above.",
					MessageType.Info);
				return;
			}

			DrawTargetFields();
			if (_database == null)
			{
				EditorGUILayout.HelpBox(
					"Assign the shared SpriteSocketDatabase used by runtime.",
					MessageType.Warning);
				if (GUILayout.Button("Create shared database"))
				{
					CreateDatabase();
				}
				return;
			}

			if (_librarySprites.Count == 0)
			{
				EditorGUILayout.HelpBox(
					"The resolver has no sprite variants to edit.",
					MessageType.Warning);
				return;
			}

			DrawSpriteSelector();
			EditorGUILayout.Space(4f);
			EditorGUILayout.BeginHorizontal();
			DrawPreviewPanel();
			DrawSocketPanel();
			EditorGUILayout.EndHorizontal();
		}

		private void DrawToolbar()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
			{
				Refresh();
			}

			if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60f)))
			{
				SaveDatabase();
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.LabelField(
				_database == null ? "No database" : _database.name,
				EditorStyles.toolbarButton,
				GUILayout.Width(180f));
			EditorGUILayout.EndHorizontal();
		}

		private void DrawTargetFields()
		{
			EditorGUI.BeginChangeCheck();
			SpriteResolverSockets sockets = (SpriteResolverSockets)EditorGUILayout.ObjectField(
				"Sprite Resolver",
				_sockets,
				typeof(SpriteResolverSockets),
				true);
			if (EditorGUI.EndChangeCheck())
			{
				_sockets = sockets;
				_database = _sockets == null ? null : _sockets.Database;
				Refresh();
			}

			EditorGUI.BeginChangeCheck();
			SpriteSocketDatabase database = (SpriteSocketDatabase)EditorGUILayout.ObjectField(
				"Runtime database",
				_database,
				typeof(SpriteSocketDatabase),
				false);
			if (EditorGUI.EndChangeCheck())
			{
				AssignDatabase(database);
			}
		}

		private void DrawSpriteSelector()
		{
			var spriteNames = new string[_librarySprites.Count];
			for (int index = 0; index < _librarySprites.Count; index++)
			{
				spriteNames[index] = _librarySprites[index].name;
			}

			EditorGUI.BeginChangeCheck();
			int selectedIndex = EditorGUILayout.Popup("Sprite variant", _selectedSpriteIndex, spriteNames);
			if (EditorGUI.EndChangeCheck())
			{
				_selectedSpriteIndex = selectedIndex;
				LoadSelectedRecord();
			}
		}

		private void DrawPreviewPanel()
		{
			EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.56f));
			EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

			float previewSize = Mathf.Clamp(
				Mathf.Min(position.width * 0.52f, position.height - 190f),
				280f,
				560f);
			_previewRect = GUILayoutUtility.GetRect(
				previewSize,
				previewSize,
				GUILayout.ExpandWidth(false),
				GUILayout.ExpandHeight(false));

			EditorGUI.DrawRect(_previewRect, new Color(0.12f, 0.12f, 0.12f));
			Sprite sprite = GetSelectedSprite();
			if (sprite != null && sprite.texture != null)
			{
				Rect textureCoordinates = new Rect(
					sprite.rect.x / sprite.texture.width,
					sprite.rect.y / sprite.texture.height,
					sprite.rect.width / sprite.texture.width,
					sprite.rect.height / sprite.texture.height);
				GUI.DrawTextureWithTexCoords(_previewRect, sprite.texture, textureCoordinates, true);
				DrawPreviewGuides(sprite);
				DrawSocketPoints(sprite);
				HandlePreviewInput(sprite);
			}

			EditorGUILayout.LabelField(
				_selectedRecord == null
					? "No record. Use Refresh from child objects."
					: string.Concat("Sockets: ", GetSelectedSocketCount()),
				EditorStyles.miniLabel);
			EditorGUILayout.EndVertical();
		}

		private void DrawSocketPanel()
		{
			EditorGUILayout.BeginVertical(GUILayout.MinWidth(300f));
			EditorGUILayout.LabelField("Sockets", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			_newSocketName = EditorGUILayout.TextField(_newSocketName);
			if (GUILayout.Button("Add", GUILayout.Width(48f)))
			{
				AddSocket();
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Refresh from child objects"))
			{
				RefreshFromChildren();
			}
			if (GUILayout.Button("Apply to child objects"))
			{
				ApplyToChildren();
			}
			EditorGUILayout.EndHorizontal();

			if (_selectedRecord == null || _selectedRecord.sockets == null)
			{
				EditorGUILayout.HelpBox(
					"Create a record or refresh it from Socket_* children.",
					MessageType.Info);
				EditorGUILayout.EndVertical();
				return;
			}

			_socketScrollPosition = EditorGUILayout.BeginScrollView(_socketScrollPosition);
			for (int index = 0; index < _selectedRecord.sockets.Count; index++)
			{
				if (DrawSocketRow(index))
				{
					break;
				}
			}
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
		}

		private bool DrawSocketRow(int index)
		{
			SpriteSocketTransform socket = _selectedRecord.sockets[index];
			if (socket == null)
			{
				return false;
			}

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.BeginHorizontal();
			bool selected = string.Equals(
				_selectedSocketName,
				socket.name,
				StringComparison.OrdinalIgnoreCase);
			if (GUILayout.Toggle(selected, socket.name, "Button"))
			{
				_selectedSocketName = socket.name;
			}
			if (GUILayout.Button("X", GUILayout.Width(24f)))
			{
				Undo.RecordObject(_database, "Remove Sprite Socket");
				_selectedRecord.sockets.RemoveAt(index);
				_selectedSocketName = null;
				MarkDatabaseChanged();
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
				return true;
			}
			EditorGUILayout.EndHorizontal();

			EditorGUI.BeginChangeCheck();
			string name = EditorGUILayout.TextField("Name", socket.name);
			Vector3 position = EditorGUILayout.Vector3Field("Position", socket.localPosition);
			Vector3 rotation = EditorGUILayout.Vector3Field("Rotation", socket.localEulerAngles);
			Vector3 scale = EditorGUILayout.Vector3Field("Scale", socket.localScale);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(_database, "Edit Sprite Socket");
				socket.name = SpriteResolverSockets.NormalizeSocketName(name);
				socket.localPosition = position;
				socket.localEulerAngles = rotation;
				socket.localScale = scale;
				MarkDatabaseChanged();
			}

			EditorGUILayout.EndVertical();
			return false;
		}

		private void DrawPreviewGuides(Sprite sprite)
		{
			float pivotX = _previewRect.x + sprite.pivot.x / sprite.rect.width * _previewRect.width;
			float pivotY = _previewRect.y +
				(_previewRect.height - sprite.pivot.y / sprite.rect.height * _previewRect.height);
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
					currentEvent.Use();
				}
			}

			if (_draggingSocket &&
				(currentEvent.type == EventType.MouseDrag || currentEvent.type == EventType.MouseMove))
			{
				SpriteSocketTransform socket = FindSocket(_selectedSocketName);
				if (socket != null)
				{
					if (!_undoRecorded)
					{
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
				(currentEvent.type == EventType.MouseUp || currentEvent.rawType == EventType.MouseUp))
			{
				_draggingSocket = false;
				_undoRecorded = false;
				SaveDatabase();
				currentEvent.Use();
			}
		}

		private SpriteSocketTransform FindSocketAt(Sprite sprite, Vector2 point)
		{
			if (_selectedRecord == null || _selectedRecord.sockets == null)
			{
				return null;
			}

			SpriteSocketTransform closestSocket = null;
			float closestDistance = 14f;
			foreach (SpriteSocketTransform socket in _selectedRecord.sockets)
			{
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
				_previewRect.x + pixelX / sprite.rect.width * _previewRect.width,
				_previewRect.y +
					_previewRect.height -
					pixelY / sprite.rect.height * _previewRect.height);
		}

		private Vector3 PreviewToLocal(Sprite sprite, Vector2 point)
		{
			float pixelsPerUnit = Mathf.Max(sprite.pixelsPerUnit, 0.0001f);
			float pixelX = Mathf.Clamp(
				(point.x - _previewRect.x) / _previewRect.width * sprite.rect.width,
				0f,
				sprite.rect.width);
			float pixelY = Mathf.Clamp(
				(_previewRect.yMax - point.y) / _previewRect.height * sprite.rect.height,
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

			if (_database == null)
			{
				_database = FindSingleDatabase();
				if (_database != null)
				{
					AssignDatabase(_database);
				}
			}

			_librarySprites = GetLibrarySprites();
			if (_librarySprites.Count == 0)
			{
				_selectedRecord = null;
				Repaint();
				return;
			}

			_selectedSpriteIndex = Mathf.Clamp(_selectedSpriteIndex, 0, _librarySprites.Count - 1);
			LoadSelectedRecord();
			Repaint();
		}

		private void LoadSelectedRecord()
		{
			_selectedRecord = null;
			Sprite sprite = GetSelectedSprite();
			if (_database != null && sprite != null)
			{
				_database.TryGet(sprite, out _selectedRecord);
			}

			_selectedSocketName = null;
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
				socket.localEulerAngles = child.localEulerAngles;
				socket.localScale = child.localScale;
			}

			MarkDatabaseChanged();
			SaveDatabase();
			Repaint();
		}

		private void ApplyToChildren()
		{
			if (_selectedRecord == null || _selectedRecord.sockets == null)
			{
				return;
			}

			int appliedCount = 0;
			foreach (SpriteSocketTransform socketData in _selectedRecord.sockets)
			{
				if (socketData == null || !_sockets.TryGetSocket(socketData.name, out Transform socket))
				{
					continue;
				}

				Undo.RecordObject(socket, "Apply Sprite Sockets To Children");
				socketData.ApplyTo(socket);
				PrefabUtility.RecordPrefabInstancePropertyModifications(socket);
				EditorUtility.SetDirty(socket);
				appliedCount++;
			}

			Debug.Log(
				string.Concat(
					"[SpriteResolverSockets] Applied ",
					appliedCount,
					" sockets for ",
					GetSelectedSprite().name,
					"."),
				_sockets);
			Repaint();
		}

		private void AddSocket()
		{
			if (_database == null || GetSelectedSprite() == null)
			{
				return;
			}

			string socketName = SpriteResolverSockets.NormalizeSocketName(_newSocketName);
			if (string.IsNullOrEmpty(socketName))
			{
				return;
			}

			Undo.RecordObject(_database, "Add Sprite Socket");
			_selectedRecord = _selectedRecord ?? _database.GetOrCreate(GetSelectedSprite());
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
				if (child == _sockets.transform || !SpriteResolverSockets.IsSocketName(child.name))
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

			string category = resolver.GetCategory();
			foreach (string label in libraryAsset.GetCategoryLabelNames(category))
			{
				AddSprite(sprites, libraryAsset.GetSprite(category, label));
			}

			return sprites;
		}

		private Sprite GetSelectedSprite()
		{
			return _selectedSpriteIndex >= 0 && _selectedSpriteIndex < _librarySprites.Count
				? _librarySprites[_selectedSpriteIndex]
				: null;
		}

		private void AssignDatabase(SpriteSocketDatabase database)
		{
			_database = database;
			if (_sockets == null)
			{
				return;
			}

			Undo.RecordObject(_sockets, "Assign Sprite Socket Database");
			SerializedObject serializedSockets = new SerializedObject(_sockets);
			SerializedProperty databaseProperty = serializedSockets.FindProperty("_database");
			if (databaseProperty != null)
			{
				databaseProperty.objectReferenceValue = database;
				serializedSockets.ApplyModifiedProperties();
			}
			EditorUtility.SetDirty(_sockets);
			LoadSelectedRecord();
		}

		private void CreateDatabase()
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
			AssignDatabase(database);
			Refresh();
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

		private static SpriteResolverSockets GetSelectedSockets()
		{
			GameObject selectedObject = Selection.activeGameObject;
			return selectedObject == null
				? null
				: selectedObject.GetComponent<SpriteResolverSockets>();
		}

		private static SpriteSocketDatabase FindSingleDatabase()
		{
			string[] guids = AssetDatabase.FindAssets("t:SpriteSocketDatabase");
			if (guids.Length != 1)
			{
				return null;
			}

			return AssetDatabase.LoadAssetAtPath<SpriteSocketDatabase>(
				AssetDatabase.GUIDToAssetPath(guids[0]));
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
	}
}