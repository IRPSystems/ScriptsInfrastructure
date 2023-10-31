
using System.Collections.ObjectModel;

namespace ScriptHandler.Interfaces
{
	public interface IScript
	{
		string Name { get; set; }
		public ObservableCollection<IScriptItem> ScriptItemsList { get; set; }
		bool? IsPass { get; set; }
	}
}
