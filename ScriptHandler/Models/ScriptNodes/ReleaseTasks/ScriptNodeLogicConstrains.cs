

namespace ScriptHandler.Models.ScriptNodes.ReleaseTasks
{
	public class ScriptNodeLogicConstrains : ScriptNodeReleaseTasks
	{

		public override string Description
		{
			get
			{
				string stepDescription = $"{Name} - ID:" + ID;
				return stepDescription;
			}
		}

		public ScriptNodeLogicConstrains()
		{
			Name = "Logic Constrains";
		}
	}
}
