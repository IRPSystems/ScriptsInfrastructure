

using DeviceCommunicators.General;
using DeviceHandler.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System.Collections.Generic;

namespace ScriptHandler.Models
{
	public class ScriptStepCANMessageStop : ScriptStepBase
	{
		[JsonIgnore]
		public ScriptStepCANMessage StepToStop { get; set; }

		public uint CANID { get; set; }
		public string MessageName { get; set; }



		public override void Execute()
		{
			if (StepToStop == null)
			{
				ErrorMessage = Description + ":\r\nThe step to stop is not set.";
				IsPass = false;
				return;
			}



			(StepToStop as IScriptStepContinuous).StopContinuous();

			IsPass = true;
		}


		public override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			if ((sourceNode as ScriptNodeStopContinuous).StepToStop is ScriptNodeCANMessage canMessage)
			{
				CANID = canMessage.CANID;
				MessageName = canMessage.MessageName;
			}
		}
	}
}
