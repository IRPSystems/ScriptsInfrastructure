

namespace ScriptHandler.Models.ScriptNodes.Tasks
{
	public class ScriptNodeLogicConstrains : ScriptNodeBase
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
