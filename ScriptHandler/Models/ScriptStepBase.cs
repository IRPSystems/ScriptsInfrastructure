﻿
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Interfaces;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Enums;
using Newtonsoft.Json;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptSteps;
using ScriptHandler.Services;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ScriptHandler.Models
{
	public class ScriptStepBase : ObservableObject, IScriptItem
	{
		#region Properties and Fields

		public string UserTitle { get; set; }

        public TimeSpan ExecutionTime { get;  set; }

        public string Name { get; set; }
		public virtual string Description { get; set; }
		public virtual string OperatorErrorDescription { get; set; }

		public bool IsPass { get; set; }

		[JsonIgnore]
		public IScriptItem PassNext { get; set; }
		[JsonIgnore]
		public IScriptItem FailNext { get; set; }

		public bool? IsError;

		public string PassNextDescription { get; set; }
		public string FailNextDescription { get; set; }

		[JsonIgnore]
		public DataTemplate Template { get; set; }

		private string _errorMessage;
		public virtual string ErrorMessage
		{
			get
			{
				string msg = string.Empty;
				if (string.IsNullOrEmpty(UserTitle) == false &&
					(_errorMessage != null && _errorMessage.Contains(UserTitle) == false))
				{
					msg = $"{UserTitle}\r\n";
				}

				msg += _errorMessage;
				return msg;
			}
			set { _errorMessage = value; }
		}

		private StopScriptStepService _stopScriptStep;
		public StopScriptStepService StopScriptStep
		{
			get => _stopScriptStep;
			set
			{
				_stopScriptStep = value;
				if (_stopScriptStep != null)
				{
					_stopScriptStep.StopEvent += Stop;
				}
			}
		}

		public Brush BorderBrush { get; set; }

		private SciptStateEnum _stepState;
		public SciptStateEnum StepState
		{
			get => _stepState;
			set
			{
				_stepState = value;
				OnPropertyChanged(nameof(StepState));
			}
		}

		public int ID { get; set; }

		public EOLReportsSelectionData EOLReportsSelectionData { get; set; }
		public List<EOLStepSummeryData> EOLStepSummerysList { get; set; }
		public CommSendResLog CommSendResLog { get; set; }

		public string SubScriptName { get; set; }
		public string TestName { get; set; }

		public int ProgressPercentage { get; set; }
		protected int _totalNumOfSteps;
		protected int _stepsCounter;

		public bool IsExecuted;


		#endregion Properties and Fields

		#region Constructor

		public ScriptStepBase()
		{
			EOLStepSummerysList = new List<EOLStepSummeryData>();
			IsExecuted = false;
		}

		#endregion Constructor

		#region Methods

		public virtual void Execute()
		{

        }

			
		protected virtual void Stop()
		{

		}

		public virtual void Resume()
		{
			Execute();
		}

		public virtual void PopulateSendResponseLog(
			string stepName, 
			string tool, 
			string Parameter, 
			DeviceTypesEnum device ,
			CommSendResLog commSendResLog)
		{
			try
			{
                if (commSendResLog == null)
                {
                    return;
                }
                CommSendResLog = new CommSendResLog();
                CommSendResLog.StepName = stepName;
                CommSendResLog.Tool = tool;
                CommSendResLog.ParamName = Parameter;
                CommSendResLog.Device = device.ToString();
				if (commSendResLog != null)
				{
					CommSendResLog.SendCommand = commSendResLog.SendCommand;
					CommSendResLog.ReceivedValue = commSendResLog.ReceivedValue;
					CommSendResLog.CommErrorMsg = commSendResLog.CommErrorMsg;
					CommSendResLog.NumberOfTries = commSendResLog.NumberOfTries;
                    CommSendResLog.timeStamp = commSendResLog.timeStamp;
                }
            }
			catch (Exception ex)
			{
				LoggerService.Error(this, "Error while updating send res log: " + ex.InnerException.Message);
			}
		}

		public virtual void AddToEOLSummary(double? testValue = null)
		{
			string _value = IsPass ? "Pass" : "Fail";

			string stepDescription = Description;
			if (!string.IsNullOrEmpty(UserTitle))
				stepDescription = UserTitle;


			EOLStepSummeryData eolStepSummeryData = new EOLStepSummeryData(
				"",
				stepDescription,
				this);
			eolStepSummeryData.IsPass = IsPass;
			eolStepSummeryData.ErrorDescription = ErrorMessage;
			eolStepSummeryData.TestValue = testValue;
            string unit = "";
			if (this is IScriptStepWithParameter withParameter
				&& withParameter.Parameter != null)
			{
				unit = withParameter.Parameter.Units;
			}
			eolStepSummeryData.Units = unit;

			EOLStepSummerysList.Add(eolStepSummeryData);
		}

		public virtual List<DeviceTypesEnum> GetUsedDevices()
		{
			return null;
		}

		public virtual bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			return false;
		}

		public virtual void Generate_Base(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			EOLReportsSelectionData = sourceNode.EOLReportsSelectionData;
			UserTitle = sourceNode.UserTitle;

			Generate(
				sourceNode,
				stepNameToObject,
				ref usedCommunicatorsList,
				generateService,
				devicesContainer);
		}

		protected virtual void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
		}

		public virtual void GetRealParamAfterLoad(
			DevicesContainer devicesContainer)
		{
			if (this is IScriptStepWithParameter withParameter)
			{
				if (withParameter.Parameter is ICalculatedParamete)
					return;

				withParameter.Parameter = GetRealParam(
					withParameter.Parameter,
					devicesContainer);
			}
		}

		protected DeviceParameterData GetRealParam(
			DeviceParameterData originalParam,
			DevicesContainer devicesContainer)
		{
			if (originalParam == null)
				return null;

			if (devicesContainer.TypeToDevicesFullData.ContainsKey(originalParam.DeviceType) == false)
				return null;

			DeviceFullData deviceFullData =
				devicesContainer.TypeToDevicesFullData[originalParam.DeviceType];
			if (deviceFullData == null)
				return null;

			DeviceParameterData actualParam = null;
			if (originalParam is MCU_ParamData mcuParam)
			{
				actualParam =
					deviceFullData.Device.ParemetersList.ToList().Find((p) =>
						((MCU_ParamData)p).Cmd == mcuParam.Cmd);
			}
			else
			{
				actualParam =
					deviceFullData.Device.ParemetersList.ToList().Find((p) =>
						p.Name == originalParam.Name);
			}

			return actualParam;
		}

		public virtual List<string> GetReportHeaders()
		{
			List<string> headers = new List<string>();

			string stepDescription = Description;
			if (string.IsNullOrEmpty(UserTitle) == false)
				stepDescription += $";\r\n{UserTitle}";

			stepDescription = FixStringService.GetFixedString(stepDescription);

			string testName = FixStringService.GetFixedString(TestName);
			string subScriptName = FixStringService.GetFixedString(SubScriptName);

			string description =
				$"{testName};\r\n{subScriptName};\r\n{stepDescription};";

			description = $"\"{description}\"";
			headers.Add(description);

			return headers;
		}

		public virtual List<string> GetReportValues()
		{
			List<string> values = new List<string>();

			string stepState = "FAILED";
			if (!IsExecuted)
				stepState = "Not Executed";

			else if (IsPass)
			{
				if (HasValueProperty(out var value))
					stepState = Convert.ToString(value);
				else
					stepState = "PASSED";
            }


            IsExecuted = false;

			values.Add(stepState);

			return values;
		}

		#endregion Methods

		private bool HasValueProperty(out object value)
		{
			var propertyInfo = this.GetType().GetProperty("Value");
			if (propertyInfo != null)
			{
				value = propertyInfo.GetValue(this);
				return true;
			}
			value = null;
			return false;
		}
	}
 }
