

namespace ScriptHandler.Models
{
	public class ScriptStepResetParentSweep : ScriptStepBase
	{
		#region Properties

		public ScriptStepSweep ParentSweep { get; set; }

		#endregion Properties

		
		#region Constructor

		public ScriptStepResetParentSweep()
		{
			
		}

		#endregion Constructor


		#region Methods

		public override void Execute()
		{
			ErrorMessage = "Failed to reste the Sweep\r\n\r\n";
			IsPass = ParentSweep.Reset();
			IsExecuted = true;
		}

		
		

		#endregion Methods
	}
}
