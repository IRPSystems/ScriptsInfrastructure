

namespace ScriptHandler.Models.ScriptNodes.ReleaseTasks
{
	public class ScriptNodeMailToOutlook : ScriptNodeReleaseTasks
	{

		public override string Description
		{
			get
			{
				string stepDescription = $"{Name} - ID:" + ID;
				return stepDescription;
			}
		}

		public ScriptNodeMailToOutlook()
		{
			Name = "Mail To Outlook";
		}
	}
}
