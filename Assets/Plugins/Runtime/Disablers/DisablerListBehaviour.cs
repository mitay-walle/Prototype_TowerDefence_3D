using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace Plugins.Utilities
{
	public class DisablerListBehaviour : MonoBehaviour
	{
		[field: ShowInInspector] public DisablerList DisablerList { get; private set; } = new();
		public bool IsEnalbed => DisablerList.IsEnabled;
		[SerializeField] protected bool Logs;
		[FoldoutGroup("Events")] public UnityEvent<bool> Changed;
		[FoldoutGroup("Events")] public UnityEvent Enabled;
		[FoldoutGroup("Events")] public UnityEvent Disabled;

		protected virtual void Awake()
		{
			DisablerList.Disablers.ItemAdded += DisablerAdd;
			DisablerList.Disablers.ItemRemoved += DisablerRemove;
		}

		protected void DisablerAdd(ObservableList<object> sender, ListChangedEventArgs<object> listChangedEventArgs) => OnChanged();

		protected void DisablerRemove(ObservableList<object> sender, ListChangedEventArgs<object> listChangedEventArgs) => OnChanged();

		protected virtual void OnChanged()
		{
			Changed?.Invoke(IsEnalbed);
			if (IsEnalbed)
			{
				Enabled?.Invoke();
			}
			else
			{
				Disabled?.Invoke();
			}
		}
	}
}