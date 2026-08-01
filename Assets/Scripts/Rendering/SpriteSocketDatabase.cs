using System.Collections.Generic;
using UnityEngine;

namespace TD.Rendering
{
	[CreateAssetMenu(fileName = "SpriteSocketDatabase", menuName = "TD/Sprite Socket Database")]
	[Icon("Packages/com.unity.2d.animation/Editor/Assets/ComponentIcons/Animation.SpriteResolver.asset")]
	public sealed class SpriteSocketDatabase : ScriptableObject
	{
		[SerializeField] private List<SpriteSocketRecord> _records =
			new List<SpriteSocketRecord>();

		public bool TryGet(Sprite sprite, out SpriteSocketRecord record)
		{
			record = null;
			if (sprite == null || _records == null)
			{
				return false;
			}

			foreach (SpriteSocketRecord candidate in _records)
			{
				if (candidate != null && candidate.sprite == sprite)
				{
					record = candidate;
					return true;
				}
			}

			return false;
		}

		public bool TryGetEffective(Sprite sprite, out SpriteSocketRecord record)
		{
			if (TryGet(sprite, out record))
			{
				if ((record.sockets == null || record.sockets.Count == 0) &&
					record.mainSprite != null &&
					record.mainSprite != sprite &&
					TryGet(record.mainSprite, out SpriteSocketRecord mainRecord))
				{
					record = mainRecord;
				}

				return true;
			}

			return false;
		}

#if UNITY_EDITOR
		public SpriteSocketRecord GetOrCreate(Sprite sprite)
		{
			if (_records == null)
			{
				_records = new List<SpriteSocketRecord>();
			}

			if (TryGet(sprite, out SpriteSocketRecord record))
			{
				return record;
			}

			var createdRecord = new SpriteSocketRecord
			{
				sprite = sprite
			};
			_records.Add(createdRecord);
			return createdRecord;
		}
#endif
	}
}
