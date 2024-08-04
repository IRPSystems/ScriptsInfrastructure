
using DeviceCommunicators.General;

namespace ScriptHandler.Interfaces
{
	public interface IScriptStepWithCommunicator
	{
		DeviceCommunicator Communicator { get; set; }
	}
}
