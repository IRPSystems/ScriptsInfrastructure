

using DeviceCommunicators.General;
using DeviceHandler.Models;
using Newtonsoft.Json;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System.Collections.Generic;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepCANMessageUpdate : ScriptStepBase
	{
		[JsonIgnore]
		public ScriptStepCANMessage StepToUpdate { get; set; }

		public uint CANID { get; set; }

		public ulong Payload { get; set; }

		public int Interval { get; set; }
		public TimeUnitsEnum IntervalUnite { get; set; }

		public bool IsChangePayload { get; set; }
		public bool IsChangeInterval { get; set; }

		[JsonIgnore]
		public GeneratedTestData ParentProject { get; set; }

		public ScriptStepCANMessageUpdate()
		{
			Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
		}

		public override void Execute()
		{
			IsExecuted = true;
			if (StepToUpdate == null)
			{
				ErrorMessage = Description +":\r\nThe message to update is not set.";
				IsPass = false;
				return;
			}

			
			if(IsChangePayload)
				StepToUpdate.UpdatePayload(Payload);

			if (IsChangeInterval)
				StepToUpdate.UpdateInterval(Interval, IntervalUnite);

			IsPass = true;
		}


		protected override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			Payload = (sourceNode as ScriptNodeCANMessageUpdate).Payload.NumericValue;

			CANID = (sourceNode as ScriptNodeCANMessageUpdate).CANID;

			Interval = (sourceNode as ScriptNodeCANMessageUpdate).Interval;
			IntervalUnite = (sourceNode as ScriptNodeCANMessageUpdate).IntervalUnite;

			IsChangePayload = (sourceNode as ScriptNodeCANMessageUpdate).IsChangePayload;
			IsChangeInterval = (sourceNode as ScriptNodeCANMessageUpdate).IsChangeInterval;
		}
	}
}
