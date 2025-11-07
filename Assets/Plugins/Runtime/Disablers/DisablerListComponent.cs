namespace Plugins.Utilities
{
	public class DisablerListComponent : DisablerListBehaviour
	{
		protected override void OnChanged()
		{
			enabled = DisablerList.NoEntries;
		}
	}
}