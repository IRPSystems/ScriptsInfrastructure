

namespace ScriptHandler.Models.ScriptNodes.ReleaseTasks
{
	public class ScriptNodeZipFiles : ScriptNodeReleaseTasks
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
