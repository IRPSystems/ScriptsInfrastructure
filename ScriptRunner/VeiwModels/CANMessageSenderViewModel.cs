
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Enums;
using ScriptHandler.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System;
using Newtonsoft.Json;
using DeviceHandler.Models;

namespace ScriptRunner.ViewModels
{
	public class CANMessageSenderViewModel: ObservableObject
	{
		public enum CANMessageForSenderStateEnum { None, Sending, Updated, Stopped }

		public class CANMessageForSenderData : ObservableObject
		{
			public ScriptStepCANMessage Message { get; set; }
			public CANMessageForSenderStateEnum State { get; set; }
		}

		#region Properties

		
		public ObservableCollection<CANMessageForSenderData> CANMessagesList { get; set; }


		#endregion Properties

		#region Fields


		private DevicesContainer _devicesContainer;

		#endregion Fields

		#region Constructor

		public CANMessageSenderViewModel(DevicesContainer devicesContainer)
		{
			_devicesContainer = devicesContainer;

			try
			{
				ClosingCommand = new RelayCommand<CancelEventArgs>(Closing);

				

				CANMessagesList = new ObservableCollection<CANMessageForSenderData>();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to init\r\n\r\n" + ex);
			}
		}

		#endregion Constructor

		#region Methods

		


		private void Closing(CancelEventArgs e)
		{
			ClearCANMessageList();
		}


		public void CANMessageRequest(string messageStr)
		{
			if (string.IsNullOrEmpty(messageStr))
				return;

			if (Application.Current == null)
				return;

			Application.Current.Dispatcher.Invoke(() =>
			{

				if (messageStr == "Clear")
				{
					ClearCANMessageList();
					return;
				}

				try
				{

					JsonSerializerSettings settings = new JsonSerializerSettings();
					settings.Formatting = Formatting.Indented;
					settings.TypeNameHandling = TypeNameHandling.All;
					ScriptStepBase baseStep =
						JsonConvert.DeserializeObject(messageStr, settings) as ScriptStepBase;

					if (baseStep is ScriptStepCANMessage canMessage)
					{
						HandleCANMessage(canMessage);
					}
					else if (baseStep is ScriptStepCANMessageUpdate update)
					{
						HandleCANMessageUpdate(update);
					}
					else if (baseStep is ScriptStepCANMessageStop stop)
					{
						HandleCANMessageStop(stop);
					}

				}
				catch (Exception ex)
				{
					MessageBox.Show("Failed handling received message\r\n\r\n" + ex, "Error");
				}
			});
		}

		private void HandleCANMessage(ScriptStepCANMessage canMessage)
		{
			bool isCANMessageExist = IsCANMessageExist(canMessage.NodeId);
			if (isCANMessageExist)
			{
				return;
			}

			CANMessageForSenderData data = new CANMessageForSenderData()
			{
				Message = canMessage,
				State = CANMessageForSenderStateEnum.Sending,
			};
			CANMessagesList.Add(data);

			if (_devicesContainer.TypeToDevicesFullData.ContainsKey(DeviceTypesEnum.MCU) == false)
				return;

			DeviceFullData mcuDevice = _devicesContainer.TypeToDevicesFullData[DeviceTypesEnum.MCU];
			canMessage.Communicator = mcuDevice.DeviceCommunicator;
			canMessage.Execute();
		}

		private void HandleCANMessageUpdate(ScriptStepCANMessageUpdate update)
		{
			foreach (CANMessageForSenderData data in CANMessagesList)
			{
				if (data.Message == null)
					continue;

				if (data.Message.NodeId == update.CANID)
				{
					data.State = CANMessageForSenderStateEnum.Updated;

					update.StepToUpdate = data.Message;
					update.Execute();
				}
			}
		}

		private void HandleCANMessageStop(ScriptStepCANMessageStop stop)
		{
			foreach (CANMessageForSenderData data in CANMessagesList)
			{
				if (data.Message == null)
					continue;

				if (data.Message.NodeId == stop.CANID)
				{
					data.State = CANMessageForSenderStateEnum.Stopped;

					stop.StepToStop = data.Message;
					stop.Execute();
				}
			}
		}

		public void StopAllCANMessages()
		{
			foreach (CANMessageForSenderData data in CANMessagesList)
			{
				if (data.Message == null)
					continue;

				data.Message.StopContinuous();
			}
		}

		private void ClearCANMessageList()
		{
			StopAllCANMessages();

			CANMessagesList.Clear();
		}

		private bool IsCANMessageExist(uint id)
		{
			foreach (CANMessageForSenderData data in CANMessagesList)
			{
				if (data.Message == null)
					continue;

				if (data.Message.NodeId == id)
					return true;
			}

			return false;
		}

		#endregion Methods

		#region Commands

		public RelayCommand<CancelEventArgs> ClosingCommand { get; private set; }
		
		#endregion Commands
	}
}
