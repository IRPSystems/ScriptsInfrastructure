
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using Entities.Models;
using System.Diagnostics.Contracts;

namespace ScriptHandler.Models
{
	public class DynamicControlColumnData : ObservableObject
	{
		public int FileIndex { get; set; }
		public string ColHeader { get; set; }

		private DeviceParameterData _parameter;
		public DeviceParameterData Parameter 
		{
			get => _parameter;
			set
			{
				_parameter = value;
				if(Parameter != null) 
				{
					ParameterNameAndDevice =
						Parameter.Name +
						" ;; " +
						Parameter.DeviceType;
				}
			}
		}

		public string ParameterNameAndDevice { get; set; }

		public DeviceCommunicator Communicator { get; set; }

		public object Clone()
		{
			return MemberwiseClone();
		}
	}
}
