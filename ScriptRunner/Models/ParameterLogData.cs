
using CommunityToolkit.Mvvm.ComponentModel;
using Entities.Models;
using System.Collections.Generic;

namespace ScriptRunner.Models
{
	public class ParameterLogListData : ObservableObject
	{
		public string Time { get; set; }
		public List<ParameterLogData> Values { get; set; }
	}

	public class ParameterLogData
	{
		public DeviceParameterData Parameter { get; set; }
		public double Value { get; set; }
	}
}
