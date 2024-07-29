
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
using ScriptRunner.Services;
using Microsoft.Win32;
using System.IO;
using DeviceCommunicators.General;
using ScriptHandler.Interfaces;
using ScriptHandler.Services;
using Services.Services;
using System.Collections.Generic;

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

		public string CANMessagesScriptPath { get; set; }


		#endregion Properties

		#region Fields


		private DevicesContainer _devicesContainer;
		private ScriptUserData _scriptUserData;

		#endregion Fields

		#region Constructor

		public CANMessageSenderViewModel(
			DevicesContainer devicesContainer,
			ScriptUserData scriptUserData)
		{
			_devicesContainer = devicesContainer;
			_scriptUserData = scriptUserData;

			BrowseCANMessagesScriptPathCommand = new RelayCommand(BrowseCANMessagesScriptPath);
			StartCANMessageSenderCommand = new RelayCommand(StartCANMessageSender);
			StopCANMessageSenderCommand = new RelayCommand(StopAllCANMessages);

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

		private void BrowseCANMessagesScriptPath()
		{
			string initDir = _scriptUserData.LastCANMessageScriptPath;
			if (string.IsNullOrEmpty(initDir))
				initDir = "";
			if (Directory.Exists(initDir) == false)
				initDir = "";


			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Test and Scripts Files|*.tst;*.scr";
			openFileDialog.InitialDirectory = initDir;
			bool? result = openFileDialog.ShowDialog();
			if (result != true)
				return;

			_scriptUserData.LastCANMessageScriptPath =
					Path.GetDirectoryName(openFileDialog.FileName);

			CANMessagesScriptPath = openFileDialog.FileName;
		}

		private void StartCANMessageSender()
		{
			try
			{

				string jsonString = System.IO.File.ReadAllText(CANMessagesScriptPath);

				JsonSerializerSettings settings = new JsonSerializerSettings();
				settings.Formatting = Formatting.Indented;
				settings.TypeNameHandling = TypeNameHandling.All;

				ScriptData script = JsonConvert.DeserializeObject(jsonString, settings) as
					ScriptData;

				GenerateProjectService generator = new GenerateProjectService();
				List<DeviceCommunicator> usedCommunicatorsList = new List<DeviceCommunicator>();
				GeneratedScriptData genScript = generator.GenerateScript(
					CANMessagesScriptPath,
					script,
					null,
					null,
					ref usedCommunicatorsList);

				foreach (IScriptItem item in genScript.ScriptItemsList)
				{
					if (!(item is ScriptStepCANMessage canMessage))
						continue;

					string sz = JsonConvert.SerializeObject(canMessage, settings);

					CANMessageRequest(sz);
				}
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to send script", ex);
			}
		}		

		private void Closing(CancelEventArgs e)
		{
			StopAllCANMessages();
		}

		private void CANMessageRequest(string messageStr)
		{
			if (string.IsNullOrEmpty(messageStr))
				return;

			if (Application.Current == null)
				return;

			Application.Current.Dispatcher.Invoke(() =>
			{

				
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

		public void SendUpdateMessage(ScriptStepCANMessageUpdate updateMessage)
		{
			try
			{
				SendStep(updateMessage);
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to update message", ex);
			}
		}

		public void SendStopMessage(ScriptStepCANMessageStop stopMessage)
		{
			try
			{
				SendStep(stopMessage);
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to stop message", ex);
			}
		}

		private void SendStep(ScriptStepBase step)
		{
			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.Formatting = Formatting.Indented;
			settings.TypeNameHandling = TypeNameHandling.All;
			string sz = JsonConvert.SerializeObject(step, settings);

			CANMessageRequest(sz);
		}

		#endregion Methods

		#region Commands

		public RelayCommand<CancelEventArgs> ClosingCommand { get; private set; }

		public RelayCommand BrowseCANMessagesScriptPathCommand { get; private set; }
		public RelayCommand CANMessageSenderCommand { get; private set; }
		public RelayCommand StartCANMessageSenderCommand { get; private set; }
		public RelayCommand StopCANMessageSenderCommand { get; private set; }

		#endregion Commands
	}
}
