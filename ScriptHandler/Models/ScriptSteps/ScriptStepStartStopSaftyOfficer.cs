
using DeviceCommunicators.General;
using DeviceHandler.Models;
using Entities.Enums;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepStartStopSaftyOfficer : ScriptStepBase
	{
		public bool IsStart;
		public ActiveErrorLevelEnum SafetyOfficerErrorLevel { get; set; }

		public ScriptStepStartStopSaftyOfficer()
		{
			if (Application.Current != null)
				Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
			Description = "Start/Stop Safty Officer";
		}

		protected override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			double d = Convert.ToDouble((sourceNode as ScriptNodeSetParameter).Value);
			IsStart = d == 1;

			//if(IsStart)
			//	Description = "Start Safty Officer" + " - ID:" + ID;
			//else
			//	Description = "Stop Safty Officer" + " - ID:" + ID;

			IsPass = true;
		}
	}
}
