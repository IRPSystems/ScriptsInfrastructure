
using DeviceCommunicators.Enums;
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepIncrementValue : ScriptStepGetParamValue
	{
		private bool _isStoped;
		private ScriptStepSetParameter _setParameter;



		public double IncrementValue { get; set; }


		public ScriptStepIncrementValue()
		{
			if (Application.Current != null)
				Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
			_setParameter = new ScriptStepSetParameter();
			_totalNumOfSteps = 3;
		}



		public override void Execute()
		{
			_isStoped = false;
			IsPass = true;
			_isExecuted = true;

			_stepsCounter = 1;

			_setParameter.Communicator = Communicator;
			_setParameter.Parameter = Parameter;

			ErrorMessage = "";

			EOLStepSummeryData eolStepSummeryData;
			bool isOK = SendAndReceive(out eolStepSummeryData);
			EOLStepSummerysList.Add(eolStepSummeryData);
			if (!isOK)
			{
				LoggerService.Inforamtion(this, "Failed SendAndReceive");
				IsPass = false;
				return;
			}

			if (_isStoped)
				return;

			_stepsCounter++;

			double value = Convert.ToDouble(Parameter.Value);
			value += IncrementValue;

			

			_setParameter.Value = value;
			_setParameter.Execute();
			EOLStepSummerysList.AddRange(_setParameter.EOLStepSummerysList);
			if (!_setParameter.IsPass)
			{
				ErrorMessage += _setParameter.ErrorMessage;
				IsPass = false;
			}

			//_waitForGet.Reset();
			//Communicator.SetParamValue(Parameter, value, GetValueCallback);

			//bool isNotTimeOut = _waitForGet.WaitOne(2000);
			//_waitForGet.Reset();
			//if(!isNotTimeOut)
			//	IsPass = false;

			AddToEOLSummary();
		}

		private void GetValueCallback(DeviceParameterData param, CommunicatorResultEnum result, string resultDescription)
		{			
			IsPass = result == CommunicatorResultEnum.OK;
			if (!IsPass) { }
			_waitForGet.Set();
		}

		protected override void Stop()
		{
			_isStoped = true;
			_waitForGet.Set();
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Parameter == null)
				return true;

			return false;
		}

		protected override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			Parameter = (sourceNode as ScriptNodeIncrementValue).Parameter;
			IncrementValue = (sourceNode as ScriptNodeIncrementValue).IncrementValue;
		}

		public override List<string> GetReportHeaders()
		{
			List<string> headers = base.GetReportHeaders();

			string stepDescription = headers[0].Trim('\"');

			string description =
				$"{stepDescription}\r\nGet {Parameter.Name}";
			headers.Add($"\"{description}\"");


			return headers;
		}

		public override List<string> GetReportValues()
		{
			List<string> values = base.GetReportValues();

			EOLStepSummeryData stepSummeryData =
				EOLStepSummerysList.Find((e) =>
					!string.IsNullOrEmpty(e.Description) && e.Description.Contains(Parameter.Name));

			if (stepSummeryData != null)
				values.Add(stepSummeryData.TestValue.ToString());
			else
				values.Add("");

			_isExecuted = false;

			return values;
		}
	}
}
