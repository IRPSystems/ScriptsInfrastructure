

namespace ScriptHandler.Models.ScriptNodes.Tasks
{
	public class ScriptNodeMailToOutlook : ScriptNodeBase
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
