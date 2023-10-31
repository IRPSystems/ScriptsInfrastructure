
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models
{


	public class InvalidScriptData: InvalidScriptItemData
	{
		public ObservableCollection<InvalidScriptItemData> ErrorsList { get; set; }
	}

	public class InvalidScriptItemData : ObservableObject
	{
		public string ErrorString { get; set; }
		public string Name { get; set; }
	}
}
