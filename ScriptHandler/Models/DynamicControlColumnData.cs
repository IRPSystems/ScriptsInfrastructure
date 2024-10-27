
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
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
					if (Parameter is MCU_ParamData mcuParam)
					{
						ParameterNameAndDevice =
							mcuParam.Cmd +
							" ;; " +
							Parameter.DeviceType;
					}
					else
					{
						ParameterNameAndDevice =
							Parameter.Name +
							" ;; " +
							Parameter.DeviceType;
					}
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
