
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
			_isExecuted = true;
		}

		protected override void Stop()
		{

		}
	}
}
