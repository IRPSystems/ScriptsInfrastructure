
namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeResetParentSweep : ScriptNodeBase
	{
		public ScriptNodeResetParentSweep()
		{
			Name = "Reset Parent Sweep";
		}

		

		public override string Description
		{
			get
			{
				return "Reset Parent Sweep" + " - ID:" + ID;
			}
		}
		
	}
}
