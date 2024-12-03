

namespace ScriptHandler.Models
{
	public class ScriptStepResetParentTool : ScriptStepBase
	{
		#region Properties

		

		#endregion Properties

		
		#region Constructor

		public ScriptStepResetParentTool()
		{
			
		}

		#endregion Constructor


		#region Methods

		public override void Execute()
		{
			
			IsPass = true;
			_isExecuted = true;
		}

		

		#endregion Methods
	}
}
