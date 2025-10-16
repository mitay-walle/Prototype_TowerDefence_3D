using UnityEditor;
using UnityEngine;

namespace Plugins.TransformOperations
{
	public static class TransformUtils
	{
		/// <summary>
		/// Ищет Transform в сцене по строковому пути "Root/Child/Child2".
		/// </summary>
		public static Transform FindByPath(string path)
		{
			if (string.IsNullOrEmpty(path))
				return null;

			string[] parts = path.Split('/');
			if (parts.Length == 0)
				return null;

			// ищем объект верхнего уровня
			GameObject rootObj = GameObject.Find(parts[0]);
			if (rootObj == null)
				return null;

			Transform current = rootObj.transform;

			// спускаемся вниз по иерархии
			for (int i = 1; i < parts.Length; i++)
			{
				current = current.Find(parts[i]);
				if (current == null)
					return null;
			}

			return current;
		}

		/// <summary>
		/// Возвращает полный путь Transform от корня сцены.
		/// </summary>
		public static string GetPath(Transform tr)
		{
			if (tr == null) return string.Empty;

			string path = tr.name;
			while (tr.parent != null)
			{
				tr = tr.parent;
				path = tr.name + "/" + path;
			}
			return path;
		}

#if UNITY_EDITOR
		[MenuItem("CONTEXT/Transform/Copy Path")]
		private static void CopyPath(MenuCommand command)
		{
			Transform tr = (Transform)command.context;
			string path = GetPath(tr);
			EditorGUIUtility.systemCopyBuffer = path;
			Debug.Log($"Copied path: {path}");
		}
#endif
	}
}