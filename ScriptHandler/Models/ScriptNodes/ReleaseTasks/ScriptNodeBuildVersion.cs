

namespace ScriptHandler.Models.ScriptNodes.ReleaseTasks
{
	public class ScriptNodeBuildVersion : ScriptNodeReleaseTasks
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
