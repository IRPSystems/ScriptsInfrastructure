

namespace ScriptHandler.Models.ScriptNodes.Tasks
{
	public class ScriptNodeUploadToGithub : ScriptNodeBase
	{

		public override string Description
		{
			get
			{
				string stepDescription = $"{Name} - ID:" + ID;
				return stepDescription;
			}
		}

		public ScriptNodeUploadToGithub()
		{
			Name = "Upload To Github";
		}
	}
}
