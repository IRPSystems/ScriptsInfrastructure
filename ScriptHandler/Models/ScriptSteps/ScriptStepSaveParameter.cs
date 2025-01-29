using DeviceCommunicators.Enums;
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Interfaces;
using DeviceHandler.Models;
using Entities.Enums;
using Newtonsoft.Json.Linq;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ScriptHandler.Models.ScriptSteps
{
    public class ScriptStepSaveParameter : ScriptStepBase, IScriptStepWithCommunicator, IScriptStepWithParameter
    {
        public DeviceParameterData Parameter { get; set; }
        public DeviceCommunicator Communicator { get; set; }

        private MCU_ParamData _saveParameter;
        private byte[] _id;

        private ManualResetEvent _waitGetCallback;
        private bool _isStopped;

        public ScriptStepSaveParameter()
        {
			if (Application.Current != null)
				Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;

            _isStopped = false;

            _saveParameter = new MCU_ParamData()
            {
                Name = "Save parameter",
                Cmd = "save_param",
                Scale = 1,
            };

            _id = new byte[3];

            _totalNumOfSteps = 2;
        }

        public override void Execute()
        {
            ErrorMessage = "Failed to save the parameter.\r\n" +
                    "\tParameter: " + Parameter.Name + "\r\n\r\n";
			IsExecuted = true;

			_waitGetCallback = new ManualResetEvent(false);

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
                PopulateSendResponseLog(UserTitle, this.GetType().Name, Parameter.Name, Parameter.DeviceType, Parameter.CommSendResLog);
                ErrorMessage += "Communication timeout.";
                IsPass = false;
            }

            _stepsCounter++;
            if (IsPass)
                LoggerService.Inforamtion(this, "Saved parameter");

            AddToEOLSummary();
        }

        private void GetCallback(DeviceParameterData param, CommunicatorResultEnum result, string resultDescription)
        {
            if (_isStopped)
                return;


            _waitGetCallback.Set();

            switch (result)
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
            if (IsPass == false) { }
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
            Parameter = (sourceNode as ScriptNodeSaveParameter).Parameter;
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
