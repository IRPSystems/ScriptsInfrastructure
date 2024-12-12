
using DeviceCommunicators.Enums;
using DeviceCommunicators.EvvaDevice;
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceCommunicators.NI_6002;
using DeviceCommunicators.NumatoGPIO;
using DeviceCommunicators.PowerSupplayEA;
using DeviceCommunicators.Scope_KeySight;
using DeviceCommunicators.SwitchRelay32;
using DeviceCommunicators.ZimmerPowerMeter;
using DeviceHandler.Interfaces;
using DeviceHandler.Models;
using Entities.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepSetParameter: ScriptStepBase, IScriptStepWithCommunicator, IScriptStepWithParameter
	{
		#region Properties and Fields

		public DeviceParameterData Parameter { get; set; }

		public object Value { get; set; }

		[JsonIgnore]
		public DeviceCommunicator Communicator { get; set; }

		public ExtraDataForParameter ExtraData { get; set; }

		private DeviceParameterData _valueParameter;
		public DeviceParameterData ValueParameter 
		{
			get => _valueParameter;
			set
			{
				_valueParameter = value;
				GetParamValue.Parameter = value;
			}
		}

		public ScriptStepGetParamValue GetParamValue {  get; set; }

		


		private AutoResetEvent _waitGetCallback;
		private bool _isStopped;


		#endregion Properties and Fields

		#region Constructor

		public ScriptStepSetParameter()
		{
			if (Application.Current != null)
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
				});
			}

			_waitGetCallback = new AutoResetEvent(false);
			GetParamValue = new ScriptStepGetParamValue();

			_isStopped = false;

			_totalNumOfSteps = 4;
		}

		#endregion Constructor

		#region Methodes

		public override void Execute()
		{
			IsExecuted = true;
			if (Parameter == null)
			{
				ErrorMessage = "The parameter is unknown";
				return;
			}

			_waitGetCallback = new AutoResetEvent(false);

			EOLStepSummeryData eolStepSummeryData = new EOLStepSummeryData();
			eolStepSummeryData.Description = GetOnlineDescription();

			_stepsCounter = 1;
			
			IsPass = true;
			_isStopped = false;


			
			ErrorMessage = "Failed to set the value.\r\n" +
					"\tParameter: \"" + Parameter.Name + "\"\r\n" +
					"\tValue: " + Value + "\r\n\r\n";

			if (Communicator == null || Communicator.IsInitialized == false)
			{
				ErrorMessage += "The communication is not initialized";
				IsPass = false;
				eolStepSummeryData.IsPass = false;
				eolStepSummeryData.ErrorDescription = ErrorMessage;
				EOLStepSummerysList.Add(eolStepSummeryData);
				return;
			}

			if(ExtraData != null) 
				ExtraData.SetToParameter(Parameter);
			
			if (Parameter is Scope_KeySight_ParamData ks_Param &&
				Parameter.Name.ToLower() == "save")
			{
				ks_Param.data = "Evva_" + DateTime.Now.ToString("DD_MMM_YYY_HH_mm_ss");
			}

			if(ValueParameter != null)
			{
				GetValue();
				if (IsPass == false)
					return;
			}

			_stepsCounter++;

			Communicator.SetParamValue(Parameter, Convert.ToDouble(Value), GetCallback);

			int timeOut = 1000;
			if (Parameter.CommunicationTimeout > 0)
			{
				timeOut = Parameter.CommunicationTimeout;
			}


			bool isNotTimeout = _waitGetCallback.WaitOne(timeOut);
			if (!isNotTimeout)
			{				
				ErrorMessage += "Communication timeout.";
				IsPass = false;
			}

			AddToEOLSummary();
			_stepsCounter++;
		}

		private string GetOnlineDescription()
		{

			string stepDescription = "Set ";

			if (Parameter is Evva_ParamData)
			{
				double d = Convert.ToDouble(Value);
				if (d == 1)
					stepDescription = "Start Safty Officer" + " - ID:" + ID;
				else
					stepDescription = "Stop Safty Officer" + " - ID:" + ID;

				return stepDescription;
			}

			if (Parameter is DeviceParameterData deviceParameter)
			{
				stepDescription += " \"" + deviceParameter + "\"";

				if (Parameter is NI6002_ParamData)
				{
					//if (deviceParameter.Name == "Digital port output" ||
					//deviceParameter.Name == "Analog port output" ||
					//deviceParameter.Name == "Read digital input" ||
					//deviceParameter.Name == "Read Anolog input")
					//{
					//	stepDescription += " - Pin out " + Ni6002_IOPort;
					//}

					//stepDescription += " = " + Ni6002_Value;
				}
				else
				{
					stepDescription += " = " + Value;
				}
			}



			stepDescription += " - ID:" + ID;
			return stepDescription;



		}

		private void GetValue()
		{
			if(GetParamValue.Communicator == null ||
				GetParamValue.Parameter == null)
			{
				IsPass = false;
				ErrorMessage += "Problem with getting the parameter value to set";
				return;
			}

			EOLStepSummeryData eolStepSummeryData;
			bool res = GetParamValue.SendAndReceive(out eolStepSummeryData, Description);
			EOLStepSummerysList.Add(eolStepSummeryData);
			if (res == false || GetParamValue.IsPass == false)
			{
				IsPass = false;
				ErrorMessage += "Failed to get the parameter value to set";
				return;
			}

			Value = null;
			Value = GetParamValue.Parameter.Value;
		}

		protected override void Stop()
		{
			_isStopped = true;
			if(_waitGetCallback != null)
				_waitGetCallback.Set();
		}


		private void GetCallback(DeviceParameterData param, CommunicatorResultEnum result, string resultDescription)
		{
			if (_isStopped)
				return;


			

			switch(result)
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

			IsPass = result == CommunicatorResultEnum.OK;
			if (!IsPass) { }

			_waitGetCallback.Set();
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
			Parameter = (sourceNode as ScriptNodeSetParameter).Parameter;
			if ((sourceNode as ScriptNodeSetParameter).Parameter is SwitchRelay_ParamData &&
				(sourceNode as ScriptNodeSetParameter).Parameter.Name.ToLower() == "relay registers")
			{
				Value = (sourceNode as ScriptNodeSetParameter).SwitchRelayValue.NumericValue;
			}
			else if ((sourceNode as ScriptNodeSetParameter).Parameter is NI6002_ParamData)
			{
				Value = (sourceNode as ScriptNodeSetParameter).Value;
			}
			else if ((sourceNode as ScriptNodeSetParameter).Parameter is NumatoGPIO_ParamData)
			{
				Value = (sourceNode as ScriptNodeSetParameter).Value;
			}
			else if ((sourceNode as ScriptNodeSetParameter).Parameter is ZimmerPowerMeter_ParamData)
			{
				Value = (sourceNode as ScriptNodeSetParameter).Value;
			}
			else
				Value = (sourceNode as ScriptNodeSetParameter).Value;

			ValueParameter = (sourceNode as ScriptNodeSetParameter).ValueParameter;

			ExtraData = new ExtraDataForParameter((sourceNode as ScriptNodeSetParameter).ExtraData);


			if ((sourceNode as ScriptNodeSetParameter).Parameter is SwitchRelay_ParamData &&
				(sourceNode as ScriptNodeSetParameter).Parameter.Name.ToLower() == "single channel")
			{
				(Parameter as SwitchRelay_ParamData).Channel_SW =
					(sourceNode as ScriptNodeSetParameter).SwitchRelayChannel;
			}
		}

		public override void GetRealParamAfterLoad(
			DevicesContainer devicesContainer)
		{
			if (Parameter is ICalculatedParamete)
				return;

			DeviceParameterData parameter = GetRealParam(
				Parameter,
				devicesContainer);
			if(Parameter is NI6002_ParamData niSource &&
				parameter is NI6002_ParamData niDest)
			{
				niDest.portLine = niSource.portLine;
				niDest.Io_port = niSource.Io_port;
			}

			if (ValueParameter != null)
			{
				ValueParameter = GetRealParam(
					ValueParameter,
					devicesContainer);
			}

		}

        public override List<DeviceTypesEnum> GetUsedDevices()
        {
            List<DeviceTypesEnum> UsedDevices = new List<DeviceTypesEnum>();
            UsedDevices.Add(Parameter.DeviceType);
            return UsedDevices;
        }

        #endregion Methodes

    }
}
