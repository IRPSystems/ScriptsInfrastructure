using DeviceCommunicators.Enums;
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceHandler.Interfaces;
using DeviceHandler.Models.DeviceFullDataModels;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using Services.Services;
using System;
using System.Collections.ObjectModel;
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

		public bool SendAndReceive(out EOLStepSummeryData eolStepSummery)
		{
			return SendAndReceive(
				Parameter,
				out eolStepSummery);
		}


		public bool SendAndReceive(
			DeviceParameterData parameter,
			out EOLStepSummeryData eolStepSummery)
		{
			eolStepSummery = null;

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

			eolStepSummery = new EOLStepSummeryData(
				GetOnlineDescription(),
				$"Get the value of parameter {parameter.Name}");

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

					//if(parameter.IsAbsolute)
					//	parameter.Value = Math.Abs((double)parameter.Value);
					eolStepSummery.Value = parameter.Value.ToString();
					eolStepSummery.IsPass = true;
					IsPass = true;
					return true;
				}
				else
				{
					eolStepSummery.IsPass = false;
					eolStepSummery.ErrorDescription = ErrorMessage;
					IsPass = false;
					return false;
				}
			}

			Communicator.GetParamValue(parameter, GetValueCallback);

            bool isNotTimeout = _waitForGet.WaitOne(2000);
			_waitForGet.Reset();

			if (!isNotTimeout)
			{
				IsPass = false;
				ErrorMessage += "Communication timeout.";
				eolStepSummery.IsPass = false;
				eolStepSummery.ErrorDescription = ErrorMessage;
				return IsPass;
			}

			if(IsPass && parameter.Value is double dValue && parameter.IsAbsolute)
				parameter.Value = Math.Abs(dValue);

			eolStepSummery.Value = parameter.Value.ToString();
			eolStepSummery.IsPass = true;

			return _isReceived;
        }

		private string GetOnlineDescription()
		{
			string description = "Get value of ";
			if (Parameter != null)
				description += $"\"{Parameter.Name}\"";
			return description;
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
