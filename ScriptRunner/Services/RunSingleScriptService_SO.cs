

using DeviceHandler.Models;
using ScriptHandler.Models;
using ScriptHandler.Services;
using ScriptRunner.ViewModels;

namespace ScriptRunner.Services
{
	public class RunSingleScriptService_SO: RunSingleScriptService
	{
		public bool IsAborted { get; set; }


		public RunSingleScriptService_SO(
			RunScriptService.RunTimeData runTime,
			ScriptLoggerService mainScriptLogger,
			GeneratedScriptData currentScript,
			StopScriptStepService stopScriptStep,
			DevicesContainer devicesContainer,
			CANMessageSenderViewModel canMessageSender):
			base(
				runTime,
				mainScriptLogger,
				currentScript,
				stopScriptStep,
				devicesContainer,
				canMessageSender)
		{
			IsAborted = false;
		}


	}
}
