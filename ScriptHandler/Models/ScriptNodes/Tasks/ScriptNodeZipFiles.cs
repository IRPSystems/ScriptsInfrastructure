

namespace ScriptHandler.Models.ScriptNodes.Tasks
{
	public class ScriptNodeZipFiles : ScriptNodeBase
	{

		public override string Description
		{
			get
			{
				string stepDescription = $"{Name} - ID:" + ID;
				return stepDescription;
			}
		}

		public ScriptNodeZipFiles()
		{
			Name = "Zip Files";
		}
	}
}
