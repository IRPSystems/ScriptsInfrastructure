﻿
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Interfaces;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
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

		public string Name { get; set; }
		public virtual string Description { get; set; }

		public bool IsPass { get; set; }

		[JsonIgnore]
		public IScriptItem PassNext { get; set; }
		[JsonIgnore]
		public IScriptItem FailNext { get; set; }

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
				if(string.IsNullOrEmpty(UserTitle) == false &&
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
				if(_stopScriptStep != null) 
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


		public int ProgressPercentage { get; set; }
		protected int _totalNumOfSteps;
		protected int _stepsCounter;



		#endregion Properties and Fields

		#region Constructor

		public ScriptStepBase()
		{
			EOLStepSummerysList = new List<EOLStepSummeryData>();

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

		public virtual void AddToEOLSummary()
		{
            string _value = IsPass ? "Pass" : "Fail";

            string stepDescription = Description;
            if (!string.IsNullOrEmpty(UserTitle))
            {
                stepDescription = UserTitle + " - Result";
            }

            EOLStepSummeryData eolStepSummeryData = new EOLStepSummeryData(
                stepDescription,
				Description);
			eolStepSummeryData.IsPass = IsPass;
            eolStepSummeryData.ErrorDescription = ErrorMessage;
            EOLStepSummerysList.Add(eolStepSummeryData);
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

		#endregion Methods
	}
}
