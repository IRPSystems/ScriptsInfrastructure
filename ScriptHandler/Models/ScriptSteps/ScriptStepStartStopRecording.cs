
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
	public class ScriptStepStartStopRecording : ScriptStepBase
	{
		public bool IsStart;

		public ScriptStepStartStopRecording()
		{
			if (Application.Current != null)
				Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
			Description = "Start/Stop Recording/Monitoring";
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

			IsPass = true;
		}
	}
}
