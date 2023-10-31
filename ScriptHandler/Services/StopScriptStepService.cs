
using System;

namespace ScriptHandler.Services
{
	public class StopScriptStepService
	{
		public void StopStep()
		{
			StopEvent?.Invoke();
		}

		public event Action StopEvent;
	}
}
