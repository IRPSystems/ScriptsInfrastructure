

using DeviceCommunicators.Models;
using Entities.Models;

namespace ScriptHandler.Interfaces
{
	public interface IScriptNodeWithParam
	{
		DeviceParameterData Parameter { get; set; }
	}
}
