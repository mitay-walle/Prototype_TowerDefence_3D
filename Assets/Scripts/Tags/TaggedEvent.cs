using System;
using UnityEngine;
using UnityEngine.Events;
namespace Design.Tags
{
	[Serializable]
	public class TaggedEvent
	{
		[field: SerializeField] public TagContainer Tags { get; private set; }
		public UnityEvent<ITagged> OnExecute;

		public void Execute(ITagged target)
		{
			if (target == null) return;
			if (target.Tags.ContainsAny(Tags))
			{
				OnExecute?.Invoke(target);
			}
		}
	}
}