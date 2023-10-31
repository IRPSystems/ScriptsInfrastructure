
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models
{
	
	public class GeneratedTestData: GeneratedScriptData
	{
		[JsonIgnore]
		public ObservableCollection<ScriptStepCANMessage> CanMessagesList { get; set; }

		public GeneratedTestData()
		{
			CanMessagesList = new ObservableCollection<ScriptStepCANMessage>();
		}
	}
}
