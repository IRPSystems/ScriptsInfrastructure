
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.Enums;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Enums;
using DeviceHandler.Services;
using Entities.Models;
using ScriptHandler.Models;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ScriptRunner.Services
{
	public class SaftyOfficerService: ObservableObject
	{
		#region Fields

		private CancellationTokenSource _cancellationTokenSource;
		private CancellationToken _cancellationToken;

		private MotorSettingsData _selectedMotor;
		private ControllerSettingsData _selectedController;
		private ParametersRepositoryService _parametersRepository;

		private MCU_DeviceData _mcu_Device;


		//private ManualResetEvent _waitGetCallback;

		private List<(DateTime,string, double?)> _parametersAndValueList;
		private bool _isTest;
		//private bool _isDoAbort;

		

		#endregion Fields

		#region Properties
		public string SaftyOfficerStatus { get; set; }

		public bool IsRunning { get; set; }

		#endregion Properties

		#region Constructor

		public SaftyOfficerService()
		{
			IsRunning = false;
		}

		#endregion Constructor

		#region Methods

		public void Start(
			MotorSettingsData selectedMotor,
			ControllerSettingsData selectedController,
			MCU_DeviceData mcu_Device,
			ParametersRepositoryService parametersRepository,
			bool isTest = false)
		{
			LoggerService.Inforamtion(this, "Starting security officer");

			_selectedMotor = selectedMotor;
			_selectedController = selectedController;
			_mcu_Device = mcu_Device;
			_parametersRepository = parametersRepository;

			SaftyOfficerStatus = "";

			_cancellationTokenSource = new CancellationTokenSource();
			_cancellationToken = _cancellationTokenSource.Token;

			SaftyOfficerStatus = "Started";
			StatusReportEvent?.Invoke();

			_isTest = isTest;

			if(_isTest)
				_parametersAndValueList = new List<(DateTime, string, double?)>();

			foreach (ParameterValueData data in _selectedMotor.StatusParameterValueList)
			{
				DeviceParameterData param = 
					_mcu_Device.MCU_FullList.ToList().Find((p) => 
												((MCU_ParamData)p).Cmd != null && 
												((MCU_ParamData)p).Cmd.ToLower() == data.ParameterName.ToLower());
				if(param != null)
					parametersRepository.Add(param, RepositoryPriorityEnum.High, GetCallback);
			}

			foreach (ParameterValueData data in _selectedController.StatusParameterValueList)
			{
				DeviceParameterData param =
					_mcu_Device.MCU_FullList.ToList().Find((p) =>
												((MCU_ParamData)p).Cmd != null &&
												((MCU_ParamData)p).Cmd.ToLower() == data.ParameterName.ToLower());
				if (param != null)
					parametersRepository.Add(param, RepositoryPriorityEnum.High, GetCallback);
			}

			IsRunning = true;
		}

		public void Stop()
		{
			if (!IsRunning)
				return;

			IsRunning  = false;

			LoggerService.Inforamtion(this, "Stoping security officer");

			if (_cancellationTokenSource != null)
				_cancellationTokenSource.Cancel();

			SaftyOfficerStatus = "Stopped";

			if (_selectedMotor != null)
			{
				foreach (ParameterValueData data in _selectedMotor.StatusParameterValueList)
				{
					DeviceParameterData param =
						_mcu_Device.MCU_FullList.ToList().Find((p) => 
									((MCU_ParamData)p).Cmd != null && 
									((MCU_ParamData)p).Cmd.ToLower() == data.ParameterName.ToLower());
					if (param != null)
						_parametersRepository.Remove(param, GetCallback);
				}
			}

			if (_selectedController != null)
			{
				foreach (ParameterValueData data in _selectedController.StatusParameterValueList)
				{
					DeviceParameterData param =
						_mcu_Device.MCU_FullList.ToList().Find((p) =>
									((MCU_ParamData)p).Cmd != null &&
									((MCU_ParamData)p).Cmd.ToLower() == data.ParameterName.ToLower());
					if (param != null)
						_parametersRepository.Remove(param, GetCallback);
				}
			}
		}

		private void GetCallback(DeviceParameterData param, CommunicatorResultEnum result, string resultDescription)
		{
			LoggerService.Debug(this, 
				"Received call back - Result: " + result +
				" - result description: " + resultDescription);

			if (_cancellationToken.IsCancellationRequested)
			{
				//_waitGetCallback.Set();
				return;
			}

			string message = "";
			string saftyOfficerStatus = "";
			if (result != CommunicatorResultEnum.OK)
			{
				saftyOfficerStatus = "Abort\r\n";
				switch (result)
				{
					case CommunicatorResultEnum.NoResponse:
						saftyOfficerStatus +=
							"No response was received from the device.";
						break;

					case CommunicatorResultEnum.ValueNotSet:
						saftyOfficerStatus +=
							"Failed to set the value.";
						break;

					case CommunicatorResultEnum.Error:
						saftyOfficerStatus +=
							"The device returned an error:\r\n" +
							resultDescription;
						break;

					case CommunicatorResultEnum.InvalidUniqueId:
						saftyOfficerStatus +=
							"Invalud Unique ID was received from the Dyno.";
						break;
				}
				StatusReportEvent?.Invoke();
				message = "Failed to get the value of \"" + param.Name + "\". Communication problem\r\n" + saftyOfficerStatus;
				if (_isTest)
				{
					_parametersAndValueList.Add((DateTime.Now, "received null", null));
				}
			}
			else if (param.Value != null) 
			{
				if (param.Name == "Bus Current") { }
				double value = Convert.ToDouble(param.Value);
				//value = value / (param as MCU_ParamData).Scale;

				string errorStr;
				ParameterValueData expectedParam = IsValueValue(
					param as MCU_ParamData, 
					out errorStr);

				if (expectedParam != null && value >= expectedParam.Value)
				{
					saftyOfficerStatus = "Abort - Invalid Value";
					StatusReportEvent?.Invoke();
					message =
						"Security Oficer Error:\r\n" +
						"\"" + param.Name + "\" = " + value + "\r\n" +
						"Should be less than " + expectedParam.Value;

					if (_isTest)
					{
						_parametersAndValueList.Add((DateTime.Now, param.Name, Convert.ToDouble(param.Value)));
					}
				}

				else if (_isTest)
				{
					saftyOfficerStatus = "Valid value";
					StatusReportEvent?.Invoke();
					_parametersAndValueList.Add((DateTime.Now, param.Name, (double?)param.Value));
				}
			}
			else { }

			if (!string.IsNullOrEmpty(message))
			{
				LoggerService.Inforamtion(this,
					"Aborting with description: " + message);

				SaftyOfficerStatus = saftyOfficerStatus;
				Abort(message);
			}

			//_waitGetCallback.Set();
		}

		private ParameterValueData IsValueValue(
			MCU_ParamData param,
			out string errorStr)
		{
			errorStr = "";
			ParameterValueData data = null;

			if (param.Name == "Current U" ||
				param.Name == "Current V" ||
				param.Name == "Current W")
			{
				data = _selectedMotor.CommandParameterValueList.Find((p) =>
						p.ParameterName.Contains("Motor Max Current Command"));
			}
			else if (param.Name == "Motor Temperature")
			{
				data = _selectedMotor.CommandParameterValueList.Find((p) =>
						p.ParameterName.Contains("Motor Temperature - Max Cut Derating"));
			}
			else if (param.Name == "Speed - RPM")
			{
				data = _selectedMotor.CommandParameterValueList.Find((p) =>
						p.ParameterName.Contains("Motor Speed Control Max"));
			}


			else if (param.Name == "MCU Temperature")
			{
				data = _selectedController.CommandParameterValueList.Find((p) =>
						p.ParameterName.Contains("MCU Over Temperature"));
			}
			else if (param.Name == "Bus Current")
			{
				data = _selectedController.CommandParameterValueList.Find((p) => 
						p.ParameterName.Contains("Battery Over Current"));
			}
			else if (param.Name == "Vbus")
			{
				data = _selectedController.CommandParameterValueList.Find((p) =>
						p.ParameterName.Contains("Battery Over Voltage"));
			}


			else
			{
				errorStr = "Invalid parameter";
				return null;
			}

			if (data == null) 
			{
				errorStr = "Expected value was not found";
				return null;
			}

			return data;

		}


		private void Abort(string message)
		{
			_cancellationTokenSource.Cancel();
			//_waitGetCallback.Set();
			AbortScriptEvent?.Invoke(message);
		}

        #endregion Methods

        #region Events

        public event Action<string> AbortScriptEvent;
		public event Action StatusReportEvent;

		#endregion Events
	}
}
