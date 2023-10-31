
using DeviceHandler.Models;
using ScriptHandler.Models;
using System.Collections.ObjectModel;

namespace ScriptHandler.Interfaces
{
	public interface IScriptItem
	{
		string Description { get; set; }
		IScriptItem PassNext { get; set; }
		IScriptItem FailNext { get; set; }
		int ID { get; set; }

		bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList);
	}
}
