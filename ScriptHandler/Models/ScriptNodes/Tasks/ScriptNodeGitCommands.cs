

namespace ScriptHandler.Models.ScriptNodes.Tasks
{
	public class ScriptNodeGitCommands : ScriptNodeBase
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
