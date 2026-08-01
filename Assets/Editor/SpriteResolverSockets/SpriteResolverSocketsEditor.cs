using TD.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace TD.Rendering.Editor
{
	[CustomEditor(typeof(SpriteResolverSockets))]
	public sealed class SpriteResolverSocketsEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			EditorGUILayout.Space(4f);
			if (GUILayout.Button("Open Sprite Socket Editor", GUILayout.Height(28f)))
			{
				SpriteResolverSocketsWindow.Open((SpriteResolverSockets)target);
			}

			EditorGUILayout.HelpBox(
				"Runtime discovers child objects named Socket_* and applies the shared database record for the current Sprite.",
				MessageType.Info);
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
	}
}