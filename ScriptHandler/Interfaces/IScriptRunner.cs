

using ScriptHandler.Models;
using System;

namespace ScriptHandler.Interfaces
{
	public interface IScriptRunner
	{
		public GeneratedScriptData CurrentScript { get; set; }
		public string ScriptErrorMessage { get; set; }

		public void Start();

		public void StopStep();

		public void Abort();

		public void PausStep();

		public void NextStep();



		public event Action<bool> ScriptEndedEvent;
	}
}
