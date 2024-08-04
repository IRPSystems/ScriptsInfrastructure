
using System;

namespace ScriptHandler.Interfaces
{
	public interface IScriptStepContinuous
	{
		void StopContinuous();

		event Action<IScriptStepContinuous> ContinuousEndedEvent;
		event Action<string> ContinuousErrorEvent;
	}
}
