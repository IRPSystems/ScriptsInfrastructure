

namespace ScriptHandler.Models.ScriptNodes.Tasks
{
	public class ScriptNodeRenameFiles : ScriptNodeBase
	{

		public override string Description
		{
			get
			{
				string stepDescription = $"{Name} - ID:" + ID;
				return stepDescription;
			}
		}

		public ScriptNodeRenameFiles()
		{
			Name = "Rename Files";
		}
	}
}
