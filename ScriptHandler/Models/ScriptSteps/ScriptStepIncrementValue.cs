
using DeviceCommunicators.Enums;
using DeviceCommunicators.General;
using DeviceHandler.Models;
using Entities.Models;
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



		public double IncrementValue { get; set; }


		public ScriptStepIncrementValue()
		{
			Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
		}



		public override void Execute()
		{
			_isStoped = false;
			IsPass = true;

			ErrorMessage = "";

			bool isOK = SendAndReceive();
			if (!isOK)
			{
				LoggerService.Inforamtion(this, "Failed SendAndReceive");
				IsPass = false;
				return;
			}

			if (_isStoped)
				return;

			double value = Convert.ToDouble(Parameter.Value);
			value += IncrementValue;

			ErrorMessage = "Failed to set the parameter.\r\n" +
				"\tParameter: " + Parameter.Name + "\r\n" + 
				"\tValue: " + value + "\r\n\r\n";
			_waitForGet.Reset();
			Communicator.SetParamValue(Parameter, value, GetValueCallback);

			bool isNotTimeOut = _waitForGet.WaitOne(2000);
			_waitForGet.Reset();
			if(!isNotTimeOut)
				IsPass = false;

			
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

		public override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			Parameter = (sourceNode as ScriptNodeIncrementValue).Parameter;
			IncrementValue = (sourceNode as ScriptNodeIncrementValue).IncrementValue;
		}
	}
}
