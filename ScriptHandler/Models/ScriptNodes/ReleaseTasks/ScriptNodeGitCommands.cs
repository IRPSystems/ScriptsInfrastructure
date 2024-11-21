

namespace ScriptHandler.Models.ScriptNodes.ReleaseTasks
{
	public class ScriptNodeGitCommands : ScriptNodeReleaseTasks
	{

		public override string Description
		{
			get
			{
				string stepDescription = $"{Name} - ID:" + ID;
				return stepDescription;
			}
		}

		public ScriptNodeGitCommands()
		{
			Name = "Git Commands";
		}
	}
}
