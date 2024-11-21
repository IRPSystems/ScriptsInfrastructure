

namespace ScriptHandler.Models.ScriptNodes.Tasks
{
	public class ScriptNodeCreateReleaseNotes : ScriptNodeBase
	{

		public override string Description
		{
			get
			{
				string stepDescription = $"{Name} - ID:" + ID;
				return stepDescription;
			}
		}

		public ScriptNodeCreateReleaseNotes()
		{
			Name = "Create Release Notes";
		}
	}
}
