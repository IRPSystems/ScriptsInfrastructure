

namespace ScriptHandler.Models.ScriptNodes.Tasks
{
	public class ScriptNodeOpenUrls : ScriptNodeBase
	{

		public override string Description
		{
			get
			{
				string stepDescription = $"{Name} - ID:" + ID;
				return stepDescription;
			}
		}

		public ScriptNodeOpenUrls()
		{
			Name = "Open Urls";
		}
	}
}
