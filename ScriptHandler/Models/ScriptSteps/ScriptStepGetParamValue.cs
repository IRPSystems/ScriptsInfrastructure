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
		public DevicesContainer DevicesContainer { get; set; }

		protected ManualResetEvent _waitForGet;
		private bool _isReceived;

		protected bool _isErrorOccured;

		public ScriptStepGetParamValue()
		{
			try
			{
				if (Application.Current != null)
					Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
				_waitForGet = new ManualResetEvent(false);
				_isReceived = false;
			}
			catch { }
        }

		public bool SendAndReceive(
			out EOLStepSummeryData eolStepSummery,
			string parentStepDescription = null)
		{
			return SendAndReceive(
				Parameter,
				out eolStepSummery,
				parentStepDescription);
		}


		public bool SendAndReceive(
			DeviceParameterData parameter,
			out EOLStepSummeryData eolStepSummery,
			string parentStepDescription = null)
		{
			try
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
					parentStepDescription,
					GetOnlineDescription(),
					this);

				_waitForGet = new ManualResetEvent(false);

				_isReceived = true;

				ErrorMessage = "Failed to get the parameter value.\r\n" +
					"\tParameter: " + parameter + "\r\n\r\n";

				if (parameter is ICalculatedParamete calculated)
				{
					calculated.DevicesList = DevicesContainer.DevicesFullDataList;

					ErrorMessage = "Failed to get the calculated parameter value.\r\n" +
						"\tParameter: " + parameter + "\r\n\r\n";

					calculated.Calculate();
					if (parameter.Value != null)
					{

						//if(parameter.IsAbsolute)
						//	parameter.Value = Math.Abs((double)parameter.Value);
						eolStepSummery.TestValue = (double)parameter.Value;
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

				int timeOut = 1000;
				if (parameter.CommunicationTimeout > 0)
				{
					timeOut = parameter.CommunicationTimeout;
				}

				bool isNotTimeout = _waitForGet.WaitOne(timeOut);
				_waitForGet.Reset();

				if (!isNotTimeout)
				{
					IsPass = false;
					ErrorMessage += "Communication timeout.";
					eolStepSummery.IsPass = false;
					eolStepSummery.ErrorDescription = ErrorMessage;
					return IsPass;
				}

				if (IsPass && parameter.Value is double dValue && parameter.IsAbsolute)
					parameter.Value = Math.Abs(dValue);

				if (parameter.Value != null)
				{
					double? value = null;
					if (parameter.Value is string str)
					{
						if(parameter is IParamWithDropDown dropDown &&
							dropDown.DropDown != null)
						{
							DropDownParamData dd = dropDown.DropDown.Find((d) => d.Name == str);
							if (dd != null)
								str = dd.Value;
						}

						double d;
						bool res = double.TryParse(str, out d);
						if (res)
							value = d;
					}
					else
					{
						double d;
						bool res = double.TryParse(parameter.Value.ToString(), out d);
						if (res)
							value = d;
					}
					if (value != null) 
						eolStepSummery.TestValue = value;
				}

				eolStepSummery.IsPass = true;
				eolStepSummery.ErrorDescription = ErrorMessage;

				return _isReceived;
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to get", ex);
				eolStepSummery = null;
				IsPass = false;
				ErrorMessage = $"Exception during failed\r\n{ex.Message}";
				return false;
			}
        }

		private string GetOnlineDescription()
		{
			//if(!string.IsNullOrEmpty(UserTitle)) 
			//	return UserTitle;
			string description = "Get value of ";
			if (Parameter != null)
				description += $"{Parameter.Name}";
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
