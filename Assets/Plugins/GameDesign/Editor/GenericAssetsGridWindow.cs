using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace TD.Plugins.GameDesign
{
	public abstract class GenericAssetsGridWindow<T> : OdinEditorWindow where T : Object
	{
		string findFilter => $"t:{typeof(T).Name}";

		[ShowInInspector]

		//[ListDrawerSettings(IsReadOnly = true,ShowPaging = false,ShowFoldout = false)]
		[TableList(DrawScrollView = true, IsReadOnly = true, ShowPaging = false, ShowIndexLabels = false, AlwaysExpanded = true)]
		[InlineEditor(InlineEditorObjectFieldModes.Hidden, Expanded = true, DrawPreview = false)]

		//[TableList(AlwaysExpanded = true, DrawScrollView = true, HideToolbar = true, IsReadOnly = true, NumberOfItemsPerPage = 3, ShowPaging = false, ShowIndexLabels = false)]
		private List<T> assets = new List<T>();

		protected override void OnEnable()
		{
			base.OnEnable();
			RefreshAssets();
		}

		[Button(Name = "", Icon = SdfIconType.ArrowClockwise, Stretch = false, ButtonAlignment = 0), PropertyOrder(-1)]
		private void RefreshAssets()
		{
			LoadAllAssets();
			Repaint();
		}

		private void LoadAllAssets()
		{
			assets.Clear();

			string typeName = typeof(T).Name;
			string filter = string.IsNullOrEmpty(findFilter) ? $"t:{typeName}" : findFilter;

			string[] guids = AssetDatabase.FindAssets(filter);
			foreach (var guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var obj = AssetDatabase.LoadAssetAtPath<T>(path);
				if (obj != null)
					assets.Add(obj);
			}
		}
	}
}