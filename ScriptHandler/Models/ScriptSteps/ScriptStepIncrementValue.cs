
using DeviceCommunicators.Enums;
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using Entities.Models;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using Services.Services;
using Syncfusion.UI.Xaml.Diagram;
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
			Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
			_setParameter = new ScriptStepSetParameter();
		}



		public override void Execute()
		{
			_isStoped = false;
			IsPass = true;

			_setParameter.Communicator = Communicator;
			_setParameter.Parameter = Parameter;

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

			

			_setParameter.Value = value;
			_setParameter.Execute();
			if(!_setParameter.IsPass)
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
