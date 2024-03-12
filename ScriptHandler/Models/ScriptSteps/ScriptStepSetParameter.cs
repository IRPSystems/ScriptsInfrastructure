
using DeviceCommunicators.Enums;
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceCommunicators.NI_6002;
using DeviceCommunicators.Scope_KeySight;
using DeviceCommunicators.SwitchRelay32;
using DeviceHandler.Models;
using Entities.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using Syncfusion.Windows.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepSetParameter: ScriptStepBase, IScriptStepWithCommunicator, IScriptStepWithParameter
	{
		public DeviceParameterData Parameter { get; set; }
		public double Value { get; set; }

		[JsonIgnore]
		public DeviceCommunicator Communicator { get; set; }

		public int Ni6002_IOPort { get; set; }
		public object Ni6002_Value { get; set; }



		private AutoResetEvent _waitGetCallback;
		private bool _isStopped;

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

			_isStopped = false;
		}

		public override void Execute()
		{
			if (Parameter == null)
			{
				ErrorMessage = "The parameter is unknown";
				return;
			}
			
			IsPass = true;
			_isStopped = false;

			
			ErrorMessage = "Failed to set the value.\r\n" +
					"\tParameter: \"" + Parameter.Name + "\"\r\n" +
					"\tValue: " + Value + "\r\n\r\n";

			if (Communicator == null || Communicator.IsInitialized == false)
			{
				ErrorMessage += "The communication is not initialized";
				IsPass = false;
				return;
			}


			if(Parameter is NI6002_ParamData ni)
			{
				ni.Io_port = Ni6002_IOPort;
				ni.Value = Ni6002_Value;
			}
			else if (Parameter is Scope_KeySight_ParamData ks_Param &&
				Parameter.Name.ToLower() == "save")
			{
				ks_Param.data = "Evva_" + DateTime.Now.ToString("DD_MMM_YYY_HH_mm_ss");
			}

			Communicator.SetParamValue(Parameter, Value, GetCallback);

			bool isNotTimeout = _waitGetCallback.WaitOne(1000);
			if (!isNotTimeout)
			{
				ErrorMessage += "Communication timeout.";
				IsPass = false;
			}

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

		public override void Generate(
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
				Ni6002_Value = (sourceNode as ScriptNodeSetParameter).Ni6002_Value;
				Ni6002_IOPort = (sourceNode as ScriptNodeSetParameter).Ni6002_IOPort;
			}
			else
				Value = (sourceNode as ScriptNodeSetParameter).Value;



			if ((sourceNode as ScriptNodeSetParameter).Parameter is SwitchRelay_ParamData &&
				(sourceNode as ScriptNodeSetParameter).Parameter.Name.ToLower() == "single channel")
			{
				(Parameter as SwitchRelay_ParamData).Channel_SW =
					(sourceNode as ScriptNodeSetParameter).SwitchRelayChannel;
			}
		}
	}
}
