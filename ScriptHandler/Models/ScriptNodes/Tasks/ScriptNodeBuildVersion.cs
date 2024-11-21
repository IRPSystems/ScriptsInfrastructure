

using DeviceCommunicators.Models;

namespace ScriptHandler.Models.ScriptNodes.Tasks
{
	public class ScriptNodeBuildVersion : ScriptNodeBase
	{
		public override string Description
		{
			get
			{
				string stepDescription = $"{Name} - ID:" + ID;
				return stepDescription;
			}
		}

		public ScriptNodeBuildVersion()
		{
			Name = "Build Version";
		}
	}
}
