

namespace ScriptHandler.Models.ScriptNodes.ReleaseTasks
{
	public class ScriptNodeCreateReleaseNotes : ScriptNodeReleaseTasks
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
