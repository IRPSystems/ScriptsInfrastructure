
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace ScriptHandler.Models.ScriptSteps
{
	public class ScriptStepEOLSendSN : ScriptStepBase
	{
        #region Properties

        public DeviceParameterData SN_Param { get; set; }
        public DeviceCommunicator MCU_Communicator { get; set; }

        private ScriptStepSetParameter _setValue;
        private ScriptStepGetParamValue _getValue;
        private ScriptStepSetSaveParameter _saveValue;

        string userSN = "E7P-22-00-00032";
        string serialNumber;

        #endregion Properties

        public override void Execute()
		{
            //Get SN from UI - Temp userSN

            //remove daash and letters

            serialNumber = Regex.Replace(userSN, "[A-Za-z ]", "");
            serialNumber = serialNumber.Remove(0, 1);
            serialNumber = serialNumber.Replace("-", "");

            //Set SN
            _setValue = new ScriptStepSetParameter();
            _setValue.Parameter = SN_Param;
            _setValue.Communicator = MCU_Communicator;
            _setValue.Value = serialNumber;
            _setValue.Execute();

            if (!_setValue.IsPass)
            {
                ErrorMessage = "Unable to set: " + SN_Param.Name;
                return;
            }

            //Verify SN- get

            _getValue = new ScriptStepGetParamValue();
            _getValue.Parameter = SN_Param;
            _getValue.Communicator = MCU_Communicator;
            _getValue.SendAndReceive();
            if (!_getValue.IsPass)
            {
                //Validate SN
                if(SN_Param.Value as string == userSN)
                {
                    ErrorMessage = "Wrong SN \r\n"
                    + _getValue.ErrorMessage;
                    IsPass = false;
                    return;
                }
            }

            //If succeed save param

            _saveValue = new ScriptStepSetSaveParameter();
            _saveValue.Parameter = SN_Param;
            _saveValue.Communicator = MCU_Communicator;
            _saveValue.Value = Convert.ToDouble(serialNumber);
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
	}
}
