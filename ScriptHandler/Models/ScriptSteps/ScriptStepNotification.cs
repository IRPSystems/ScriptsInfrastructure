
using DeviceCommunicators.General;
using DeviceHandler.Models;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepNotification : ScriptStepBase, IScriptStepWithWaitForUser
	{
		public string Notification { get; set; }
		public NotificationLevelEnum NotificationLevel { get; set; }


		public ScriptStepNotification()
		{
			Template = Application.Current.MainWindow.FindResource("NotificationTemplate") as DataTemplate;
		}

		public override void Execute()
		{
			IsPass = true;
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (string.IsNullOrEmpty(Notification))
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
			NotificationLevel = (sourceNode as ScriptNodeNotification).NotificationLevel;
			Notification = (sourceNode as ScriptNodeNotification).Notification;
		}
	}
}
