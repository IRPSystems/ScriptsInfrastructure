


using DeviceHandler.Models;
using ScriptHandler.Enums;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeNotification : ScriptNodeBase
	{
		private string _notification;
		public string Notification 
		{
			get => _notification;
			set
			{
				_notification = value;
				OnPropertyChanged("Notification");
			}
		}

		private NotificationLevelEnum _notificationLevel;
		public NotificationLevelEnum NotificationLevel 
		{
			get => _notificationLevel;
			set
			{ 
				_notificationLevel = value;

				OnPropertyChanged("NotificationLevel");
			}
		}

		public override string Description
		{
			get
			{
				return "Notification - ID:" + ID;
			}
		}

		public ScriptNodeNotification()
		{
			Name = "Notification";

		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (string.IsNullOrEmpty(Notification))
				return true;

			return false;
		}
	}
}
