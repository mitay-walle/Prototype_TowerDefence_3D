using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace TD.Plugins.Runtime
{
	[Serializable]
	public class DisablerList
	{
		[ShowInInspector]
		private List<object> entries = new();

		/// <summary>Событие вызывается при изменении списка, передает true если есть элементы.</summary>
		public event Action<bool> ListChanged;

		public bool HasEntries => entries != null && entries.Count > 0;
		public bool NoEntries => !HasEntries;
		public IReadOnlyList<object> Entries => entries;

		/// <summary>
		/// Инициализация — подписывает колбэк и сразу вызывает его с текущим состоянием.
		/// </summary>
		public void Init(Action<bool> callback)
		{
			if (callback == null)
				return;

			// отписываемся на всякий случай, чтобы не было дубликатов
			ListChanged -= callback;
			ListChanged += callback;

			// сразу оповещаем о текущем состоянии
			callback.Invoke(HasEntries);
		}

		/// <summary>
		/// Добавляет или убирает объект в зависимости от флага.
		/// </summary>
		public void Toggle(object obj, bool state)
		{
			if (obj == null)
				return;

			bool changed = false;

			if (state)
			{
				if (!entries.Contains(obj))
				{
					entries.Add(obj);
					changed = true;
				}
			}
			else
			{
				if (entries.Remove(obj))
					changed = true;
			}

			if (changed)
				OnListChanged();
		}

		public void Add(object obj)
		{
			if (obj == null || entries.Contains(obj))
				return;

			entries.Add(obj);
			OnListChanged();
		}

		public void Remove(object obj)
		{
			if (entries.Remove(obj))
				OnListChanged();
		}

		public void Clear()
		{
			if (entries.Count == 0)
				return;

			entries.Clear();
			OnListChanged();
		}

		private void OnListChanged()
		{
			ListChanged?.Invoke(HasEntries);
		}
	}
}