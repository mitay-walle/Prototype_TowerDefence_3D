using System;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace TD.Rendering
{
	[ExecuteAlways]
	[RequireComponent(typeof(SpriteResolver))]
	[RequireComponent(typeof(SpriteRenderer))]
	[Icon("Packages/com.unity.2d.animation/Editor/Assets/ComponentIcons/Animation.SpriteResolver.asset")]
	public sealed class SpriteResolverSockets : MonoBehaviour
	{
		public const string SocketPrefix = "Socket_";

		[SerializeField] private SpriteSocketDatabase _database;

		private SpriteRenderer _spriteRenderer;
		private Sprite _appliedSprite;

		public SpriteSocketDatabase Database => _database;

		private void Awake()
		{
			CacheComponents();
		}

		private void OnEnable()
		{
			CacheComponents();
			_appliedSprite = null;
		}

		private void OnValidate()
		{
			_appliedSprite = null;
		}

		private void LateUpdate()
		{
			CacheComponents();

			Sprite currentSprite = _spriteRenderer.sprite;
			if (currentSprite == _appliedSprite)
			{
				return;
			}

			_appliedSprite = currentSprite;
			if (currentSprite == null ||
				_database == null ||
				!_database.TryGet(currentSprite, out SpriteSocketRecord record) ||
				record.sockets == null)
			{
				return;
			}

			foreach (SpriteSocketTransform socketData in record.sockets)
			{
				if (socketData != null && TryGetSocket(socketData.name, out Transform socket))
				{
					socketData.ApplyTo(socket);
				}
			}
		}

		public bool TryGetSocket(string socketName, out Transform socket)
		{
			socket = null;
			string normalizedName = NormalizeSocketName(socketName);
			if (string.IsNullOrEmpty(normalizedName))
			{
				return false;
			}

			Transform[] children = GetComponentsInChildren<Transform>(true);
			foreach (Transform child in children)
			{
				if (child == transform ||
					!string.Equals(
						NormalizeSocketName(child.name),
						normalizedName,
						StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				socket = child;
				return true;
			}

			return false;
		}

		public bool AttachToSocket(
			string socketName,
			GameObject objectToAttach,
			bool preserveWorldTransform = false)
		{
			if (objectToAttach == null || objectToAttach == gameObject)
			{
				return false;
			}

			if (!TryGetSocket(socketName, out Transform socket))
			{
				return false;
			}

			Transform objectTransform = objectToAttach.transform;
			if (socket == objectTransform || socket.IsChildOf(objectTransform))
			{
				return false;
			}

			objectTransform.SetParent(socket, preserveWorldTransform);
			if (!preserveWorldTransform)
			{
				objectTransform.localPosition = Vector3.zero;
				objectTransform.localRotation = Quaternion.identity;
				objectTransform.localScale = Vector3.one;
			}

			return true;
		}

		public bool DetachFromSocket(GameObject objectToDetach, bool preserveWorldTransform = true)
		{
			if (objectToDetach == null)
			{
				return false;
			}

			Transform objectTransform = objectToDetach.transform;
			Transform parent = objectTransform.parent;
			if (parent == null ||
				!TryGetSocket(parent.name, out Transform socket) ||
				socket != parent)
			{
				return false;
			}

			objectTransform.SetParent(transform, preserveWorldTransform);
			if (!preserveWorldTransform)
			{
				objectTransform.localPosition = Vector3.zero;
				objectTransform.localRotation = Quaternion.identity;
				objectTransform.localScale = Vector3.one;
			}

			return true;
		}

		public static bool IsSocketName(string objectName)
		{
			return !string.IsNullOrEmpty(objectName) &&
				objectName.StartsWith(SocketPrefix, StringComparison.OrdinalIgnoreCase);
		}

		public static string NormalizeSocketName(string socketName)
		{
			if (string.IsNullOrEmpty(socketName))
			{
				return string.Empty;
			}

			return IsSocketName(socketName)
				? socketName.Substring(SocketPrefix.Length)
				: socketName;
		}

		private void CacheComponents()
		{
			if (_spriteRenderer == null)
			{
				_spriteRenderer = GetComponent<SpriteRenderer>();
			}
		}
	}
}