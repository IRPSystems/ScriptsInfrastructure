
using DeviceCommunicators.Models;
using Entities.Models;

namespace ScriptHandler.Interfaces
{
	public interface IScriptStepWithParameter
	{
		DeviceParameterData Parameter { get; set; }
	}
}
