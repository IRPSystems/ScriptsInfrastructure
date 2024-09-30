
using ScriptHandler.Services;
using ScriptHandler.ViewModels;
using ScriptRunner.Services;
using System.Windows;

namespace ScriptRunner.ViewModels
{
	public class ScriptLogDiagramViewModel:
		ScriptDiagramViewModel

	{

		#region Fields

		private RunScriptService _runScript;

#endregion Fields

		#region Constructor

		public ScriptLogDiagramViewModel(
			RunScriptService runScript)
		{
			_runScript = runScript;
			_runScript.ScriptStartedEvent += ScriptStarted;

		}

		#endregion Constructor

		#region Methods

		private void ScriptStarted()
		{
			if (_runScript.CurrentScript == null)
				return;

			DrawScript(_runScript.CurrentScript.CurrentScript);
		}



		#endregion Methods
	}
}
