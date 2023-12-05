
using CommunityToolkit.Mvvm.ComponentModel;
using Entities.Models;
using System.Collections.Generic;

namespace ScriptHandler.Models
{
	public class MotorSettingsData: ObservableObject
	{
		public string Customer { get; set; }
		public string Name { get; set; }

	
		public List<ParameterValueData> CommandParameterValueList { get; set; }
		public List<ParameterValueData> StatusParameterValueList { get; set; }
	}
}
