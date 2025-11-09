using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace Design.Tags
{
	[Serializable, HideLabel]
	public class TagContainer : IEnumerable<Tag>
	{
		[ListDrawerSettings(OnBeginListElementGUI = nameof(DrawTagColor), OnEndListElementGUI = nameof(EndTagDraw))]
		[SerializeField] private List<Tag> Tags = new();
		public int Count => Tags.Count;

		public bool Contains(Tag tag) => Tags.Contains(tag);
		public Tag GetAnySame(TagContainer other) => Tags.Find(other.Contains);
		public Tag GetAnySame(IEnumerable<Tag> other)
		{
			foreach (Tag tag in other)
			{
				if (Tags.Contains(tag)) return tag;
			}

			return Tag.None;
		}
		public bool ContainsAny(IEnumerable<Tag> other)
		{
			bool result = false;

			foreach (Tag tag in other)
			{
				if (Tags.Contains(tag)) result = true;
			}

			return result;
		}
		public bool Contains(TagContainer other) => !other.Tags.Exists(tag => !Tags.Contains(tag));
		public bool ContainsAny(TagContainer other) => Tags.Exists(other.Contains);

		public void Add(Tag tag) => Tags.Add(tag);
		public void AddRange(IEnumerable<Tag> tags, bool allowDublicates = false)
		{
			if (tags == null) return;
			
			foreach (Tag tag in tags)
			{
				if (!Tags.Contains(tag) || allowDublicates)
				{
					Tags.Add(tag);
				}
			}
		}
		public void AddRange(TagContainer tags, bool allowDublicates = false)
		{
			if (tags == null) return;
 
			tags.Tags.ForEach(tag =>
			{
				if (!Tags.Contains(tag) || allowDublicates)
				{
					Tags.Add(tag);
				}
			});
		}

		public void RemoveAll(TagContainer tags)
		{
			if (tags == null) return;
			foreach (Tag tag in tags.Tags)
			{
				if (Tags.Contains(tag))
				{
					Tags.Remove(tag);
				}
			}
		}
		public void Remove(Tag tag)
		{
			//if (tag == Tag.None) return;
			if (Tags.Contains(tag)) Tags.Remove(tag);
		}

		public IEnumerator<Tag> GetEnumerator() => Tags.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		public Tag this[int index] => Tags[index];

		private void DrawTagColor(int index)
		{
#if UNITY_EDITOR
			GUIHelper.PushColor(Tags[index].ToColor());
  #endif
		}

		private void EndTagDraw(int index)
		{
    #if UNITY_EDITOR
			GUIHelper.PopColor();
  #endif
		}
	}
}