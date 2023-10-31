
using Entities.Models;
using System.Collections.ObjectModel;

namespace ScriptRunner.Models
{
	public class RECORD_LIST_CHANGEDMessage
	{
		public ObservableCollection<DeviceParameterData> LogParametersList { get; set; }
	}
}
