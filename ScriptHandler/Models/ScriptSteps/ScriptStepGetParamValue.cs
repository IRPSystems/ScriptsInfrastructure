using DeviceCommunicators.Enums;
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceHandler.Interfaces;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using Services.Services;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using System.Threading;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepGetParamValue : ScriptStepBase, IScriptStepWithCommunicator, IScriptStepWithParameter
	{

		public DeviceParameterData Parameter { get; set; }
		[JsonIgnore]
		public DeviceCommunicator Communicator { get; set; }

		[JsonIgnore]
		public ObservableCollection<DeviceFullData> DevicesList { get; set; }

		protected ManualResetEvent _waitForGet;
		private bool _isReceived;

		protected bool _isErrorOccured;

		public ScriptStepGetParamValue()
		{
			try
			{
				Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
				_waitForGet = new ManualResetEvent(false);
				_isReceived = false;
			}
			catch { }
        }

		public bool SendAndReceive()
		{
			return SendAndReceive(Parameter);
		}


		public bool SendAndReceive(DeviceParameterData parameter)
		{
			if (parameter == null)
			{
				LoggerService.Error(this, "The parameter is not set");
				return false;
			}

			if (Communicator == null)
			{
				LoggerService.Error(this, "The communicator is not set");
				return false;
			}

			_waitForGet = new ManualResetEvent(false);

			_isReceived = true;

            ErrorMessage = "Failed to get the parameter value.\r\n" +
				"\tParameter: " + parameter + "\r\n\r\n";

			if(parameter is ICalculatedParamete calculated)
			{
				calculated.DevicesList = DevicesList;

				ErrorMessage = "Failed to get the calculated parameter value.\r\n" +
					"\tParameter: " + parameter + "\r\n\r\n";

				calculated.Calculate();
				if(parameter.Value != null) 
				{
					IsPass = true;
					return true;
				}
				else
				{
					IsPass = false;
					return false;
				}
			}

			Communicator.GetParamValue(parameter, GetValueCallback);

            bool isNotTimeout = _waitForGet.WaitOne(2000);
			_waitForGet.Reset();

			if (!isNotTimeout)
			{
				ErrorMessage += "Communication timeout.";
				return false;
			}

            return _isReceived;
        }

		private void GetValueCallback(DeviceParameterData param, CommunicatorResultEnum result, string resultDescription)
		{


            _waitForGet.Set();

			switch (result)
			{
				case CommunicatorResultEnum.NoResponse:
					ErrorMessage +=
						"No response was received from the device.";
					break;

				case CommunicatorResultEnum.ValueNotSet:
					ErrorMessage +=
						"Failed to set the value.";
					break;

				case CommunicatorResultEnum.Error:
					ErrorMessage +=
						"The device returned an error:\r\n" +
						resultDescription;
					break;

				case CommunicatorResultEnum.InvalidUniqueId:
					ErrorMessage +=
						"Invalud Unique ID was received from the Dyno.";
					break;
			}

			IsPass = _isReceived = (result == CommunicatorResultEnum.OK);
			if (!IsPass)
			{
				LoggerService.Inforamtion(
					this,
					"Failed to get value of " + param.Name + " - " + result + " - " + resultDescription);
			}
		}

		protected override void Stop()
		{

		}
	}
}
