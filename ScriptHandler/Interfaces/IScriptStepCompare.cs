
using DeviceCommunicators.Models;

namespace ScriptHandler.Interfaces
{
	public interface IScriptStepCompare
	{
		public DeviceParameterData ValueLeft { get; set; }

		public object ValueRight { get; set; }
	}
}
