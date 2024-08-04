
using ScriptHandler.Interfaces;
using ScriptHandler.Models;
using System.Collections.Generic;

namespace ScriptRunner.Services
{
	internal class HandleContinuousStepsService
	{
		#region Fields

		private Dictionary<string, IScriptStepContinuous> _descriptionToContinuous;

		#endregion Fields

		#region Constructor

		public HandleContinuousStepsService()
		{
			_descriptionToContinuous = new Dictionary<string, IScriptStepContinuous>();
		}

		#endregion Constructor

		#region Methods

		public void StartContinuous(IScriptStepContinuous scriptStepContinuous)
		{
			if (_descriptionToContinuous.ContainsKey((scriptStepContinuous as ScriptStepBase).Description) == true)
				return;

			_descriptionToContinuous.Add((scriptStepContinuous as ScriptStepBase).Description, scriptStepContinuous);
			scriptStepContinuous.ContinuousEndedEvent += ContinuousEndedEvent;
			(scriptStepContinuous as ScriptStepBase).Execute();
		}

		public void StopContinuous(string continuousDescription)
		{
			if (_descriptionToContinuous.ContainsKey(continuousDescription) == false)
				return;

			IScriptStepContinuous scriptStepContinuous =
				_descriptionToContinuous[continuousDescription];
			StopContinuous_Do(scriptStepContinuous);
		}

		public void EndAll()
		{
			foreach(IScriptStepContinuous scriptStepContinuous in _descriptionToContinuous.Values)
			{
				StopContinuous_Do(scriptStepContinuous);				
			}

			_descriptionToContinuous.Clear();
		}



		private void StopContinuous_Do(IScriptStepContinuous scriptStepContinuous)
		{
			scriptStepContinuous.StopContinuous();
			ContinuousEndedEvent(scriptStepContinuous);
		}

		private void ContinuousEndedEvent(IScriptStepContinuous scriptStepContinuous)
		{
			if (_descriptionToContinuous.ContainsKey((scriptStepContinuous as ScriptStepBase).Description) == false)
				return;

			_descriptionToContinuous.Remove((scriptStepContinuous as ScriptStepBase).Description);
		}


		#endregion Methods
	}
}
