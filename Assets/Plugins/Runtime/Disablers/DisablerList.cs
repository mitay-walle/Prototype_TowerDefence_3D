using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace Plugins.Utilities
{
	[InlineProperty, HideReferenceObjectPicker]
	public class DisablerList
	{
		[ShowInInspector, HideLabel, HorizontalGroup(Width = 15)] public bool IsEnabled => Disablers.Count == 0;
		[ShowInInspector, HideLabel, HorizontalGroup] public ObservableList<object> Disablers = new();

		public DisablerList() { }

		public DisablerList(Action<bool> action)
		{
			Disablers.ItemAdded += Changed;
			Disablers.ItemRemoved += Changed;

			void Changed(ObservableList<object> sender, ListChangedEventArgs<object> listChangedEventArgs) => action?.Invoke(IsEnabled);
		}

		public DisablerList(Behaviour component)
		{
			Disablers.ItemAdded += Changed;
			Disablers.ItemRemoved += Changed;

			void Changed(ObservableList<object> sender, ListChangedEventArgs<object> listChangedEventArgs) => component.enabled = IsEnabled;
		}

		public void Add(object source)
		{
			if (!Disablers.Contains(source)) Disablers.Add(source);
		}

		public void Remove(object source)
		{
			if (Disablers.Contains(source)) Disablers.Remove(source);
		}

		public void Toggle(object source, bool value)
		{
			bool contains = Disablers.Contains(source);
			if (contains == value) return;

			if (value)
			{
				Disablers.Add(source);
			}
			else
			{
				Disablers.Remove(source);
			}
		}

		[Button]
		private void Toggle()
		{
			Toggle(this, !Disablers.Contains(this));
		}
	}
}