

namespace ScriptHandler.Models.ScriptNodes.Tasks
{
	public class ScriptNodeUseExeFiles : ScriptNodeBase
	{

		public override string Description
		{
			get
			{
				string stepDescription = $"{Name} - ID:" + ID;
				return stepDescription;
			}
		}

		public ScriptNodeUseExeFiles()
		{
			Name = "Use Exe Files";
		}
	}
}
