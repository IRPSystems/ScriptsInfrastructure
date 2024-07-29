
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
			public bool IsUseCRCCounter 
			{
				get => _isUseCRCCounter;
				set
				{
					_isUseCRCCounter = value;
					if (Message != null)
						Message.IsAddCRCCounter = value;
				}
			}

			private bool _isUseCRCCounter;
		}

		#region Properties
		
		public ObservableCollection<CANMessageForSenderData> CANMessagesList { get; set; }

		public string CANMessagesScriptPath { get; set; }


		#endregion Properties

		#region Fields


		private DevicesContainer _devicesContainer;
		private ScriptUserData _scriptUserData;


		private byte[] _srgCrc8_tab;
		private const uint _CRC8_START_VALUE = 0xFFU;
		private const uint _CRC8_XOR = 0xFFU;

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

			
			CRCInit();

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

		private void CRCInit()
		{
			_srgCrc8_tab = new byte[] {0x0, 0x1d, 0x3a, 0x27, 0x74, 0x69, 0x4e, 0x53,
									0xe8, 0xf5, 0xd2, 0xcf, 0x9c, 0x81, 0xa6, 0xbb,
									0xcd, 0xd0, 0xf7, 0xea, 0xb9, 0xa4, 0x83, 0x9e,
									0x25, 0x38, 0x1f, 0x2, 0x51, 0x4c, 0x6b, 0x76,
									0x87, 0x9a, 0xbd, 0xa0, 0xf3, 0xee, 0xc9, 0xd4,
									0x6f, 0x72, 0x55, 0x48, 0x1b, 0x6, 0x21, 0x3c,
									0x4a, 0x57, 0x70, 0x6d, 0x3e, 0x23, 0x4, 0x19,
									0xa2, 0xbf, 0x98, 0x85, 0xd6, 0xcb, 0xec, 0xf1,
									0x13, 0xe, 0x29, 0x34, 0x67, 0x7a, 0x5d, 0x40,
									0xfb, 0xe6, 0xc1, 0xdc, 0x8f, 0x92, 0xb5, 0xa8,
									0xde, 0xc3, 0xe4, 0xf9, 0xaa, 0xb7, 0x90, 0x8d,
									0x36, 0x2b, 0xc, 0x11, 0x42, 0x5f, 0x78, 0x65,
									0x94, 0x89, 0xae, 0xb3, 0xe0, 0xfd, 0xda, 0xc7,
									0x7c, 0x61, 0x46, 0x5b, 0x8, 0x15, 0x32, 0x2f,
									0x59, 0x44, 0x63, 0x7e, 0x2d, 0x30, 0x17, 0xa,
									0xb1, 0xac, 0x8b, 0x96, 0xc5, 0xd8, 0xff, 0xe2,
									0x26, 0x3b, 0x1c, 0x1, 0x52, 0x4f, 0x68, 0x75,
									0xce, 0xd3, 0xf4, 0xe9, 0xba, 0xa7, 0x80, 0x9d,
									0xeb, 0xf6, 0xd1, 0xcc, 0x9f, 0x82, 0xa5, 0xb8,
									0x3, 0x1e, 0x39, 0x24, 0x77, 0x6a, 0x4d, 0x50,
									0xa1, 0xbc, 0x9b, 0x86, 0xd5, 0xc8, 0xef, 0xf2,
									0x49, 0x54, 0x73, 0x6e, 0x3d, 0x20, 0x7, 0x1a,
									0x6c, 0x71, 0x56, 0x4b, 0x18, 0x5, 0x22, 0x3f,
									0x84, 0x99, 0xbe, 0xa3, 0xf0, 0xed, 0xca, 0xd7,
									0x35, 0x28, 0xf, 0x12, 0x41, 0x5c, 0x7b, 0x66,
									0xdd, 0xc0, 0xe7, 0xfa, 0xa9, 0xb4, 0x93, 0x8e,
									0xf8, 0xe5, 0xc2, 0xdf, 0x8c, 0x91, 0xb6, 0xab,
									0x10, 0xd, 0x2a, 0x37, 0x64, 0x79, 0x5e, 0x43,
									0xb2, 0xaf, 0x88, 0x95, 0xc6, 0xdb, 0xfc, 0xe1,
									0x5a, 0x47, 0x60, 0x7d, 0x2e, 0x33, 0x14, 0x9,
									0x7f, 0x62, 0x45, 0x58, 0xb, 0x16, 0x31, 0x2c,
									0x97, 0x8a, 0xad, 0xb0, 0xe3, 0xfe, 0xd9, 0xc4};
		}

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
				IsUseCRCCounter = false,
			};
			CANMessagesList.Add(data);

			if (_devicesContainer.TypeToDevicesFullData.ContainsKey(DeviceTypesEnum.MCU) == false)
				return;

			DeviceFullData mcuDevice = _devicesContainer.TypeToDevicesFullData[DeviceTypesEnum.MCU];
			canMessage.Communicator = mcuDevice.DeviceCommunicator;
			canMessage.IsAddCRCCounter = data.IsUseCRCCounter;
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






		private byte Crc_CalculateCRC8(byte[] pCrcData, uint crcLength, byte Crc_StartValue8, bool bCrcIsFirstCall)
		{


			byte crc = 0;    /* Default return value if NULL pointer */

			if (pCrcData != null)
			{
				crc = ((bCrcIsFirstCall == true) ? (byte)_CRC8_START_VALUE : Crc_StartValue8);

				
				uint byteIndex;

				for (byteIndex = 0; byteIndex < crcLength; ++byteIndex)
				{
					crc = _srgCrc8_tab[crc ^ pCrcData[byteIndex]];
				}
				
				crc = (byte)(crc ^ _CRC8_XOR);
			}

			return crc;
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
