using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Rendering;

namespace Design.Tags
{
	[CreateAssetMenu(menuName = "Tags/TagDB")]
	public class TagDB : ScriptableObject
	{
		static TagDB Load() => Resources.Load<TagDB>("TagDB");
		private static TagDB instance;

		public static TagDB Instance
		{
			get
			{
				if (instance == null) instance = Load();
				return instance;
			}
		}

		[SerializeField] private SerializedDictionary<Tag, TagData> Tags = new();

		public TagData this[Tag key] => Tags[key];

		[Button]
		private void FillAll()
		{
			foreach (Tag tag in Enum.GetValues(typeof(Tag)))
			{
				Tags.TryAdd(tag, new TagData(tag, new LocalizedString("UI",""), new LocalizedString("UI","")));
			}
		}
	}

	[Serializable, TableList]
	public class TagData
	{
		public TagData(Tag tag, LocalizedString name, LocalizedString description)
		{
			Tag = tag;
			Name = name;
			Description = description;
		}

		[field: SerializeField] public Tag Tag { get; private set; }
		[field: SerializeField] public Color Color { get; private set; } = Color.white;
		[field: SerializeField] public LocalizedString Name { get; private set; }
		[field: SerializeField] public LocalizedString Description { get; private set; }
		//[ShowInInspector, TextArea] protected string _finalText => ToString();

		//[Button]
		public override string ToString()
			=> $"<color=#{ColorUtility.ToHtmlStringRGBA(Color)}>{Name.GetLocalizedString()}</color> - {Description.GetLocalizedString()}";

		public static implicit operator TagData(Tag tag) => tag.ToTagData();
	}

	public static class TagUtility
	{
		public static TagData ToTagData(this Tag tag) => TagDB.Instance[tag];
		public static Color ToColor(this Tag tag) => TagDB.Instance[tag].Color;
		public static LocalizedString ToName(this Tag tag) => TagDB.Instance[tag].Name;
		public static LocalizedString ToDescription(this Tag tag) => TagDB.Instance[tag].Description;
	}
}