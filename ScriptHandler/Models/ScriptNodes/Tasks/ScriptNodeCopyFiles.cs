

namespace ScriptHandler.Models.ScriptNodes.Tasks
{
	public class ScriptNodeCopyFiles: ScriptNodeBase
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
