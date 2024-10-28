

using CommunityToolkit.Mvvm.Input;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeCANMessageStop : ScriptNodeStopContinuous
	{
		public ScriptNodeCANMessageStop()
		{
			Name = "CAN Message Stop";
		}
	}
}
