

using DeviceCommunicators.General;
using DeviceHandler.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models
{
	public class ScriptStepStopContinuous : ScriptStepBase
	{
		[JsonIgnore]
		public IScriptItem StepToStop { get; set; }

		public int StepToStopID { get; set; }


		[JsonIgnore]
		public GeneratedTestData ParentProject { get; set; }

		public override void Execute()
		{
			IsExecuted = true;
			if (StepToStop == null)
			{
				ErrorMessage = Description + ":\r\nThe step to stop is not set.";
				IsPass = false;
				return;
			}



			(StepToStop as IScriptStepContinuous).StopContinuous();

			IsPass = true;
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (StepToStop == null)
				return true;

			return false;
		}


		protected override void Generate(
			ScriptNodeBase sourceNode, 
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			StepToStopID = (sourceNode as ScriptNodeStopContinuous).StepToStopID;
		}
	}
}
