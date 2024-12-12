
using ScriptHandler.Enums;

namespace ScriptHandler.Models
{
	public class ScriptStepScritAborted : ScriptStepNotification
	{

		public ScriptStepScritAborted()
		{
			NotificationLevel= NotificationLevelEnum.Error;
		}

		public override void Execute()
		{
			IsPass = false;
			IsExecuted = true;
		}

		protected override void Stop()
		{

		}
	}
}
