

using DeviceCommunicators.General;
using DeviceHandler.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System.Collections.Generic;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepCANMessageStop : ScriptStepBase
	{
		[JsonIgnore]
		public ScriptStepCANMessage StepToStop { get; set; }

		public uint CANID { get; set; }

		public ScriptStepCANMessageStop()
		{
			Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
		}

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


		protected override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			CANID = (sourceNode as ScriptNodeCANMessageStop).CANID;
		}
	}
}
