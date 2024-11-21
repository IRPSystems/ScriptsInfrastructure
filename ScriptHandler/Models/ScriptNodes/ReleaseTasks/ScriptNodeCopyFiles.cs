

namespace ScriptHandler.Models.ScriptNodes.ReleaseTasks
{
	public class ScriptNodeCopyFiles: ScriptNodeReleaseTasks
	{
		public override string Description
		{
			get
			{
				string stepDescription = $"{Name} - ID:" + ID;
				return stepDescription;
			}
		}

		public ScriptNodeCopyFiles()
		{
			Name = "Copy Files";
		}
	}
}
