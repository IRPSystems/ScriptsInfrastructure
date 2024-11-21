

namespace ScriptHandler.Models.ScriptNodes.Tasks
{
	public class ScriptNodeLinkToJira : ScriptNodeBase
	{

		public override string Description
		{
			get
			{
				string stepDescription = $"{Name} - ID:" + ID;
				return stepDescription;
			}
		}

		public ScriptNodeLinkToJira()
		{
			Name = "Link To Jira";
		}
	}
}
