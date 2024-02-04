
using DeviceCommunicators.General;
using DeviceHandler.Models;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System.Collections.Generic;

namespace ScriptHandler.Models.ScriptSteps
{
	public class ScriptStepEOLSendSN : ScriptStepBase
	{


		public override void Execute()
		{

		}

		protected override void Stop()
		{

		}

		public override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
		}
	}
}
