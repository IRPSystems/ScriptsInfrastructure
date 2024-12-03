
using DeviceCommunicators.Enums;
using DeviceCommunicators.MCU;
using Entities.Models;
using ScriptHandler.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System;
using System.Threading;
using System.Windows;
using Newtonsoft.Json;
using Services.Services;
using DeviceCommunicators.General;
using DeviceHandler.Models;
using ScriptHandler.Services;
using System.Collections.Generic;
using ScriptHandler.Models.ScriptNodes;
using System.Collections.ObjectModel;
using DeviceCommunicators.Models;
using DeviceHandler.Interfaces;
using Entities.Enums;

namespace ScriptHandler.Models
{
	public class ScriptStepSetSaveParameter : ScriptStepBase, IScriptStepWithCommunicator, IScriptStepWithParameter
	{
		public DeviceParameterData Parameter { get; set; }
		
		public double Value { get; set; }

		[JsonIgnore]
		public DeviceCommunicator Communicator { get; set; }

		private MCU_ParamData _saveParameter;
		private byte[] _id;

		private ManualResetEvent _waitGetCallback;
		private bool _isStopped;

		public ScriptStepSetSaveParameter()
		{
            try
            {
                if (Application.Current != null)
                    Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
            }
            catch { }

            _isStopped = false;

			_saveParameter = new MCU_ParamData()
			{
				Name = "Save parameter",
				Cmd = "save_param",
				Scale = 1,
			};

			_id = new byte[3];

			_totalNumOfSteps = 6;
		}

		public override void Execute()
		{
			IsPass = true;
			_isExecuted = true;
			_isStopped = false;

			_stepsCounter = 1;

			if (Parameter == null)
				return;

			EOLStepSummeryData eolStepSummeryData = new EOLStepSummeryData();
			eolStepSummeryData.Description = Description;

			_waitGetCallback = new ManualResetEvent(false);

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

			double value = Value;

			_stepsCounter++;

			Communicator.SetParamValue(Parameter, value, GetCallback);

			bool isNotTimeout = _waitGetCallback.WaitOne(1000);
			if (!isNotTimeout)
			{
				ErrorMessage += "Communication timeout.";
				IsPass = false;
				eolStepSummeryData.IsPass = false;
				eolStepSummeryData.ErrorDescription = ErrorMessage;
				EOLStepSummerysList.Add(eolStepSummeryData);
			}

			LoggerService.Inforamtion(this, "Set parameter for saving");

			_stepsCounter++;

			System.Threading.Thread.Sleep(1000);

			if(IsPass)
				Save();

			AddToEOLSummary();
		}


		private void Save()
		{
			ErrorMessage = "Failed to save the parameter.\r\n" +
					"\tParameter: \"" + Parameter.Name + "\r\n\r\n";

			EOLStepSummeryData eolStepSummeryData = new EOLStepSummeryData();
			eolStepSummeryData.Description = Description;

			if (Communicator == null || Communicator.IsInitialized == false)
			{
				ErrorMessage += "The communication is not initialized";
				IsPass = false;
				eolStepSummeryData.IsPass = false;
				eolStepSummeryData.ErrorDescription = ErrorMessage;
				EOLStepSummerysList.Add(eolStepSummeryData);
				return;
			}

			using (var md5 = MD5.Create())
			{
				Array.Copy(md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes((Parameter as MCU_ParamData).Cmd)), 0, _id, 0, 3);
			}

			var hex_id = BitConverter.ToString(_id).Replace("-", "").ToLower();
			double value = Convert.ToInt32(hex_id, 16);

			_stepsCounter++;

			Communicator.SetParamValue(_saveParameter, value, GetCallback);

			bool isNotTimeout = _waitGetCallback.WaitOne(1000);
			if (!isNotTimeout)
			{
				ErrorMessage += "Communication timeout.";
				IsPass = false;
			}

			_stepsCounter++;
			if (IsPass)
				LoggerService.Inforamtion(this, "Saved parameter");
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


			_waitGetCallback.Set();

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
			if(IsPass == false) { }
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
			Parameter = (sourceNode as ScriptNodeSetSaveParameter).Parameter;
			Value = (sourceNode as ScriptNodeSetSaveParameter).Value;
		}

		public override void GetRealParamAfterLoad(
			DevicesContainer devicesContainer)
		{
			if (Parameter is ICalculatedParamete)
				return;

			Parameter = GetRealParam(
				Parameter,
				devicesContainer);

		}

        public override List<DeviceTypesEnum> GetUsedDevices()
        {
            List<DeviceTypesEnum> UsedDevices = new List<DeviceTypesEnum>();
            UsedDevices.Add(Parameter.DeviceType);
            return UsedDevices;
        }
    }
}
