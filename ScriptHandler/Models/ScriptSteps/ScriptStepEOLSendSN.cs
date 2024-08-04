﻿
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace ScriptHandler.Models.ScriptSteps
{
	public class ScriptStepEOLSendSN : ScriptStepBase, IScriptStepWithCommunicator
	{
        #region Properties and Fields

        public DeviceParameterData SN_Param { get; set; }
        public DeviceCommunicator Communicator { get; set; }

        private ScriptStepSetParameter _setValue;
        private ScriptStepGetParamValue _getValue;
        private ScriptStepSetSaveParameter _saveValue;

		private string userSN = "E7P-22-00-00032";
		private string serialNumber;

		#endregion Properties and Fields

		#region Constructor

        public ScriptStepEOLSendSN()
        {
            _setValue = new ScriptStepSetParameter();
            _getValue = new ScriptStepGetParamValue();
            _saveValue = new ScriptStepSetSaveParameter();
        }

		#endregion Constructor

		#region Methods

		public override void Execute()
		{
            //Get SN from UI - Temp userSN

            //remove daash and letters

            serialNumber = Regex.Replace(userSN, "[A-Za-z ]", "");
            serialNumber = serialNumber.Remove(0, 1);
            serialNumber = serialNumber.Replace("-", "");

            //Set SN
            _setValue.Parameter = SN_Param;
            _setValue.Communicator = Communicator;
            _setValue.Value = serialNumber;
            _setValue.Execute();

            if (!_setValue.IsPass)
            {
                ErrorMessage = "Unable to set: " + SN_Param.Name;
                return;
            }

            //Verify SN- get
            _getValue.Parameter = SN_Param;
            _getValue.Communicator = Communicator;
            _getValue.SendAndReceive();
            if (!_getValue.IsPass)
            {
                //Validate SN
                ErrorMessage = "Unable to get SN: \r\n"
                + _getValue.ErrorMessage;
                IsPass = false;
                return;
            }

            double snCompare;

            bool isParseSuccess = double.TryParse(serialNumber, out snCompare);

            if (!isParseSuccess)
            {
                ErrorMessage = "Unable to parse SN \r\n"
                + _getValue.ErrorMessage;
                IsPass = false;
                return;
            }

            //Validate SN
            if ((double)SN_Param.Value != snCompare)
            {
                ErrorMessage = "Wrong SN \r\n"
                + _getValue.ErrorMessage;
                IsPass = false;
                return;
            }

            //If succeed save param
            _saveValue.Parameter = SN_Param;
            _saveValue.Communicator = Communicator;
            _saveValue.Value = snCompare;
            _saveValue.Execute();

            if (!_saveValue.IsPass)
            {
                IsPass = false;
                ErrorMessage = "Unable to save SN: " + _saveValue.ErrorMessage;
                return;
            }
            IsPass = true;
            return;
        }

		protected override void Stop()
		{

		}

		public override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			
		}

		public override void GetRealParamAfterLoad(
			DevicesContainer devicesContainer)
		{
			SN_Param = new MCU_ParamData()
			{
				Name = "Serial Number",
				Cmd = "serialnumber",
				DeviceType = Entities.Enums.DeviceTypesEnum.MCU
			};

			SN_Param = GetRealParam(
				SN_Param,
				devicesContainer);
		}

		#endregion Methods
	}
}
