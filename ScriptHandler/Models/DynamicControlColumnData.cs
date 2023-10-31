
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.General;
using Entities.Models;

namespace ScriptHandler.Models
{
	public class DynamicControlColumnData : ObservableObject
	{
		public int FileIndex { get; set; }
		public string ColHeader { get; set; }
		public DeviceParameterData Parameter { get; set; }
		public DeviceCommunicator Communicator { get; set; }

		public object Clone()
		{
			return MemberwiseClone();
		}
	}
}
