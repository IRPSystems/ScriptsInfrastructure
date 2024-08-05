﻿
using DeviceCommunicators.General;
using DeviceCommunicators.SwitchRelay32;
using DeviceHandler.Models;
using Newtonsoft.Json.Linq;
using ScriptHandler.Enums;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepStartStopSaftyOfficer : ScriptStepBase
	{
		public bool IsStart;
		public SafetyOfficerErrorLevelEnum SafetyOfficerErrorLevel { get; set; }

		public ScriptStepStartStopSaftyOfficer()
		{
			Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
			Description = "Start/Stop Safty Officer";
		}

		public override void Generate(
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
