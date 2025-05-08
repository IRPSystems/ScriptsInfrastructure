using DeviceCommunicators.Enums;
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceCommunicators.NI_6002;
using DeviceHandler.Interfaces;
using DeviceHandler.Models;
using Entities.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Services;
using Services.Services;
using System;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Reflection.Metadata;
using System.Threading;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepGetParamValue : ScriptStepBase, IScriptStepWithCommunicator, IScriptStepWithParameter
	{
		#region Properties and Fileds

		public DeviceParameterData Parameter { get; set; }
		[JsonIgnore]
		public DeviceCommunicator Communicator { get; set; }

		[JsonIgnore]
		public DevicesContainer DevicesContainer { get; set; }

		protected ManualResetEvent _waitForGet;
		private bool _isReceived;

		protected bool _isErrorOccured;

		#endregion Properties and Fileds

		#region Constructor

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

		#endregion Constructor

		#region Methods

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

				bool? isOK = HandleCalculatedParam(
					parameter,
					eolStepSummery);
				if (isOK != null)
					return isOK == true;

				WaitForDbcMessageService waitForDbcMessageService = new WaitForDbcMessageService();
				isOK = waitForDbcMessageService.Run(parameter, Communicator as MCU_Communicator);
				if (isOK != null)
				{
					IsPass = isOK == true;

					eolStepSummery.IsPass = IsPass;
					eolStepSummery.ErrorDescription = ErrorMessage;

					return IsPass;
				}


				Communicator.GetParamValue(parameter, GetValueCallback);

				int timeOut = 1000;
				if (parameter.CommunicationTimeout > 0)
				{
					timeOut = parameter.CommunicationTimeout;
				}

				bool isNotTimeout = _waitForGet.WaitOne(timeOut);
				_waitForGet.Reset();

				if (!isNotTimeout) // Timeout occored
				{
					IsPass = false;
					ErrorMessage += "Communication timeout.";
                    if (Communicator is NI6002_Communicator communicator)
						communicator.DisposeTask();
					eolStepSummery.IsPass = false;
					eolStepSummery.ErrorDescription = ErrorMessage;
					return IsPass;
				}

				if (IsPass && parameter.Value is double dValue && parameter.IsAbsolute)
					parameter.Value = Math.Abs(dValue);

				if (parameter.Value != null)
				{
					ExtractParameterValue(
						parameter,
						eolStepSummery);
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

		private void ExtractParameterValue(
			DeviceParameterData parameter,
			EOLStepSummeryData eolStepSummery)
		{
			double? value = null;
			if (parameter.Value is string str)
			{
				if (parameter is IParamWithDropDown dropDown &&
					dropDown.DropDown != null && dropDown.DropDown.Count > 0)
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

		private bool? HandleCalculatedParam(
			DeviceParameterData parameter,
			EOLStepSummeryData eolStepSummery)
		{
			if (!(parameter is ICalculatedParamete calculated))
				return null;

			calculated.DevicesList = DevicesContainer.DevicesFullDataList;

			ErrorMessage = "Failed to get the calculated parameter value.\r\n" +
				"\tParameter: " + parameter + "\r\n\r\n";

			calculated.Calculate();
			if (parameter.Value != null)
			{
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
					IsError = true;
					break;

				case CommunicatorResultEnum.ValueNotSet:
					ErrorMessage +=
						"Failed to set the value.";
                    IsError = true;
                    break;

				case CommunicatorResultEnum.Error:
					ErrorMessage +=
						"The device returned an error:\r\n" +
						resultDescription;
                    IsError = true;
                    break;

				case CommunicatorResultEnum.InvalidUniqueId:
					ErrorMessage +=
						"Invalud Unique ID was received from the Dyno.";
                    IsError = true;
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

		#endregion Methods
	}
}
